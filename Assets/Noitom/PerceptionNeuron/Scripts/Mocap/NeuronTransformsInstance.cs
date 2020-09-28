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
using UnityEngine;
using NeuronDataReaderManaged;

namespace Neuron
{
	public class NeuronTransformsInstance : NeuronInstance
	{
        //public NeuronTransformsInstance temp;
        //public static NeuronTransformsInstance tempSource;
        public bool useNewRig = true;
        public bool enableHipMove = true;
		public Transform					root = null;
        //
        // Obsolete don't use it
        [HideInInspector]
		public string						prefix = "Robot_";
		public bool							boundTransforms { get ; private set; }
		public UpdateMethod					motionUpdateMethod = UpdateMethod.Normal;

        [HideInInspector]
        public Transform[]					transforms = new Transform[(int)NeuronBones.NumOfBones];

		public Transform					physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference
		public NeuronTransformsPhysicalReference	physicalReference = new NeuronTransformsPhysicalReference();
		Vector3[]							bonePositionOffsets = new Vector3[(int)NeuronBones.NumOfBones];
		Vector3[]							boneRotationOffsets = new Vector3[(int)NeuronBones.NumOfBones];

		public float 								velocityMagic = 3000.0f;
		public float 								angularVelocityMagic = 70.0f;
        Quaternion[] orignalRot = new Quaternion[(int)NeuronBones.NumOfBones];
        Quaternion[] orignalParentRot = new Quaternion[(int)NeuronBones.NumOfBones];

		public NeuronTransformsInstance()
		{
		}
		
