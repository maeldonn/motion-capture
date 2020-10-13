/************************************************************************************
 Copyright: Copyright 2020 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Neuron;

public class NeuronTransformsInstanceVR : NeuronInstance
{

	public enum Modes { 
		FULL_BODY,
		RIGHT_ARM_FOLLOW_HMD,
		RIGHT_HAND_OPTICAL_ASSISTED
	}

	[Header("How the motion data is used:")]
	public bool 									allowHipRotation = false;
	public Modes 									ActiveMode = Modes.FULL_BODY;
	public UpdateMethod								motionUpdateMethod = UpdateMethod.Normal;

	[Header("Settings for when not using FULL_BODY mode:")]
	public Vector3 									rightShoulderRotationOffset;
	public Transform								OpticalAssistanceObject;
	//public bool										boundTransforms { get ; set; }


	[Header("Body and bone settings:")]
	public Transform								root = null;
	public string									prefix = "Robot_";
	public Transform								physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference
	public NeuronTransformsPhysicalReference		physicalReference = new NeuronTransformsPhysicalReference();
	public Transform[]								bones = new Transform[(int)NeuronBones.NumOfBones];

	Vector3[]										bonePositionOffsets = new Vector3[(int)HumanBodyBones.LastBone];
	Vector3[]										boneRotationOffsets = new Vector3[(int)HumanBodyBones.LastBone];
	float 											velocityMagic = 3000.0f;
	float 											angularVelocityMagic = 20.0f;
	List<NeuronBones> 								validBones = new List<NeuronBones>();
	Transform	 									rightArmTarget;
	Transform 										RightArmHolder;
	[HideInInspector] public bool editorScriptHasLoadedBones;

	public NeuronTransformsInstanceVR()
	{
	}

	public NeuronTransformsInstanceVR( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
		:base( address, port, commandServerPort, socketType, actorID )
	{
	}

	public NeuronTransformsInstanceVR( Transform root, string prefix, string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
		:base( address, port, commandServerPort, socketType, actorID )
	{
		Bind( root, prefix );
	}

	public NeuronTransformsInstanceVR( Transform root, string prefix, NeuronActor actor )
		:base( actor )
	{
		Bind( root, prefix );
	}

	public NeuronTransformsInstanceVR( NeuronActor actor )
		:base( actor )
	{
	}

	new void OnEnable()
	{
		base.OnEnable();

		if( root == null )
		{
			root = transform;
		}

		if (!editorScriptHasLoadedBones) {
			Bind (root, prefix);
		}

		LoadValidBones ();
	}


	new void Update()
	{
		base.ToggleConnect();
		base.Update();

		if( boundActor != null && motionUpdateMethod == UpdateMethod.Normal )
		{				
			if( physicalReference.Initiated() )
			{
				ReleasePhysicalContext();
			}

			ApplyMotion( boundActor, bonePositionOffsets, boneRotationOffsets );
		}
	}

	void FixedUpdate()
	{
		base.ToggleConnect();

		if( boundActor != null && motionUpdateMethod != UpdateMethod.Normal )
		{				
			if( !physicalReference.Initiated() )
			{
				InitPhysicalContext();
			}

			ApplyMotionPhysically( physicalReference.GetReferenceTransforms(), bones );
		}
	}


	public Transform[] GetBones()
	{
		return bones;
	}

	static bool ValidateVector3( Vector3 vec )
	{
		return !float.IsNaN( vec.x ) && !float.IsNaN( vec.y ) && !float.IsNaN( vec.z )
			&& !float.IsInfinity( vec.x ) && !float.IsInfinity( vec.y ) && !float.IsInfinity( vec.z );
	}



	// set rotation for bone
	void SetRotation(NeuronBones bone, Vector3 rotation )
	{
		Transform t = bones[(int)bone];
		if( t != null )
		{
			Quaternion rot = Quaternion.Euler( rotation );
			if( !float.IsNaN( rot.x ) && !float.IsNaN( rot.y ) && !float.IsNaN( rot.z ) && !float.IsNaN( rot.w ) )
			{
				t.localRotation = rot;
			}
		}
	}

	// apply transforms extracted from actor mocap data to bones
	public void ApplyMotion( NeuronActor actor, Vector3[] bonePositionOffsets, Vector3[] boneRotationOffsets )
	{			
		// only rotation in vr mode on the hips
		if (allowHipRotation){
			//SetPosition( NeuronBones.Hips, actor.GetReceivedPosition( NeuronBones.Hips ) );
			SetRotation( NeuronBones.Hips, actor.GetReceivedRotation( NeuronBones.Hips ) );
		}

		switch (ActiveMode) {
		case Modes.FULL_BODY:
			// Full body mode, apply to every bone
			for( int i = 1; i < (int)NeuronBones.NumOfBones && i < bones.Length; ++i ) {
				SetRotation ((NeuronBones)i, actor.GetReceivedRotation ((NeuronBones)i));
			}
			break; 

		case Modes.RIGHT_ARM_FOLLOW_HMD:
			// Mode when the root rotation and position follows the HMD
			// apply to only valid bones and also adjust position of detached right arm
			if (motionUpdateMethod == UpdateMethod.Normal && rightArmTarget != null) {
				RightArmHolder.position = rightArmTarget.position;
			}
			for (int i = 0; i < validBones.Count; ++i) {
				SetRotation (validBones[i], actor.GetReceivedRotation (validBones[i]));
			}
			break;

		case Modes.RIGHT_HAND_OPTICAL_ASSISTED:
			// Move right lower arm to the optical assisted object including rotation
			// Then rotate the remaining bones of the hand:
			if (OpticalAssistanceObject != null) {
				bones [(int)NeuronBones.RightForeArm].position = OpticalAssistanceObject.position;
				bones [(int)NeuronBones.RightForeArm].rotation = OpticalAssistanceObject.rotation;

				for (int i = 0; i < validBones.Count; ++i) {
					SetRotation (validBones [i], actor.GetReceivedRotation (validBones [i]));
				}
			}
			break;
		}
	}

	// apply Transforms of src bones to dest Rigidbody Components of bone
	public void ApplyMotionPhysically( Transform[] src, Transform[] dest )
	{
		if( src != null && dest != null )
		{
			for( int i = 0; i < (int)NeuronBones.NumOfBones; ++i )
			{
				Transform src_transform = src[i];
				Transform dest_transform = dest[i];
				if( src_transform != null && dest_transform != null )
				{
					Rigidbody rigidbody = dest_transform.GetComponent<Rigidbody>();
					if( rigidbody != null )
					{
						switch (motionUpdateMethod) {
						case UpdateMethod.Physical:
							rigidbody.MovePosition( src_transform.position );
							rigidbody.MoveRotation( src_transform.rotation );
							break;

						case UpdateMethod.EstimatedPhysical:
							Quaternion dAng = src_transform.rotation * Quaternion.Inverse (dest_transform.rotation);
							float angle = 0.0f;
							Vector3 axis = Vector3.zero;
							dAng.ToAngleAxis (out angle, out axis);

							Vector3 velocityTarget = (src_transform.position - dest_transform.position) * velocityMagic * Time.fixedDeltaTime;

							Vector3 angularTarget = (Time.fixedDeltaTime * angle * axis) * angularVelocityMagic;

							ApplyVelocity(rigidbody, velocityTarget, angularTarget);

							break;

						case UpdateMethod.MixedPhysical:
							Vector3 velocityTarget2 = (src_transform.position - dest_transform.position) * velocityMagic * Time.fixedDeltaTime;

							Vector3 v = Vector3.MoveTowards(rigidbody.velocity, velocityTarget2, 10.0f);
							if( ValidateVector3( v ) )
							{
								rigidbody.velocity = v;
							}

							rigidbody.MoveRotation( src_transform.rotation );

							break;
						}
					}
				}
			}
		}
	}


	void ApplyVelocity(Rigidbody rb, Vector3 velocityTarget, Vector3 angularTarget)
	{
		Vector3 v = Vector3.MoveTowards(rb.velocity, velocityTarget, 10.0f);
		if( ValidateVector3( v ) )
		{
			rb.velocity = v;
		}

		v = Vector3.MoveTowards(rb.angularVelocity, angularTarget, 10.0f);
		if( ValidateVector3( v ) )
		{
			rb.angularVelocity = v;
		}
	}

	public bool Bind( Transform root, string prefix )
	{
		this.root = root;
		this.prefix = prefix;
		//int bound_count = 
        NeuronHelper.Bind( root, bones, prefix, false , NeuronBoneVersion.NotSet_AutoByName);
		//boundTransforms = bound_count >= (int)NeuronBones.NumOfBones;
		//return boundTransforms;
		return true;
	}

	void LoadValidBones(){
		switch (ActiveMode) {
		case Modes.RIGHT_ARM_FOLLOW_HMD:
			// RIGHT ARM
			validBones.Add (NeuronBones.RightArm);
			validBones.Add (NeuronBones.RightForeArm);
			// RIGHT HAND
			validBones.Add (NeuronBones.RightHand);
			validBones.Add (NeuronBones.RightHandThumb1);
			validBones.Add (NeuronBones.RightHandThumb2);
			validBones.Add (NeuronBones.RightHandThumb3);
			validBones.Add (NeuronBones.RightInHandIndex);
			validBones.Add (NeuronBones.RightHandIndex1);
			validBones.Add (NeuronBones.RightHandIndex2);
			validBones.Add (NeuronBones.RightHandIndex3);
			validBones.Add (NeuronBones.RightInHandMiddle);
			validBones.Add (NeuronBones.RightHandMiddle1);
			validBones.Add (NeuronBones.RightHandMiddle2);
			validBones.Add (NeuronBones.RightHandMiddle3);
			validBones.Add (NeuronBones.RightInHandRing);
			validBones.Add (NeuronBones.RightHandRing1);
			validBones.Add (NeuronBones.RightHandRing2);
			validBones.Add (NeuronBones.RightHandRing3);
			validBones.Add (NeuronBones.RightInHandPinky);
			validBones.Add (NeuronBones.RightHandPinky1);
			validBones.Add (NeuronBones.RightHandPinky2);
			validBones.Add (NeuronBones.RightHandPinky3);

			if (motionUpdateMethod == UpdateMethod.Normal) { // only detach in normal update mode
				Invoke ("DetachRightArm", 0.3f); // delayed detach to allow collider robot to find all the right target references
			}
			break;

		case Modes.RIGHT_HAND_OPTICAL_ASSISTED:
			// RIGHT HAND
			validBones.Add (NeuronBones.RightHand);
			validBones.Add (NeuronBones.RightHandThumb1);
			validBones.Add (NeuronBones.RightHandThumb2);
			validBones.Add (NeuronBones.RightHandThumb3);
			validBones.Add (NeuronBones.RightInHandIndex);
			validBones.Add (NeuronBones.RightHandIndex1);
			validBones.Add (NeuronBones.RightHandIndex2);
			validBones.Add (NeuronBones.RightHandIndex3);
			validBones.Add (NeuronBones.RightInHandMiddle);
			validBones.Add (NeuronBones.RightHandMiddle1);
			validBones.Add (NeuronBones.RightHandMiddle2);
			validBones.Add (NeuronBones.RightHandMiddle3);
			validBones.Add (NeuronBones.RightInHandRing);
			validBones.Add (NeuronBones.RightHandRing1);
			validBones.Add (NeuronBones.RightHandRing2);
			validBones.Add (NeuronBones.RightHandRing3);
			validBones.Add (NeuronBones.RightInHandPinky);
			validBones.Add (NeuronBones.RightHandPinky1);
			validBones.Add (NeuronBones.RightHandPinky2);
			validBones.Add (NeuronBones.RightHandPinky3);
			break;

		}
	}


	void DetachRightArm(){
		Transform t = bones [(int)NeuronBones.RightArm];

		RightArmHolder = new GameObject ("RightArm Holder").transform;
		// rotate 180 on Y for the arm holder to get the right arm rotation
		RightArmHolder.rotation = Quaternion.Euler(rightShoulderRotationOffset);
		RightArmHolder.position = t.position;
		rightArmTarget = new GameObject ("RightArm Target Position Reference").transform;
		rightArmTarget.position = t.position;
		rightArmTarget.rotation =  t.rotation;
		rightArmTarget.SetParent (bones[(int)NeuronBones.RightShoulder]);
		rightArmTarget.localRotation =  Quaternion.identity;

		t.SetParent (RightArmHolder);
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
	}


	void InitPhysicalContext()
	{
		if( physicalReference.Init( root, prefix, bones, physicalReferenceOverride ) )
		{
			// break original object's hierachy of transforms, so we can use MovePosition() and MoveRotation() to set transform
			NeuronHelper.BreakHierarchy( bones );
		}

		CheckRigidbodySettings ();
	}

	void ReleasePhysicalContext()
	{
		physicalReference.Release();
	}

	void CheckRigidbodySettings( ){
		//check if rigidbodies have correct settings
		bool kinematicSetting = false;
		if (motionUpdateMethod == UpdateMethod.Physical) {
			kinematicSetting = true;
		}

		for( int i = 0; i < (int)NeuronBones.NumOfBones && i < bones.Length; ++i )
		{
			Rigidbody r = bones[i].GetComponent<Rigidbody> ();
			if (r != null) {
				r.isKinematic = kinematicSetting;
			}
		}
	}

	public Transform GetBoneTransform( NeuronBones bone ){
		return bones [(int)bone];
	}
}