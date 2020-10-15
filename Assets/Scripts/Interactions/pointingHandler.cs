using System;
using Neuron;
using UniHumanoid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CERV.MouvementRecognition.Recognition;

namespace CERV.MouvementRecognition.Interactions
{

    public enum simpleStateMachine
    {
        Idle,
        Action,
        IdleAction
    }

    /// <summary>
    /// The <c>pointingHandler</c> class.
    /// Contains almost all the methods to handle the pointing system. To interact with the canvas some of them are in the mocapInputModule script that is attached to the EventSystem.
    /// <list type="bullet">
    /// <item>
    /// <term>UpdateUserInputs: </term>
    /// <description>Update all that is related to the user input.</description>
    /// </item>
    /// <item>
    /// <term>PlaySoundWhilePointing: </term>
    /// <description>Play a sound while pointing, and update the statePointing variable.</description>
    /// </item>
    /// <item>
    /// <term>HandleClicks: </term>
    /// <description>Handle the visual effects of the clicks.</description>
    /// </item>
    /// <item>
    /// <term>InitPointingHandler: </term>
    /// <description>Initialize the values used by the pointingHandler script.</description>
    /// </item>
    /// <item>
    /// <term>DrawLineUsedToInteract: </term>
    /// <description>Draws the line used to interact with the menu.</description>
    /// </item>
    /// <item>
    /// <term>GetConfirmState: </term>
    /// <description>Return the state of the stateConfirm variable.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The <c>Start()</c> and <c>Update()</c> methods are used, it might be a good idea to do the processing on another file.
    /// </remarks>
    public class PointingHandler
    {
        Bvh idlePointing = null;
        Bvh BVHactivating = null;
        NeuronActor actor = null;

        [SerializeField] private GameObject player = null;

        [SerializeField] int degreeOfMarginPointing = 0;

        [SerializeField] int degreeOfMarginValidating = 0;

        [SerializeField] LineRenderer lineMenu = null;

        [SerializeField] GameObject leftHand = null;

        simpleStateMachine stateConfirm;
        simpleStateMachine statePointing;

        [SerializeField] public AudioClip clipConfirm = null;

        [SerializeField] public AudioClip clipPointing = null;

        [SerializeField] MvtRecognition mvtRecognition = null;

        public PointingHandler(GameObject player, int degreeOfMarginPointing, int degreeOfMarginValidating,
            LineRenderer lineMenu, GameObject leftHand, AudioClip clipConfirm, AudioClip clipPointing,
            MvtRecognition mvtRecognition)
        {
            this.player = player;
            this.degreeOfMarginPointing = degreeOfMarginPointing;
            this.degreeOfMarginValidating = degreeOfMarginValidating;
            this.lineMenu = lineMenu;
            this.leftHand = leftHand;
            this.clipConfirm = clipConfirm;
            this.clipPointing = clipPointing;
            this.mvtRecognition = mvtRecognition;
        }

        /// <summary>
        /// Update all that is related to the user input (the pointing line and the clicks). 
        /// </summary>
        public void UpdateUserInputs()
        {
            if (statePointing != simpleStateMachine.Idle)
            {
                HandleClicks();
            }

            if (statePointing == simpleStateMachine.Action)
            {
                PlaySoundWhilePointing();
            }

            if (mvtRecognition.LaunchComparison(
                idlePointing.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0]
                    .Children[0], idlePointing, degreeOfMarginPointing, new[] {"Thumb"}, 0))
            {
                if (statePointing == simpleStateMachine.Idle)
                {
                    statePointing = simpleStateMachine.Action;
                }
                else
                {
                    statePointing = simpleStateMachine.IdleAction;
                }

                DrawLineUsedToInteract();
            }
            else
            {
                //Debug.Log("Hand not pointing");
                statePointing = simpleStateMachine.Idle;
                lineMenu.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Play a sound while pointing, and update the statePointing variable.
        /// </summary>
        public void PlaySoundWhilePointing()
        {
            SoundManager.PlaySound(clipPointing,
                leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0)
                    .position);
            statePointing = simpleStateMachine.IdleAction;
        }

        /// <summary>
        /// Handle the visual effects of the clicks.
        /// </summary>
        public void HandleClicks()
        {
            switch (stateConfirm)
            {
                case simpleStateMachine.Idle:
                    if (mvtRecognition.LaunchComparison(
                        BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0]
                            .Children[0].Children[0].Children[0], BVHactivating, degreeOfMarginValidating,
                        new string[0], 1))
                    {
                        SoundManager.PlaySound(clipConfirm,
                            leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform
                                .GetChild(0).position);
                        stateConfirm = simpleStateMachine.Action;
                        lineMenu.endColor = Color.green;
                    }

                    break;

                case simpleStateMachine.Action:
                    stateConfirm = simpleStateMachine.IdleAction;
                    break;

                case simpleStateMachine.IdleAction:
                    if (mvtRecognition.LaunchComparison(
                        BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0]
                            .Children[0].Children[0].Children[0], BVHactivating, degreeOfMarginValidating,
                        new string[0], 0))
                    {
                        stateConfirm = simpleStateMachine.Idle;
                        lineMenu.endColor = Color.white;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Initialize the values used by the pointingHandler script.
        /// </summary>
        public void InitPointingHandler()
        {
            idlePointing = new Bvh().GetBvhFromPath("Assets/BVH/Pointer/pointeur_3.bvh");
            BVHactivating = new Bvh().GetBvhFromPath("Assets/BVH/Pointer/pointeur_2.bvh");
            actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
        }

        /// <summary>
        /// Draws the line used to interact with the menu.
        /// </summary>
        public void DrawLineUsedToInteract()
        {
            lineMenu.gameObject.SetActive(true);
            var unitVector =
                leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0)
                    .position - leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position;

            Vector3 tmpPos;
            var ray = new Ray(
                leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0)
                    .position, unitVector);
            if (Physics.Raycast(ray, out var hitPoint, Mathf.Infinity))
            {
                //Debug.Log("Hit Something");
                tmpPos = hitPoint.point;
            }
            else
            {
                //Debug.Log("No collider hit");
                tmpPos = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform
                    .GetChild(0).position + unitVector * 1000;
            }

            lineMenu.SetPositions(new[]
            {
                leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0)
                    .position,
                tmpPos
            });
            lineMenu.transform.position = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0)
                .transform.GetChild(0).position;
            lineMenu.transform.LookAt(tmpPos);
        }

        public simpleStateMachine GetConfirmState()
        {
            return stateConfirm;
        }
    }
}