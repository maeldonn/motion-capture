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
	public class TranceTransformsInstance : NeuronInstance
	{
        public string avatarName;
        public Transform					root = null;
        //public string						prefix = "Robot_";
        public bool							boundTransforms { get ; private set; }
        public UpdateMethod					motionUpdateMethod = UpdateMethod.Normal;

        //public Transform[]					transforms = new Transform[(int)InertialBones.NumOfBones];

        public Transform					physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference
        public NeuronTransformsPhysicalReference	physicalReference = new NeuronTransformsPhysicalReference();
        Vector3[]							bonePositionOffsets = new Vector3[(int)InertialBones.NumOfBones];
        Vector3[]							boneRotationOffsets = new Vector3[(int)InertialBones.NumOfBones];

        public float 								velocityMagic = 3000.0f;
        public float 								angularVelocityMagic = 70.0f;

        #region just copy from trance sdk
        //public TrackingMotion source;
        Animator animator;

        public bool physicalUpdate = false;
        public string prefix = System.String.Empty;

        // if avatar is humanrig, we use animator.GetBoneTransform to find transform, else we use this array.
        public Transform[] transforms = new Transform[(int)InertialBones.NumOfBones];

#pragma warning disable 0414
        bool usingHumanRig = true;
#pragma warning restore 0414

        void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            CheckValid();
        }

        void CheckValid()
        {
            if (string.IsNullOrEmpty(avatarName))
            {
                Debug.LogErrorFormat("AliceMotion: {0} , not set avatarName; avatarName should be same as actor name in trance", gameObject.name);
            }

            //bool isHumanRigAvatar = false;
            //if (animator != null && animator.avatar != null)
            //{
            //    isHumanRigAvatar = animator.avatar.isHuman;
            //    Debug.LogWarningFormat("{0} has animator component, it's a humenrig avatar , ignore auto bind", gameObject.name);
            //}

            //// bind transform by name
            //if (!isHumanRigAvatar)
            {
                usingHumanRig = false;
                int bindCount = AliceMotionBindHelper.Bind(transform, transforms, prefix);
                if (bindCount != (int)InertialBones.NumOfBones)
                {
                    Debug.LogErrorFormat("some of alice motion bone can't find, wanted:{0}, found:{1}", (int)InertialBones.NumOfBones, bindCount);
                }
                else
                {
                    Debug.LogFormat("{0} bind avatar success!", gameObject.name);
                }
            }
        }

        void FindTrackingSource()
        {
            if (source != null && (boundActor == null || (boundActor != null && boundActor.name != avatarName)) && (Time.frameCount % UnityEngine.Random.Range(15, 20) == 0))
            {
                boundActor = source.FindTrackingMotionByAvatarName(avatarName);
                if (boundActor != null)
                {
                    Debug.Log("Found source by name " + avatarName, this);
                }
            }
        }
        #endregion


        public TranceTransformsInstance()
		{
		}
		
		public TranceTransformsInstance( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
			:base( address, port, commandServerPort, socketType, actorID )
		{
		}
		
		public TranceTransformsInstance( Transform root, string prefix, string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, int actorID )
			:base( address, port, commandServerPort, socketType, actorID )
		{
			Bind( root, prefix );
		}
		
		public TranceTransformsInstance( Transform root, string prefix, NeuronActor actor )
			:base( actor )
		{
			Bind( root, prefix );
		}
		
		public TranceTransformsInstance( NeuronActor actor )
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
			
			Bind( root, prefix );
		}
		
		new void Update()
		{
			base.ToggleConnect();
			base.Update();
            FindTrackingSource();

            if ( boundActor != null /*&& boundTransforms*/ && motionUpdateMethod == UpdateMethod.Normal )
			{				
				if( physicalReference.Initiated() )
				{
					ReleasePhysicalContext();
				}
				
				ApplyMotion( boundActor, transforms, bonePositionOffsets, boneRotationOffsets );
			}
		}
		
		void FixedUpdate()
		{
			base.ToggleConnect();
            FindTrackingSource();


            if ( boundActor != null /*&& boundTransforms*/ && motionUpdateMethod != UpdateMethod.Normal )
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
		static void SetPosition( Transform[] transforms, InertialBones bone, Vector3 pos )
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
		static void SetRotation( Transform[] transforms, InertialBones bone, Vector3 rotation )
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
		public static void ApplyMotion( NeuronActor actor, Transform[] transforms, Vector3[] bonePositionOffsets, Vector3[] boneRotationOffsets )
		{
			// apply Hips position
			SetPosition( transforms, InertialBones.Hips, actor.GetReceivedPosition((NeuronBones) InertialBones.Hips ) );
			SetRotation( transforms, InertialBones.Hips, actor.GetReceivedRotation((NeuronBones)InertialBones.Hips ) );
			
			// apply positions
			if( actor.withDisplacement )
			{

                for (int i = 1; i < (int)InertialBones.NumOfBones && i < transforms.Length; ++i)
                {
                    SetPosition(transforms, (InertialBones)i, actor.GetReceivedPosition((NeuronBones)i) + bonePositionOffsets[i]);
                    SetRotation(transforms, (InertialBones)i, actor.GetReceivedRotation((NeuronBones)i) + boneRotationOffsets[i]);
                }
                //for (int i = 0; i < (int)InertialBones.NumOfBones; i++)
                //{
                //    SetPosition(transforms, (InertialBones)i, actor.GetReceivedPosition((NeuronBones)i));
                //}
                //for (int i = 0; i < (int)InertialBones.NumOfBones; i++)
                //{
                //    SetRotation(transforms, (InertialBones)i, actor.GetReceivedRotation((NeuronBones)i));
                //}
            }
			else
			{
				// apply rotations
				for( int i = 1; i < (int)InertialBones.NumOfBones && i < transforms.Length; ++i )
				{
					SetRotation( transforms, (InertialBones)i, actor.GetReceivedRotation( (NeuronBones)i ) );
				}
			}
		}
		
		// apply Transforms of src bones to dest Rigidbody Components of bone
		public void ApplyMotionPhysically( Transform[] src, Transform[] dest )
		{
			if( src != null && dest != null )
			{
				for( int i = 0; i < (int)InertialBones.NumOfBones; ++i )
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
			int bound_count = AliceMotionBindHelper.Bind(transform, transforms, prefix);
            boundTransforms = bound_count >= (int)InertialBones.NumOfBones;
			UpdateOffset();
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
			
			if( boundTransforms )
			{
				bonePositionOffsets[(int)InertialBones.LeftUpLeg] = new Vector3( 0.0f, transforms[(int)InertialBones.LeftUpLeg].localPosition.y, 0.0f );
				bonePositionOffsets[(int)InertialBones.RightUpLeg] = new Vector3( 0.0f, transforms[(int)InertialBones.RightUpLeg].localPosition.y, 0.0f );
			}
		}

		void CheckRigidbodySettings( ){
			//check if rigidbodies have correct settings
			bool kinematicSetting = false;
			if (motionUpdateMethod == UpdateMethod.Physical) {
				kinematicSetting = true;
			}

			for( int i = 0; i < (int)InertialBones.NumOfBones && i < transforms.Length; ++i )
			{
				Rigidbody r = transforms[i].GetComponent<Rigidbody> ();
				if (r != null) {
					r.isKinematic = kinematicSetting;
				}
			}
		}


        public enum InertialBones
        {
            Hips = 0,
            RightUpLeg = 1,
            RightLeg = 2,
            RightFoot = 3,
            LeftUpLeg = 4,
            LeftLeg = 5,
            LeftFoot = 6,
            Spine = 7,
            Spine1 = 8,
            Spine2 = 9,
            Neck = 10,
            Neck1 = 11,
            Head = 12,
            RightShoulder = 13,
            RightArm = 14,
            RightForeArm = 15,
            RightHand = 16,
            RightHandThumb1 = 17,
            RightHandThumb2 = 18,
            RightHandThumb3 = 19,
            RightInHandIndex = 20,
            RightHandIndex1 = 21,
            RightHandIndex2 = 22,
            RightHandIndex3 = 23,
            RightInHandMiddle = 24,
            RightHandMiddle1 = 25,
            RightHandMiddle2 = 26,
            RightHandMiddle3 = 27,
            RightInHandRing = 28,
            RightHandRing1 = 29,
            RightHandRing2 = 30,
            RightHandRing3 = 31,
            RightInHandPinky = 32,
            RightHandPinky1 = 33,
            RightHandPinky2 = 34,
            RightHandPinky3 = 35,
            LeftShoulder = 36,
            LeftArm = 37,
            LeftForeArm = 38,
            LeftHand = 39,
            LeftHandThumb1 = 40,
            LeftHandThumb2 = 41,
            LeftHandThumb3 = 42,
            LeftInHandIndex = 43,
            LeftHandIndex1 = 44,
            LeftHandIndex2 = 45,
            LeftHandIndex3 = 46,
            LeftInHandMiddle = 47,
            LeftHandMiddle1 = 48,
            LeftHandMiddle2 = 49,
            LeftHandMiddle3 = 50,
            LeftInHandRing = 51,
            LeftHandRing1 = 52,
            LeftHandRing2 = 53,
            LeftHandRing3 = 54,
            LeftInHandPinky = 55,
            LeftHandPinky1 = 56,
            LeftHandPinky2 = 57,
            LeftHandPinky3 = 58,

            NumOfBones
        }
    }

}