		public NeuronTransformsInstance( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
			:base( address, port, commandServerPort, socketType, actorID )
		{
		}
		
		public NeuronTransformsInstance( Transform root, string prefix, string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
			:base( address, port, commandServerPort, socketType, actorID )
		{
			Bind( root, prefix );
		}
		
		public NeuronTransformsInstance( Transform root, string prefix, NeuronActor actor )
			:base( actor )
		{
			Bind( root, prefix );
		}
		
		public NeuronTransformsInstance( NeuronActor actor )
			:base( actor )
		{
		}

        bool inited = false;
		new void OnEnable()
		{
            if(inited)
            {
                return;
            }
            inited = true;

            base.OnEnable();
			
			if( root == null )
			{
				root = transform;
			}
			
			Bind( root, prefix );
		}
		
		new void Update()
		{
			base.ToggleConnect();
			base.Update();
			
			if( boundActor != null && boundTransforms && motionUpdateMethod == UpdateMethod.Normal )
			{				
				if( physicalReference.Initiated() )
				{
					ReleasePhysicalContext();
				}
				
				ApplyMotion( boundActor, transforms, bonePositionOffsets, boneRotationOffsets , enableHipMove, orignalRot, orignalParentRot);
			}

		}
		
		void FixedUpdate()
		{
			base.ToggleConnect();
			
			if( boundActor != null && boundTransforms && motionUpdateMethod != UpdateMethod.Normal )
			{				
				if( !physicalReference.Initiated() )
				{
					InitPhysicalContext();
				}

				ApplyMotionPhysically( physicalReference.GetReferenceTransforms(), transforms );
			}
		}
		
		public Transform[] GetTransforms()
		{
			return transforms;
		}
		
		static bool ValidateVector3( Vector3 vec )
		{
			return !float.IsNaN( vec.x ) && !float.IsNaN( vec.y ) && !float.IsNaN( vec.z )
				&& !float.IsInfinity( vec.x ) && !float.IsInfinity( vec.y ) && !float.IsInfinity( vec.z );
		}
		
		// set position for bone
		static void SetPosition( Transform[] transforms, NeuronBones bone, Vector3 pos )
		{
			Transform t = transforms[(int)bone];
			if( t != null )
			{
				// calculate position when we have scale
				pos.Scale( new Vector3( 1.0f / t.parent.lossyScale.x, 1.0f / t.parent.lossyScale.y, 1.0f / t.parent.lossyScale.z ) );
			
				if( !float.IsNaN( pos.x ) && !float.IsNaN( pos.y ) && !float.IsNaN( pos.z ) )
				{
					t.localPosition = pos;
				}
			}
		}
		
		// set rotation for bone
		static void SetRotation( Transform[] transforms, NeuronBones bone, Vector3 rotation )
		{
			Transform t = transforms[(int)bone];
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
		public static void ApplyMotion( NeuronActor actor, Transform[] transforms, Vector3[] bonePositionOffsets, Vector3[] boneRotationOffsets , bool enableHipMove,  Quaternion[] orignalRot = null, Quaternion[] orignalParentRot = null)
		{
            // apply Hips position
            if (enableHipMove)
                SetPosition(transforms, NeuronBones.Hips, actor.GetReceivedPosition(NeuronBones.Hips));
            else
            {
                Vector3 p = actor.GetReceivedPosition(NeuronBones.Hips);
                SetPosition(transforms, NeuronBones.Hips, new Vector3(0f, p.y, 0f));
            }

            SetRotation(transforms, NeuronBones.Hips,
                (Quaternion.Euler(actor.GetReceivedRotation(NeuronBones.Hips)) * orignalRot[(int)NeuronBones.Hips]).eulerAngles);


            // apply positions
            if (actor.withDisplacement )
			{
				for( int i = 1; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i )
				{
                    if (transforms[i] == null)
                        continue;

                    // q
                    Quaternion orignalBoneRot = Quaternion.identity;
                    if (orignalRot != null)
                    {
                        orignalBoneRot = orignalRot[i];
                    }
                    Vector3 rot = actor.GetReceivedRotation((NeuronBones)i) + boneRotationOffsets[i] ;
                    Quaternion srcQ = Quaternion.Euler(rot);

                    Quaternion usedQ = Quaternion.Inverse(orignalParentRot[i]) * srcQ * orignalParentRot[i];
                    Vector3 transedRot = usedQ.eulerAngles;
                    Quaternion finalBoneQ = Quaternion.Euler(transedRot) * orignalBoneRot;
                    SetRotation(transforms, (NeuronBones)i, finalBoneQ.eulerAngles);

                    // p
                    Vector3 srcP = actor.GetReceivedPosition((NeuronBones)i) + bonePositionOffsets[i];
                    Vector3 finalP = Quaternion.Inverse(orignalParentRot[i]) * srcP;
                    SetPosition(transforms, (NeuronBones)i, finalP);
                    //  SetPosition( transforms, (NeuronBones)i, actor.GetReceivedPosition( (NeuronBones)i ) + bonePositionOffsets[i] );
                    //	SetRotation( transforms, (NeuronBones)i, actor.GetReceivedRotation( (NeuronBones)i ) + boneRotationOffsets[i] );
                }
            }
			else
			{
				// apply rotations
				for( int i = 1; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i )
				{
                    if (transforms[i] == null)
                        continue;
                    Quaternion orignalBoneRot = Quaternion.identity;
                    if (/*testUnityChan && */orignalRot != null)
                    {
                        orignalBoneRot = orignalRot[i];
                    }
                    Vector3 rot = actor.GetReceivedRotation((NeuronBones)i);
                    Quaternion srcQ = Quaternion.Euler(rot);

                    //// Test
                    //rot = tempSource.transforms[i].localRotation.eulerAngles;
                    //Quaternion srcQ = tempSource.transforms[i].localRotation;
                    //// Test end

                    Quaternion usedQ = Quaternion.Inverse(orignalParentRot[i]) * srcQ * orignalParentRot[i];
                    Vector3 transedRot =    usedQ.eulerAngles; 
                    Quaternion finalBoneQ =  Quaternion.Euler(transedRot) * orignalBoneRot;
                    SetRotation( transforms, (NeuronBones)i, finalBoneQ.eulerAngles);

                }
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
            NeuronHelper.Bind( root, transforms, prefix, false, useNewRig ? NeuronBoneVersion.V2 : NeuronBoneVersion.V1);
            boundTransforms = true; // bound_count >= (int)NeuronBones.NumOfBones;
			UpdateOffset();
            CaluateOrignalRot();
            return boundTransforms;
		}
		
		void InitPhysicalContext()
		{
			if( physicalReference.Init( root, prefix, transforms, physicalReferenceOverride ) )
			{
				// break original object's hierachy of transforms, so we can use MovePosition() and MoveRotation() to set transform
				NeuronHelper.BreakHierarchy( transforms );
			}

			CheckRigidbodySettings ();
		}

		
		void ReleasePhysicalContext()
		{
			physicalReference.Release();
		}
		
		void UpdateOffset()
		{
			// initiate values
			for( int i = 0; i < (int)HumanBodyBones.LastBone; ++i )
			{
				bonePositionOffsets[i] = Vector3.zero;
				boneRotationOffsets[i] = Vector3.zero;
			}
			/*
			if( boundTransforms )
			{
                if(transforms[(int)NeuronBones.LeftUpLeg] != null)
				    bonePositionOffsets[(int)NeuronBones.LeftUpLeg] = new Vector3( 0.0f, transforms[(int)NeuronBones.LeftUpLeg].localPosition.y, 0.0f );
                if(transforms[(int)NeuronBones.RightUpLeg] != null)
				    bonePositionOffsets[(int)NeuronBones.RightUpLeg] = new Vector3( 0.0f, transforms[(int)NeuronBones.RightUpLeg].localPosition.y, 0.0f );
			}
			*/
		}

        void CaluateOrignalRot()
        {
            for (int i = 0; i < orignalRot.Length; i++)
            {
                orignalRot[i] = transforms[i] == null ? Quaternion.identity : transforms[i].localRotation;
            }
            for (int i = 0; i < orignalRot.Length; i++)
            {
                Quaternion parentQs = Quaternion.identity;
                if(transforms[i] == null)
                {
                    orignalParentRot[i] = Quaternion.identity;
                    continue;
                }
                Transform tempParent = transforms[i].transform.parent;
                while (tempParent != null)
                {
                    parentQs = tempParent.transform.localRotation * parentQs;
                    tempParent = tempParent.parent;
                    if (tempParent == null || tempParent == this.gameObject)
                        break;
                }
                orignalParentRot[i] = parentQs;
            }
        }
		void CheckRigidbodySettings( ){
			//check if rigidbodies have correct settings
			bool kinematicSetting = false;
			if (motionUpdateMethod == UpdateMethod.Physical) {
				kinematicSetting = true;
			}

			for( int i = 0; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i )
			{
				Rigidbody r = transforms[i].GetComponent<Rigidbody> ();
				if (r != null) {
					r.isKinematic = kinematicSetting;
				}
			}
		}
	}
}