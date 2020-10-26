using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Neuron;
using UniHumanoid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CERV.MouvementRecognition.Recognition;
using UnityEngine.SocialPlatforms.Impl;

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
        BvhProperties idlePointing = null;
        BvhProperties BVHactivating = null;

        private GameObject player = null;

        int degreeOfMarginPointing = 0;

        int degreeOfMarginValidating = 0;

        LineRenderer lineMenu = null;

        GameObject leftHand = null;

        simpleStateMachine stateConfirm;
        simpleStateMachine statePointing;

        public AudioClip clipConfirm = null;

        public AudioClip clipPointing = null;

        MvtRecognition mvtRecognition = null;

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

            if (mvtRecognition.LaunchComparisonPointing(
                idlePointing.Bvh.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0]
                    .Children[0], idlePointing.Bvh, new[] { "Thumb" }, degreeOfMarginPointing, 0))
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

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (mvtRecognition.RecordingScore)
                {
                    //ScoreContainer scoreContainer = new ScoreContainer();
                    //foreach (var mvt in mvtRecognition.listOfMvts)
                    //{
                    //    scoreContainer.scoreMouvement.Add(new ScoreMvt(mvt.Name,mvt.ScoreRecorded));
                    //}
                    Debug.Log("XML file created at the following location: "+Path.Combine(Application.persistentDataPath, "scores.xml"));


                    exportToCsv(mvtRecognition.listOfMvts);
                    //scoreContainer.Save(Path.Combine(Application.persistentDataPath, "scores.xml"));
                }
                else
                {
                    foreach (var mvt in mvtRecognition.listOfMvts)
                    {

                        mvt.ClrScoreRecord();
                    }
                }
                mvtRecognition.RecordingScore = !mvtRecognition.RecordingScore;
            }
        }

        private void exportToCsv(List<MovementProperties> listMvt)
        {
            if (listMvt==null || listMvt.Count == 0)
            {
                throw new ArgumentException("There are no movement to detect!");
            }
            if (listMvt[0].ScoreRecorded==null || listMvt[0].ScoreRecorded.Count == 0)
            {
                throw new ArgumentException("Recording session too short!");
            }

            List<string[]> rowData = new List<string[]>();

            // Creating First row of titles manually..
            string[] rowDataTemp = new string[listMvt.Count];
            for(int i=0; i<rowDataTemp.Length; i++)
            {
                rowDataTemp[i] = listMvt[i].Name;
            }
            rowData.Add(rowDataTemp);

            // You can add up the values in as many cells as you want.
            for (int i = 0; i < listMvt[0].ScoreRecorded.Count; i++)
            {
                rowDataTemp = new string[listMvt.Count];
                for(int j = 0; j<listMvt.Count; j++)
                {
                    rowDataTemp[j] = listMvt[j].ScoreRecorded[i].ToString(CultureInfo.InvariantCulture);
                }
                rowData.Add(rowDataTemp);
            }

            string[][] output = new string[rowData.Count][];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = rowData[i];
            }

            int length = output.GetLength(0);
            string delimiter = ",";

            StringBuilder sb = new StringBuilder();

            for (int index = 0; index < length; index++)
                sb.AppendLine(string.Join(delimiter, output[index]));


            string filePath = Path.Combine(Application.persistentDataPath, "scores.csv");

            StreamWriter outStream = System.IO.File.CreateText(filePath);
            outStream.WriteLine(sb);
            outStream.Close();
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
                    if (mvtRecognition.LaunchComparisonPointing(
                        BVHactivating.Bvh.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0]
                            .Children[0].Children[0].Children[0], BVHactivating.Bvh, new string[0], degreeOfMarginValidating,
                        0))
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
                    if (mvtRecognition.LaunchComparisonPointing(
                        BVHactivating.Bvh.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0]
                            .Children[0].Children[0].Children[0], BVHactivating.Bvh,new string[0], degreeOfMarginValidating,
                         1))
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
            idlePointing = new BvhProperties("Assets/BVH/Pointer/pouce.bvh", "pouce", 0);
            BVHactivating = new BvhProperties("Assets/BVH/Pointer/pouce.bvh", "pouce", 0);
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