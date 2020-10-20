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

using Neuron;
using UniHumanoid;
using UnityEngine;

using CERV.MouvementRecognition.Main;

namespace CERV.MouvementRecognition.Animations
{

    /// <summary>
    /// The <c>NeuronAnimatorInstanceBVH</c> class.
    /// Contains all the methods to animate a character with a .bvh file. Most of this class is from a pre-existing class on the Neuron library.
    /// <list type="bullet">
    /// <item>
    /// <term>SetPosition: </term>
    /// <description>Set position for bone in animator.</description>
    /// </item>
    /// <item>
    /// <term>SetRotation: </term>
    /// <description>Set rotation for bone in animator.</description>
    /// </item>
    /// <item>
    /// <term>ApplyMotion: </term>
    /// <description>Apply transforms extracted from the .bvh file data to transforms of animator bones.</description>
    /// </item>
    /// <item>
    /// <term>EulerToQuaternion: </term>
    /// <description>Convert an euler angle to a quaternion.</description>
    /// </item>
    /// <item>
    /// <term>ReleasePhysicalContext: </term>
    /// <description>Call the release method on the PhysicalReference object. TODO: go and see what exactly it does.</description>
    /// </item>
    /// <item>
    /// <term>UpdateOffset: </term>
    /// <description>We do some adjustment for the bones here which would replaced by our model retargeting later.</description>
    /// </item>
    /// <item>
    /// <term>CalculateOriginalRot: </term>
    /// <description>Calculate the original rotation of the model. TODO: go and see what exactly it does.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The <c>Start()</c> and <c>Update()</c> methods are used, it might be a good idea to do the processing on another file.
    /// </remarks>
    public class NeuronAnimatorInstanceBVH : MonoBehaviour
    {
        [SerializeField] private Store store = null;

        public bool UseNewRig = true;
        public Animator BoundAnimator = null;
        public UpdateMethod MotionUpdateMethod = UpdateMethod.Normal;
        public bool EnableHipsMovement = true;

        public Animator
            PhysicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference

        public NeuronAnimatorPhysicalReference PhysicalReference;
        private Vector3[] bonePositionOffsets;
        private Vector3[] boneRotationOffsets;

        public float VelocityMagic = 3000.0f;
        public float AngularVelocityMagic = 20.0f;
        private Quaternion[] orignalRot;
        private Quaternion[] orignalParentRot;

        public NeuronAnimatorInstanceBVH(Animator animator, NeuronActor actor)
        {
            BoundAnimator = animator;
            UpdateOffset();
        }

        public NeuronAnimatorInstanceBVH(NeuronActor actor)
        {
        }

        private bool inited = false;

        private void OnEnable()
        {
            if (inited)
            {
                return;
            }

            inited = true;
            if (BoundAnimator == null)
            {
                BoundAnimator = GetComponent<Animator>();
            }

            bonePositionOffsets = new Vector3[(int)HumanBodyBones.LastBone];
            boneRotationOffsets = new Vector3[(int)HumanBodyBones.LastBone];

            orignalRot = new Quaternion[(int)HumanBodyBones.LastBone];
            orignalParentRot = new Quaternion[(int)HumanBodyBones.LastBone];
            PhysicalReference = new NeuronAnimatorPhysicalReference();
            UpdateOffset();
            CalculateOriginalRot();
        }

        private void Update()
        {
            // TODO: Update this line
            bvh = store.Bvh;

            if (BoundAnimator != null && MotionUpdateMethod == UpdateMethod.Normal)
            {
                if (PhysicalReference.Initiated())
                {
                    ReleasePhysicalContext();
                }
                if (bvh != null) ApplyMotion(BoundAnimator, bonePositionOffsets, boneRotationOffsets);
            }
        }

        public bool applyPosition;
        private int m_nbFrame;
        private Bvh bvh;

        public int NbFrame
        {
            get => m_nbFrame;
            set
            {
                if (m_nbFrame == value) return;
                m_nbFrame = value;
            }
        }

        private void Start()
        {
            NbFrame = 0;
        }

