/************************************************************************************
 Copyright: Copyright 2020 Beijing Noitom Technology Ltd.All Rights reserved.
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

using UnityEngine;
using Neuron;
using UniHumanoid;

public class NeuronAnimatorInstanceBVH : NeuronInstance
{
	public bool useNewRig = true;
	public Animator boundAnimator = null;
	public UpdateMethod motionUpdateMethod = UpdateMethod.Normal;
	public bool enableHipsMovement = true;

	public Animator physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference
	public NeuronAnimatorPhysicalReference physicalReference = new NeuronAnimatorPhysicalReference();
	Vector3[] bonePositionOffsets = new Vector3[(int)HumanBodyBones.LastBone];
	Vector3[] boneRotationOffsets = new Vector3[(int)HumanBodyBones.LastBone];

	public float velocityMagic = 3000.0f;
	public float angularVelocityMagic = 20.0f;
	Quaternion[] orignalRot = new Quaternion[(int)HumanBodyBones.LastBone];
	Quaternion[] orignalParentRot = new Quaternion[(int)HumanBodyBones.LastBone];



	public NeuronAnimatorInstanceBVH()
	{
	}


	public NeuronAnimatorInstanceBVH(Animator animator, NeuronActor actor)
		: base(actor)
	{
		boundAnimator = animator;
		UpdateOffset();
	}

	public NeuronAnimatorInstanceBVH(NeuronActor actor)
		: base(actor)
	{
	}

	bool inited = false;
	new void OnEnable()
	{
		if (inited)
		{
			return;
		}
		inited = true;
		base.OnEnable();
		if (boundAnimator == null)
		{
			boundAnimator = GetComponent<Animator>();
		}
		UpdateOffset();
		CaluateOrignalRot();
	}

	// TODO: Use a Timer                //J'ai arrangé l'animation avec Time.deltaTime, ça devrait mieux fonctionner maintenant.
	new void Update()
	{
		if (boundAnimator != null && motionUpdateMethod == UpdateMethod.Normal)
		{
			if (physicalReference.Initiated())
			{
				ReleasePhysicalContext();
			}

            //Les 7 lignes suivantes servent à calculer la frame de l'animation suivant le temps passé.
            timePassedBetweenFrame += Time.deltaTime;
            timePassedBetweenFrame = timePassedBetweenFrame%totalTime;
            nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
            //Debug.Log((int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds));    //Cette ligne peut-être utile pour avoir une idée du nombre de frame qui sont sautées.

            ApplyMotion(boundAnimator, bonePositionOffsets, boneRotationOffsets);
		}
	}

    public bool applyPosition;
    int nbFrame;
    float timePassedBetweenFrame;
	Bvh bvh;
    private float totalTime;        //Etant donné que cette valeur ne change pas, et puisqu'elle est utilisée régulièrement, on la garde de coté.

    public int NbFrame
    {
        get { return nbFrame; }
        set
        {
            if (nbFrame == value) return;
            nbFrame = value;
        }
    }

    void Start()
	{
		nbFrame = 0;
        bvh = gameObject.GetComponent<BvhImporter>().GetBvh();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

	bool ValidateVector3(Vector3 vec)
	{
		return !float.IsNaN(vec.x) && !float.IsNaN(vec.y) && !float.IsNaN(vec.z)
			&& !float.IsInfinity(vec.x) && !float.IsInfinity(vec.y) && !float.IsInfinity(vec.z);
	}

	void SetScale(Animator animator, HumanBodyBones bone, float size, float referenceSize)
	{
		Transform t = animator.GetBoneTransform(bone);
		if (t != null && bone <= HumanBodyBones.Jaw)
		{
			float ratio = size / referenceSize;

			Vector3 newScale = new Vector3(ratio, ratio, ratio);
			newScale.Scale(new Vector3(1.0f / t.parent.lossyScale.x, 1.0f / t.parent.lossyScale.y, 1.0f / t.parent.lossyScale.z));

			if (ValidateVector3(newScale))
			{
				t.localScale = newScale;
			}
		}
	}

	// set position for bone in animator
	void SetPosition(Animator animator, HumanBodyBones bone, Vector3 pos)
	{
		Transform t = animator.GetBoneTransform(bone);
		if (t != null)
		{
			if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z))
			{
				//	t.localPosition = pos;

				Vector3 srcP = pos;
				Vector3 finalP = Quaternion.Inverse(orignalParentRot[(int)bone]) * srcP;
				t.localPosition = finalP;
			}
		}
	}

	// set rotation for bone in animator
	void SetRotation(Animator animator, HumanBodyBones bone, Vector3 rotation)
	{
		Transform t = animator.GetBoneTransform(bone);
		if (t != null)
		{
			Quaternion rot = Quaternion.Euler(rotation);
			if (!float.IsNaN(rot.x) && !float.IsNaN(rot.y) && !float.IsNaN(rot.z) && !float.IsNaN(rot.w))
			{
				//t.localRotation = rot;

				Quaternion orignalBoneRot = Quaternion.identity;
				if (orignalRot != null)
				{
					orignalBoneRot = orignalRot[(int)bone];
				}
				Quaternion srcQ = rot;

				Quaternion usedQ = Quaternion.Inverse(orignalParentRot[(int)bone]) * srcQ * orignalParentRot[(int)bone];
				Vector3 transedRot = usedQ.eulerAngles;
				Quaternion finalBoneQ = Quaternion.Euler(transedRot) * orignalBoneRot;
				t.localRotation = finalBoneQ;
			}
		}
	}

	// apply transforms extracted from actor mocap data to transforms of animator bones
	public void ApplyMotion(Animator animator, Vector3[] positionOffsets, Vector3[] rotationOffsets)
	{
		// apply Hips position
		if (enableHipsMovement)
		{
			SetPosition(animator, HumanBodyBones.Hips, bvh.GetReceivedPosition("Hips",nbFrame, false) + positionOffsets[(int)HumanBodyBones.Hips]);
			SetRotation(animator, HumanBodyBones.Hips, bvh.GetReceivedPosition("Hips", nbFrame, true));
		}

		// apply positions
		if (applyPosition)//actor.withDisplacement)
		{
			// legs
			SetPosition(animator, HumanBodyBones.RightUpperLeg, bvh.GetReceivedPosition("RightUpLeg", nbFrame, false) + positionOffsets[(int)HumanBodyBones.RightUpperLeg]);
			SetPosition(animator, HumanBodyBones.RightLowerLeg, bvh.GetReceivedPosition("RightLeg", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightFoot, bvh.GetReceivedPosition("RightFoot", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftUpperLeg, bvh.GetReceivedPosition("LeftUpLeg", nbFrame, false) + positionOffsets[(int)HumanBodyBones.LeftUpperLeg]);
			SetPosition(animator, HumanBodyBones.LeftLowerLeg, bvh.GetReceivedPosition("LeftLeg", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftFoot, bvh.GetReceivedPosition("LeftFoot", nbFrame, false));

			// spine
			SetPosition(animator, HumanBodyBones.Spine, bvh.GetReceivedPosition("Spine", nbFrame, false));
			if (useNewRig)
			{
				SetPosition(animator, HumanBodyBones.Chest,
					 bvh.GetReceivedPosition("Spine1", nbFrame, false)
					 );
#if UNITY_2018_2_OR_NEWER
				SetPosition(animator, HumanBodyBones.UpperChest,
					 bvh.GetReceivedPosition("Spine2", nbFrame, false)
					 );
#endif
				SetPosition(animator, HumanBodyBones.Neck,
					 bvh.GetReceivedPosition("Neck", nbFrame, false) +
					 (EulerToQuaternion(bvh.GetReceivedPosition("Neck", nbFrame, false)) * bvh.GetReceivedPosition("Neck1", nbFrame, false))
					);
			}
			else
			{
				SetPosition(animator, HumanBodyBones.Chest, bvh.GetReceivedPosition("Spin3", nbFrame, false));
				SetPosition(animator, HumanBodyBones.Neck, bvh.GetReceivedPosition("Neck", nbFrame, false));
			}
			SetPosition(animator, HumanBodyBones.Head, bvh.GetReceivedPosition("Head", nbFrame, false));

			// right arm
			SetPosition(animator, HumanBodyBones.RightShoulder, bvh.GetReceivedPosition("RightShoulder", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightUpperArm, bvh.GetReceivedPosition("RightArm", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightLowerArm, bvh.GetReceivedPosition("RightForeArm", nbFrame, false));

			// right hand
			SetPosition(animator, HumanBodyBones.RightHand, bvh.GetReceivedPosition("RightHand", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightThumbProximal, bvh.GetReceivedPosition("RightHandThumb1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightThumbIntermediate, bvh.GetReceivedPosition("RightHandThumb2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightThumbDistal, bvh.GetReceivedPosition("RightHandThumb3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.RightIndexProximal, bvh.GetReceivedPosition("RightHandIndex1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightIndexIntermediate, bvh.GetReceivedPosition("RightHandIndex2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightIndexDistal, bvh.GetReceivedPosition("RightHandIndex3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.RightMiddleProximal, bvh.GetReceivedPosition("RightHandMiddle1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightMiddleIntermediate, bvh.GetReceivedPosition("RightHandMiddle2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightMiddleDistal, bvh.GetReceivedPosition("RightHandMiddle3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.RightRingProximal, bvh.GetReceivedPosition("RightHandRing1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightRingIntermediate, bvh.GetReceivedPosition("RightHandRing2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightRingDistal, bvh.GetReceivedPosition("RightHandRing3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.RightLittleProximal, bvh.GetReceivedPosition("RightHandPinky1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightLittleIntermediate, bvh.GetReceivedPosition("RightHandPinky2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.RightLittleDistal, bvh.GetReceivedPosition("RightHandPinky3", nbFrame, false));

			// left arm
			SetPosition(animator, HumanBodyBones.LeftShoulder, bvh.GetReceivedPosition("LeftShoulder", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftUpperArm, bvh.GetReceivedPosition("LeftArm", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftLowerArm, bvh.GetReceivedPosition("LeftForeArm", nbFrame, false));

			// left hand
			SetPosition(animator, HumanBodyBones.LeftHand, bvh.GetReceivedPosition("LeftHand", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftThumbProximal, bvh.GetReceivedPosition("LeftHandThumb1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftThumbIntermediate, bvh.GetReceivedPosition("LeftHandThumb2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftThumbDistal, bvh.GetReceivedPosition("LeftHandThumb3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.LeftIndexProximal, bvh.GetReceivedPosition("LeftHandIndex1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftIndexIntermediate, bvh.GetReceivedPosition("LeftHandIndex2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftIndexDistal, bvh.GetReceivedPosition("LeftHandIndex3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.LeftMiddleProximal, bvh.GetReceivedPosition("LeftHandMiddle1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftMiddleIntermediate, bvh.GetReceivedPosition("LeftHandMiddle2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftMiddleDistal, bvh.GetReceivedPosition("LeftHandMiddle3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.LeftRingProximal, bvh.GetReceivedPosition("LeftHandRing1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftRingIntermediate, bvh.GetReceivedPosition("LeftHandRing2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftRingDistal, bvh.GetReceivedPosition("LeftHandRing3", nbFrame, false));

			SetPosition(animator, HumanBodyBones.LeftLittleProximal, bvh.GetReceivedPosition("LeftHandPinky1", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftLittleIntermediate, bvh.GetReceivedPosition("LeftHandPinky2", nbFrame, false));
			SetPosition(animator, HumanBodyBones.LeftLittleDistal, bvh.GetReceivedPosition("LeftHandPinky3", nbFrame, false));
		}

		// apply rotations

		// legs
		SetRotation(animator, HumanBodyBones.RightUpperLeg, bvh.GetReceivedPosition("RightUpLeg", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightLowerLeg, bvh.GetReceivedPosition("RightLeg", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightFoot, bvh.GetReceivedPosition("RightFoot", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftUpperLeg, bvh.GetReceivedPosition("LeftUpLeg", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftLowerLeg, bvh.GetReceivedPosition("LeftLeg", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftFoot, bvh.GetReceivedPosition("LeftFoot", nbFrame, true));

		// spine
		SetRotation(animator, HumanBodyBones.Spine, bvh.GetReceivedPosition("Spine", nbFrame, true));
		//SetRotation( animator, HumanBodyBones.Chest,					actor.GetReceivedRotation( NeuronBones.Spine1 ) + actor.GetReceivedRotation( NeuronBones.Spine2 ) + actor.GetReceivedRotation( NeuronBones.Spine3 ) ); 
		if (useNewRig)
		{
			SetRotation(animator, HumanBodyBones.Chest,
				(EulerToQuaternion(bvh.GetReceivedPosition("Spine1", nbFrame, true)) *
				EulerToQuaternion(bvh.GetReceivedPosition("Spine2", nbFrame, true))).eulerAngles
				);

			SetRotation(animator, HumanBodyBones.Neck,
				(EulerToQuaternion(bvh.GetReceivedPosition("Neck", nbFrame, true)) *
				EulerToQuaternion(bvh.GetReceivedPosition("Neck1", nbFrame, true))).eulerAngles
				);
		}
		else
		{
			SetRotation(animator, HumanBodyBones.Chest,
				(EulerToQuaternion(bvh.GetReceivedPosition("Spine1", nbFrame, true)) *
				EulerToQuaternion(bvh.GetReceivedPosition("Spine2", nbFrame, true)) *
				EulerToQuaternion(bvh.GetReceivedPosition("Spine3", nbFrame, true))).eulerAngles
				);
			SetRotation(animator, HumanBodyBones.Neck, bvh.GetReceivedPosition("Neck", nbFrame, true));
		}

		SetRotation(animator, HumanBodyBones.Head, bvh.GetReceivedPosition("Head", nbFrame, true));

		// right arm
		SetRotation(animator, HumanBodyBones.RightShoulder, bvh.GetReceivedPosition("RightShoulder", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightUpperArm, bvh.GetReceivedPosition("RightArm", nbFrame, true)); 
		SetRotation(animator, HumanBodyBones.RightLowerArm, bvh.GetReceivedPosition("RightForeArm", nbFrame, true)); 

		// right hand
		SetRotation(animator, HumanBodyBones.RightHand, bvh.GetReceivedPosition("RightHand", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightThumbProximal, bvh.GetReceivedPosition("RightHandThumb1", nbFrame, true)); 
		SetRotation(animator, HumanBodyBones.RightThumbIntermediate, bvh.GetReceivedPosition("RightHandThumb2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightThumbDistal, bvh.GetReceivedPosition("RightHandThumb3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.RightIndexProximal,		actor.GetReceivedRotation( NeuronBones.RightHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandIndex ) );
		SetRotation(animator, HumanBodyBones.RightIndexProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("RightInHandIndex", nbFrame, true)) * //
			 EulerToQuaternion(bvh.GetReceivedPosition("RightHandIndex1", nbFrame, true))).eulerAngles //
			);
		SetRotation(animator, HumanBodyBones.RightIndexIntermediate, bvh.GetReceivedPosition("RightHandIndex2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightIndexDistal, bvh.GetReceivedPosition("RightHandIndex3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.RightMiddleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandMiddle ) );
		SetRotation(animator, HumanBodyBones.RightMiddleProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("RightInHandMiddle", nbFrame, true)) *  //
			 EulerToQuaternion(bvh.GetReceivedPosition("RightHandMiddle1", nbFrame, true))).eulerAngles    //
			);
		SetRotation(animator, HumanBodyBones.RightMiddleIntermediate, bvh.GetReceivedPosition("RightHandMiddle2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightMiddleDistal, bvh.GetReceivedPosition("RightHandMiddle3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.RightRingProximal,		actor.GetReceivedRotation( NeuronBones.RightHandRing1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandRing ) );
		SetRotation(animator, HumanBodyBones.RightRingProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("RightInHandRing", nbFrame, true)) *    //
			 EulerToQuaternion(bvh.GetReceivedPosition("RightHandRing1", nbFrame, true))).eulerAngles  //
			);
		SetRotation(animator, HumanBodyBones.RightRingIntermediate, bvh.GetReceivedPosition("RightHandRing2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightRingDistal, bvh.GetReceivedPosition("RightHandRing3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.RightLittleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandPinky ) );
		SetRotation(animator, HumanBodyBones.RightLittleProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("RightInHandPinky", nbFrame, true)) *       //
			 EulerToQuaternion(bvh.GetReceivedPosition("RightHandPinky1", nbFrame, true))).eulerAngles //
			);
		SetRotation(animator, HumanBodyBones.RightLittleIntermediate, bvh.GetReceivedPosition("RightHandPinky2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.RightLittleDistal, bvh.GetReceivedPosition("RightHandPinky3", nbFrame, true));

		// left arm
		SetRotation(animator, HumanBodyBones.LeftShoulder, bvh.GetReceivedPosition("LeftShoulder", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftUpperArm, bvh.GetReceivedPosition("LeftArm", nbFrame, true)); //
		SetRotation(animator, HumanBodyBones.LeftLowerArm, bvh.GetReceivedPosition("LeftForeArm", nbFrame, true)); //

		// left hand
		SetRotation(animator, HumanBodyBones.LeftHand, bvh.GetReceivedPosition("LeftHand", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftThumbProximal, bvh.GetReceivedPosition("LeftHandThumb1", nbFrame, true)); //
		SetRotation(animator, HumanBodyBones.LeftThumbIntermediate, bvh.GetReceivedPosition("LeftHandThumb2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftThumbDistal, bvh.GetReceivedPosition("LeftHandThumb3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.LeftIndexProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandIndex ) );
		SetRotation(animator, HumanBodyBones.LeftIndexProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandIndex", nbFrame, true)) *    //
			 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandIndex1", nbFrame, true))).eulerAngles  //
			);
		SetRotation(animator, HumanBodyBones.LeftIndexIntermediate, bvh.GetReceivedPosition("LeftHandIndex2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftIndexDistal, bvh.GetReceivedPosition("LeftHandIndex3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.LeftMiddleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandMiddle ) );
		SetRotation(animator, HumanBodyBones.LeftMiddleProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandMiddle", nbFrame, true)) *   //
			 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandMiddle1", nbFrame, true))).eulerAngles //
			);
		SetRotation(animator, HumanBodyBones.LeftMiddleIntermediate, bvh.GetReceivedPosition("LeftHandMiddle2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftMiddleDistal, bvh.GetReceivedPosition("LeftHandMiddle3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.LeftRingProximal,			actor.GetReceivedRotation( NeuronBones.LeftHandRing1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandRing ) );
		SetRotation(animator, HumanBodyBones.LeftRingProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandRing", nbFrame, true)) * //
			 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandRing1", nbFrame, true))).eulerAngles   //
			);
		SetRotation(animator, HumanBodyBones.LeftRingIntermediate, bvh.GetReceivedPosition("LeftHandRing2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftRingDistal, bvh.GetReceivedPosition("LeftHandRing3", nbFrame, true));

		//SetRotation( animator, HumanBodyBones.LeftLittleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandPinky ) );
		SetRotation(animator, HumanBodyBones.LeftLittleProximal,
			(EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandPinky", nbFrame, true)) *    //
			 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandPinky1", nbFrame, true))).eulerAngles  //
			);
		SetRotation(animator, HumanBodyBones.LeftLittleIntermediate, bvh.GetReceivedPosition("LeftHandPinky2", nbFrame, true));
		SetRotation(animator, HumanBodyBones.LeftLittleDistal, bvh.GetReceivedPosition("LeftHandPinky3", nbFrame, true));
	}

	Quaternion EulerToQuaternion(Vector3 euler)
	{
		return Quaternion.Euler(euler.x, euler.y, euler.z);
	}

	// apply Transforms of src bones to Rigidbody Components of dest bones
	public void ApplyMotionPhysically(Animator src, Animator dest)
	{
		for (HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i)
		{
			Transform src_transform = src.GetBoneTransform(i);
			Transform dest_transform = dest.GetBoneTransform(i);
			if (src_transform != null && dest_transform != null)
			{
				Rigidbody rigidbody = dest_transform.GetComponent<Rigidbody>();
				if (rigidbody != null)
				{
					switch (motionUpdateMethod)
					{
						case UpdateMethod.Physical:
							rigidbody.MovePosition(src_transform.position);
							rigidbody.MoveRotation(src_transform.rotation);
							break;

						case UpdateMethod.EstimatedPhysical:
							Quaternion dAng = src_transform.rotation * Quaternion.Inverse(dest_transform.rotation);
							float angle = 0.0f;
							Vector3 axis = Vector3.zero;
							dAng.ToAngleAxis(out angle, out axis);

							Vector3 velocityTarget = (src_transform.position - dest_transform.position) * velocityMagic * Time.fixedDeltaTime;

							Vector3 angularTarget = (Time.fixedDeltaTime * angle * axis) * angularVelocityMagic;

							ApplyVelocity(rigidbody, velocityTarget, angularTarget);

							break;

						case UpdateMethod.MixedPhysical:
							Vector3 velocityTarget2 = (src_transform.position - dest_transform.position) * velocityMagic * Time.fixedDeltaTime;

							Vector3 v = Vector3.MoveTowards(rigidbody.velocity, velocityTarget2, 10.0f);
							if (ValidateVector3(v))
							{
								rigidbody.velocity = v;
							}

							rigidbody.MoveRotation(src_transform.rotation);


							break;
					}

				}
			}
		}
	}

	void ApplyVelocity(Rigidbody rb, Vector3 velocityTarget, Vector3 angularTarget)
	{
		Vector3 v = Vector3.MoveTowards(rb.velocity, velocityTarget, 10.0f);
		if (ValidateVector3(v))
		{
			rb.velocity = v;
		}

		v = Vector3.MoveTowards(rb.angularVelocity, angularTarget, 10.0f);
		if (ValidateVector3(v))
		{
			rb.angularVelocity = v;
		}
	}

	void InitPhysicalContext()
	{
		if (physicalReference.Init(boundAnimator, physicalReferenceOverride))
		{
			// break original object's hierachy of transforms, so we can use MovePosition() and MoveRotation() to set transform
			NeuronHelper.BreakHierarchy(boundAnimator);
			//return true;
		}

		CheckRigidbodySettings();
		//	return false;
	}

	void ReleasePhysicalContext()
	{
		physicalReference.Release();
	}

	void UpdateOffset()
	{
		// we do some adjustment for the bones here which would replaced by our model retargeting later

		if (boundAnimator != null)
		{
			// initiate values
			for (int i = 0; i < (int)HumanBodyBones.LastBone; ++i)
			{
				bonePositionOffsets[i] = Vector3.zero;
				boneRotationOffsets[i] = Vector3.zero;
			}

			if (boundAnimator != null)
			{
				Transform leftLegTransform = boundAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
				Transform rightLegTransform = boundAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
				if (leftLegTransform != null)
				{
					bonePositionOffsets[(int)HumanBodyBones.LeftUpperLeg] = new Vector3(0.0f, leftLegTransform.localPosition.y, 0.0f);
					bonePositionOffsets[(int)HumanBodyBones.RightUpperLeg] = new Vector3(0.0f, rightLegTransform.localPosition.y, 0.0f);
					bonePositionOffsets[(int)HumanBodyBones.Hips] = new Vector3(0.0f, -(leftLegTransform.localPosition.y + rightLegTransform.localPosition.y) * 0.5f, 0.0f);
				}
			}
		}
	}

	void CaluateOrignalRot()
	{
		for (int i = 0; i < orignalRot.Length; i++)
		{
			Transform t = boundAnimator.GetBoneTransform((HumanBodyBones)i);

			orignalRot[i] = t == null ? Quaternion.identity : t.localRotation;
		}
		for (int i = 0; i < orignalRot.Length; i++)
		{
			Quaternion parentQs = Quaternion.identity;
			Transform t = boundAnimator.GetBoneTransform((HumanBodyBones)i);
			if (t == null)
			{
				orignalParentRot[i] = Quaternion.identity;
				continue;
			}
			Transform tempParent = t.transform.parent;
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
	void CheckRigidbodySettings()
	{
		//check if rigidbodies have correct settings
		bool kinematicSetting = false;
		if (motionUpdateMethod == UpdateMethod.Physical)
		{
			kinematicSetting = true;
		}

		for (HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i)
		{
			Transform t = boundAnimator.GetBoneTransform(i);
			if (t != null)
			{
				Rigidbody r = t.GetComponent<Rigidbody>();
				if (r != null)
				{
					r.isKinematic = kinematicSetting;
				}
			}
		}
	}
}