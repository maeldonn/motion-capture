﻿using CERV.MouvementRecognition.Recognition;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CERV.MouvementRecognition.Main;
using Neuron;
using UniHumanoid;
using UnityEngine;

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
    public class PointingHandler
    {
        private BvhProperties idlePointing = null;
        private BvhProperties BVHactivating = null;

        private GameObject player = null;

        private int degreeOfMarginPointing = 0;

        private int degreeOfMarginValidating = 0;

        private LineRenderer lineMenu = null;

        private GameObject leftHand = null;

        private simpleStateMachine stateConfirm;
        private simpleStateMachine statePointing;

        public AudioClip clipConfirm = null;

        public AudioClip clipPointing = null;

        private MvtRecognition mvtRecognition = null;

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
                    Debug.Log("CSV file created at the following location: " + Path.Combine(Application.persistentDataPath, "scores.csv"));
                    exportToCsv(mvtRecognition.listOfMvts);
                }
                else
                {
                    foreach (var mvt in mvtRecognition.listOfMvts)
                    {
                        mvt.ClrScoreRecord();
                    }

                    mvtRecognition.TimeSinceStartRecord = 0f;
                    Debug.Log("Recording started!");
                }
                mvtRecognition.RecordingScore = !mvtRecognition.RecordingScore;
            }
        }

        /// <summary>
        /// This method is used to export a recording of the scores to a csv file.
        /// </summary>
        /// <param name="listMvt">A list of MovementProperties; it will export the scores of all these MovementProperties.</param>

        private void exportToCsv(List<MovementProperties> listMvt)
        {
            if (listMvt == null || listMvt.Count == 0)
            {
                throw new ArgumentException("There are no movement to detect!");
            }
            if (listMvt[0].ScoreRecorded==null || listMvt[0].ScoreRecorded[0].Count == 0)
            {
                throw new ArgumentException("Recording session too short!");
            }

            List<string[]> rowData = new List<string[]>();

            // Creating First row of titles manually..
            string[] rowDataTemp = new string[listMvt.Count+1];
            rowDataTemp[0] = "time";
            for (int i=1; i<rowDataTemp.Length; i++)
            {
                rowDataTemp[i] = listMvt[i-1].Name;
            }
            rowData.Add(rowDataTemp);

            // You can add up the values in as many cells as you want.
            for (int i = 0; i < listMvt[0].ScoreRecorded[0].Count; i++)
            {
                rowDataTemp = new string[listMvt.Count+1];
                rowDataTemp[0] = listMvt[0].ScoreRecorded[0][i].ToString(CultureInfo.InvariantCulture);
                for (int j = 1; j < listMvt.Count+1; j++)
                {
                    rowDataTemp[j] = listMvt[j-1].ScoreRecorded[1][i].ToString(CultureInfo.InvariantCulture);
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
                            .Children[0].Children[0].Children[0], BVHactivating.Bvh, new string[0], degreeOfMarginValidating,
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