        // set position for bone in animator
        private void SetPosition(Animator animator, HumanBodyBones bone, Vector3 pos)
        {
            var t = animator.GetBoneTransform(bone);
            if (t != null)
            {
                if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z))
                {
                    //	t.localPosition = pos;

                    var srcP = pos;
                    var finalP = Quaternion.Inverse(orignalParentRot[(int) bone]) * srcP;
                    t.localPosition = finalP;
                }
            }
        }

        // set rotation for bone in animator
        private void SetRotation(Animator animator, HumanBodyBones bone, Vector3 rotation)
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
                        orignalBoneRot = orignalRot[(int) bone];
                    }

                    Quaternion srcQ = rot;

                    Quaternion usedQ = Quaternion.Inverse(orignalParentRot[(int) bone]) * srcQ *
                                       orignalParentRot[(int) bone];
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
            if (EnableHipsMovement)
            {
                //Debug.Log(bvh);
                SetPosition(animator, HumanBodyBones.Hips,
                    bvh.GetReceivedPosition("Hips", NbFrame, false) + positionOffsets[(int) HumanBodyBones.Hips]);
                SetRotation(animator, HumanBodyBones.Hips, bvh.GetReceivedPosition("Hips", NbFrame, true));
            }

            // apply positions
            if (applyPosition) //actor.withDisplacement)
            {
                // legs
                SetPosition(animator, HumanBodyBones.RightUpperLeg,
                    bvh.GetReceivedPosition("RightUpLeg", NbFrame, false) +
                    positionOffsets[(int) HumanBodyBones.RightUpperLeg]);
                SetPosition(animator, HumanBodyBones.RightLowerLeg,
                    bvh.GetReceivedPosition("RightLeg", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightFoot, bvh.GetReceivedPosition("RightFoot", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftUpperLeg,
                    bvh.GetReceivedPosition("LeftUpLeg", NbFrame, false) +
                    positionOffsets[(int) HumanBodyBones.LeftUpperLeg]);
                SetPosition(animator, HumanBodyBones.LeftLowerLeg, bvh.GetReceivedPosition("LeftLeg", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftFoot, bvh.GetReceivedPosition("LeftFoot", NbFrame, false));

                // spine
                SetPosition(animator, HumanBodyBones.Spine, bvh.GetReceivedPosition("Spine", NbFrame, false));
                if (UseNewRig)
                {
                    SetPosition(animator, HumanBodyBones.Chest,
                        bvh.GetReceivedPosition("Spine1", NbFrame, false)
                    );
#if UNITY_2018_2_OR_NEWER
                    SetPosition(animator, HumanBodyBones.UpperChest,
                        bvh.GetReceivedPosition("Spine2", NbFrame, false)
                    );
#endif
                    SetPosition(animator, HumanBodyBones.Neck,
                        bvh.GetReceivedPosition("Neck", NbFrame, false) +
                        (EulerToQuaternion(bvh.GetReceivedPosition("Neck", NbFrame, false)) *
                         bvh.GetReceivedPosition("Neck1", NbFrame, false))
                    );
                }
                else
                {
                    SetPosition(animator, HumanBodyBones.Chest, bvh.GetReceivedPosition("Spin3", NbFrame, false));
                    SetPosition(animator, HumanBodyBones.Neck, bvh.GetReceivedPosition("Neck", NbFrame, false));
                }

                SetPosition(animator, HumanBodyBones.Head, bvh.GetReceivedPosition("Head", NbFrame, false));

                // right arm
                SetPosition(animator, HumanBodyBones.RightShoulder,
                    bvh.GetReceivedPosition("RightShoulder", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightUpperArm,
                    bvh.GetReceivedPosition("RightArm", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightLowerArm,
                    bvh.GetReceivedPosition("RightForeArm", NbFrame, false));

                // right hand
                SetPosition(animator, HumanBodyBones.RightHand, bvh.GetReceivedPosition("RightHand", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightThumbProximal,
                    bvh.GetReceivedPosition("RightHandThumb1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightThumbIntermediate,
                    bvh.GetReceivedPosition("RightHandThumb2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightThumbDistal,
                    bvh.GetReceivedPosition("RightHandThumb3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.RightIndexProximal,
                    bvh.GetReceivedPosition("RightHandIndex1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightIndexIntermediate,
                    bvh.GetReceivedPosition("RightHandIndex2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightIndexDistal,
                    bvh.GetReceivedPosition("RightHandIndex3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.RightMiddleProximal,
                    bvh.GetReceivedPosition("RightHandMiddle1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightMiddleIntermediate,
                    bvh.GetReceivedPosition("RightHandMiddle2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightMiddleDistal,
                    bvh.GetReceivedPosition("RightHandMiddle3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.RightRingProximal,
                    bvh.GetReceivedPosition("RightHandRing1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightRingIntermediate,
                    bvh.GetReceivedPosition("RightHandRing2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightRingDistal,
                    bvh.GetReceivedPosition("RightHandRing3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.RightLittleProximal,
                    bvh.GetReceivedPosition("RightHandPinky1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightLittleIntermediate,
                    bvh.GetReceivedPosition("RightHandPinky2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.RightLittleDistal,
                    bvh.GetReceivedPosition("RightHandPinky3", NbFrame, false));

                // left arm
                SetPosition(animator, HumanBodyBones.LeftShoulder,
                    bvh.GetReceivedPosition("LeftShoulder", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftUpperArm, bvh.GetReceivedPosition("LeftArm", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftLowerArm,
                    bvh.GetReceivedPosition("LeftForeArm", NbFrame, false));

                // left hand
                SetPosition(animator, HumanBodyBones.LeftHand, bvh.GetReceivedPosition("LeftHand", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftThumbProximal,
                    bvh.GetReceivedPosition("LeftHandThumb1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftThumbIntermediate,
                    bvh.GetReceivedPosition("LeftHandThumb2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftThumbDistal,
                    bvh.GetReceivedPosition("LeftHandThumb3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.LeftIndexProximal,
                    bvh.GetReceivedPosition("LeftHandIndex1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftIndexIntermediate,
                    bvh.GetReceivedPosition("LeftHandIndex2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftIndexDistal,
                    bvh.GetReceivedPosition("LeftHandIndex3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.LeftMiddleProximal,
                    bvh.GetReceivedPosition("LeftHandMiddle1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftMiddleIntermediate,
                    bvh.GetReceivedPosition("LeftHandMiddle2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftMiddleDistal,
                    bvh.GetReceivedPosition("LeftHandMiddle3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.LeftRingProximal,
                    bvh.GetReceivedPosition("LeftHandRing1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftRingIntermediate,
                    bvh.GetReceivedPosition("LeftHandRing2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftRingDistal,
                    bvh.GetReceivedPosition("LeftHandRing3", NbFrame, false));

                SetPosition(animator, HumanBodyBones.LeftLittleProximal,
                    bvh.GetReceivedPosition("LeftHandPinky1", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftLittleIntermediate,
                    bvh.GetReceivedPosition("LeftHandPinky2", NbFrame, false));
                SetPosition(animator, HumanBodyBones.LeftLittleDistal,
                    bvh.GetReceivedPosition("LeftHandPinky3", NbFrame, false));
            }

            // apply rotations

            // legs
            SetRotation(animator, HumanBodyBones.RightUpperLeg, bvh.GetReceivedPosition("RightUpLeg", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightLowerLeg, bvh.GetReceivedPosition("RightLeg", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightFoot, bvh.GetReceivedPosition("RightFoot", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftUpperLeg, bvh.GetReceivedPosition("LeftUpLeg", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftLowerLeg, bvh.GetReceivedPosition("LeftLeg", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftFoot, bvh.GetReceivedPosition("LeftFoot", NbFrame, true));

            // spine
            SetRotation(animator, HumanBodyBones.Spine, bvh.GetReceivedPosition("Spine", NbFrame, true));
            //SetRotation( animator, HumanBodyBones.Chest,					actor.GetReceivedRotation( NeuronBones.Spine1 ) + actor.GetReceivedRotation( NeuronBones.Spine2 ) + actor.GetReceivedRotation( NeuronBones.Spine3 ) );
            if (UseNewRig)
            {
                SetRotation(animator, HumanBodyBones.Chest,
                    (EulerToQuaternion(bvh.GetReceivedPosition("Spine1", NbFrame, true)) *
                     EulerToQuaternion(bvh.GetReceivedPosition("Spine2", NbFrame, true))).eulerAngles
                );

                SetRotation(animator, HumanBodyBones.Neck,
                    (EulerToQuaternion(bvh.GetReceivedPosition("Neck", NbFrame, true)) *
                     EulerToQuaternion(bvh.GetReceivedPosition("Neck1", NbFrame, true))).eulerAngles
                );
            }
            else
            {
                SetRotation(animator, HumanBodyBones.Chest,
                    (EulerToQuaternion(bvh.GetReceivedPosition("Spine1", NbFrame, true)) *
                     EulerToQuaternion(bvh.GetReceivedPosition("Spine2", NbFrame, true)) *
                     EulerToQuaternion(bvh.GetReceivedPosition("Spine3", NbFrame, true))).eulerAngles
                );
                SetRotation(animator, HumanBodyBones.Neck, bvh.GetReceivedPosition("Neck", NbFrame, true));
            }

            SetRotation(animator, HumanBodyBones.Head, bvh.GetReceivedPosition("Head", NbFrame, true));

            // right arm
            SetRotation(animator, HumanBodyBones.RightShoulder,
                bvh.GetReceivedPosition("RightShoulder", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightUpperArm, bvh.GetReceivedPosition("RightArm", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightLowerArm, bvh.GetReceivedPosition("RightForeArm", NbFrame, true));

            // right hand
            SetRotation(animator, HumanBodyBones.RightHand, bvh.GetReceivedPosition("RightHand", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightThumbProximal,
                bvh.GetReceivedPosition("RightHandThumb1", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightThumbIntermediate,
                bvh.GetReceivedPosition("RightHandThumb2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightThumbDistal,
                bvh.GetReceivedPosition("RightHandThumb3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.RightIndexProximal,		actor.GetReceivedRotation( NeuronBones.RightHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandIndex ) );
            SetRotation(animator, HumanBodyBones.RightIndexProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("RightInHandIndex", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("RightHandIndex1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.RightIndexIntermediate,
                bvh.GetReceivedPosition("RightHandIndex2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightIndexDistal,
                bvh.GetReceivedPosition("RightHandIndex3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.RightMiddleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandMiddle ) );
            SetRotation(animator, HumanBodyBones.RightMiddleProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("RightInHandMiddle", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("RightHandMiddle1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.RightMiddleIntermediate,
                bvh.GetReceivedPosition("RightHandMiddle2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightMiddleDistal,
                bvh.GetReceivedPosition("RightHandMiddle3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.RightRingProximal,		actor.GetReceivedRotation( NeuronBones.RightHandRing1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandRing ) );
            SetRotation(animator, HumanBodyBones.RightRingProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("RightInHandRing", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("RightHandRing1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.RightRingIntermediate,
                bvh.GetReceivedPosition("RightHandRing2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightRingDistal,
                bvh.GetReceivedPosition("RightHandRing3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.RightLittleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandPinky ) );
            SetRotation(animator, HumanBodyBones.RightLittleProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("RightInHandPinky", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("RightHandPinky1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.RightLittleIntermediate,
                bvh.GetReceivedPosition("RightHandPinky2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.RightLittleDistal,
                bvh.GetReceivedPosition("RightHandPinky3", NbFrame, true));

            // left arm
            SetRotation(animator, HumanBodyBones.LeftShoulder, bvh.GetReceivedPosition("LeftShoulder", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftUpperArm, bvh.GetReceivedPosition("LeftArm", NbFrame, true)); //
            SetRotation(animator, HumanBodyBones.LeftLowerArm,
                bvh.GetReceivedPosition("LeftForeArm", NbFrame, true)); //

            // left hand
            SetRotation(animator, HumanBodyBones.LeftHand, bvh.GetReceivedPosition("LeftHand", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftThumbProximal,
                bvh.GetReceivedPosition("LeftHandThumb1", NbFrame, true)); //
            SetRotation(animator, HumanBodyBones.LeftThumbIntermediate,
                bvh.GetReceivedPosition("LeftHandThumb2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftThumbDistal,
                bvh.GetReceivedPosition("LeftHandThumb3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.LeftIndexProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandIndex ) );
            SetRotation(animator, HumanBodyBones.LeftIndexProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandIndex", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandIndex1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.LeftIndexIntermediate,
                bvh.GetReceivedPosition("LeftHandIndex2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftIndexDistal,
                bvh.GetReceivedPosition("LeftHandIndex3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.LeftMiddleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandMiddle ) );
            SetRotation(animator, HumanBodyBones.LeftMiddleProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandMiddle", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandMiddle1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.LeftMiddleIntermediate,
                bvh.GetReceivedPosition("LeftHandMiddle2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftMiddleDistal,
                bvh.GetReceivedPosition("LeftHandMiddle3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.LeftRingProximal,			actor.GetReceivedRotation( NeuronBones.LeftHandRing1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandRing ) );
            SetRotation(animator, HumanBodyBones.LeftRingProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandRing", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandRing1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.LeftRingIntermediate,
                bvh.GetReceivedPosition("LeftHandRing2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftRingDistal,
                bvh.GetReceivedPosition("LeftHandRing3", NbFrame, true));

            //SetRotation( animator, HumanBodyBones.LeftLittleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandPinky ) );
            SetRotation(animator, HumanBodyBones.LeftLittleProximal,
                (EulerToQuaternion(bvh.GetReceivedPosition("LeftInHandPinky", NbFrame, true)) * //
                 EulerToQuaternion(bvh.GetReceivedPosition("LeftHandPinky1", NbFrame, true))).eulerAngles //
            );
            SetRotation(animator, HumanBodyBones.LeftLittleIntermediate,
                bvh.GetReceivedPosition("LeftHandPinky2", NbFrame, true));
            SetRotation(animator, HumanBodyBones.LeftLittleDistal,
                bvh.GetReceivedPosition("LeftHandPinky3", NbFrame, true));
        }

        private static Quaternion EulerToQuaternion(Vector3 euler)
        {
            return Quaternion.Euler(euler.x, euler.y, euler.z);
        }

        private void ReleasePhysicalContext()
        {
            PhysicalReference.Release();
        }

        private void UpdateOffset()
        {
            // we do some adjustment for the bones here which would replaced by our model retargeting later

            if (BoundAnimator != null)
            {
                // initiate values
                for (int i = 0; i < (int) HumanBodyBones.LastBone; ++i)
                {
                    bonePositionOffsets[i] = Vector3.zero;
                    boneRotationOffsets[i] = Vector3.zero;
                }

                if (BoundAnimator != null)
                {
                    Transform leftLegTransform = BoundAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    Transform rightLegTransform = BoundAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    if (leftLegTransform != null)
                    {
                        bonePositionOffsets[(int) HumanBodyBones.LeftUpperLeg] =
                            new Vector3(0.0f, leftLegTransform.localPosition.y, 0.0f);
                        bonePositionOffsets[(int) HumanBodyBones.RightUpperLeg] =
                            new Vector3(0.0f, rightLegTransform.localPosition.y, 0.0f);
                        bonePositionOffsets[(int) HumanBodyBones.Hips] = new Vector3(0.0f,
                            -(leftLegTransform.localPosition.y + rightLegTransform.localPosition.y) * 0.5f, 0.0f);
                    }
                }
            }
        }

        private void CalculateOriginalRot()
        {
            for (int i = 0; i < orignalRot.Length; i++)
            {
                Transform t = BoundAnimator.GetBoneTransform((HumanBodyBones) i);

                orignalRot[i] = t == null ? Quaternion.identity : t.localRotation;
            }

            for (int i = 0; i < orignalRot.Length; i++)
            {
                Quaternion parentQs = Quaternion.identity;
                Transform t = BoundAnimator.GetBoneTransform((HumanBodyBones) i);
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
    }
}