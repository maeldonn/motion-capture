using Neuron;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Boo.Lang;
using CERV.MouvementRecognition.Animations;
using CERV.MouvementRecognition.Main;
using UniHumanoid;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


namespace CERV.MouvementRecognition.Recognition
{
    public class BvhProperties
    {
        public Bvh Bvh = null;
        public string Name = null;
        public Dictionary<string, bool[]> ValuesToIgnore;

        public BvhProperties(string path,string name, int percentageVarianceAccepted)
        {
            this.Bvh = new Bvh().GetBvhFromPath(path);
            this.Name = name;
            ValuesToIgnore = DetermineValuesToIgnore(Bvh, percentageVarianceAccepted);
        }

        public Dictionary<string, bool[]> DetermineValuesToIgnore(Bvh Bvh, int percentageVarianceAccepted)
        {
            var variance = new System.Collections.Generic.List<double>();
            foreach (var node in Bvh.Root.Traverse())       //We go through all the nodes, and we calculate the variance for each angle.
            {
                for (var i = 0; i < 3; i++)
                {
                    if (node.Name == "Hips") continue;
                    var average = 0.0;
                    var sumOfRootsSquared = 0.0;
                    for (var j = 0; j < Bvh.FrameCount; j++)
                    {
                        var pos = Bvh.GetReceivedPosition(node.Name, j, true)[i];
                        sumOfRootsSquared += Math.Pow(pos, 2);
                        average += pos;
                    }

                    average /= Bvh.FrameCount;
                    variance.Add(((1 / (double) Bvh.FrameCount) * sumOfRootsSquared) - Math.Pow(average, 2));
                }
            }
            var returnValue = new Dictionary<string, bool[]>();
            var tmpIndex = 0;
            foreach (var node in Bvh.Root.Traverse())   //We go through all the node and angle again, this time to check if the variance of each angle is superior to 1% of the maximum variance. If not, we consider it useless in the movement recognition process.
            {
                returnValue.Add(node.Name,new bool[3]);
                if (node.Name == "Hips") continue;
                for (var i = 0; i < 3; i++)
                {
                    if (variance[i + tmpIndex * 3] < (percentageVarianceAccepted/100f) * variance.Max()) returnValue[node.Name][i] = false;
                    else returnValue[node.Name][i] = true;
                }

                tmpIndex++;
            }
            return returnValue;
        }
    }

    public class MovementProperties : BvhProperties
    {
        public System.Collections.Generic.List<float> TabTimePassedBetweenFrame;
        public System.Collections.Generic.List<float> ScoreSeq;
        public System.Collections.Generic.List<int> OldNbFrame;
        public float Score;
        
        public MovementProperties(string path, string name, int percentageVarianceAccepted) : base (path, name, percentageVarianceAccepted)
        {
            Score = 0;
        }

        public void NewMvt()
        {
            if (TabTimePassedBetweenFrame == null) TabTimePassedBetweenFrame = new System.Collections.Generic.List<float>();
            if (ScoreSeq == null) ScoreSeq = new System.Collections.Generic.List<float>();
            if (OldNbFrame == null) OldNbFrame = new System.Collections.Generic.List<int>();
            if (TabTimePassedBetweenFrame.Count > 0) if (TabTimePassedBetweenFrame[TabTimePassedBetweenFrame.Count - 1] <= 0.1) return;
            TabTimePassedBetweenFrame.Add(0f); //It adds a new element to the tabTimePassedBetweenFrame list.
            ScoreSeq.Add(0f); //TODO commentaires
            OldNbFrame.Add(0); //TODO commentaires
        }
    }

    /// <summary>
    /// The <c>MvtRecognition</c> class.
    /// Contains all the methods to detect and analyze movements.
    /// <list type="bullet">
    /// <item>
    /// <term>initiateValues: </term>
    /// <description>Initiate the different values of the class.</description>
    /// </item>
    /// <item>
    /// <term>CheckIfMvtIsRight: </term>
    /// <description>Compare the movements made by the user with the one he should reproduce.</description>
    /// </item>
    /// <item>
    /// <term>checkBeginningMvt: </term>
    /// <description>Check if the user is doing the beginning of a movement.</description>
    /// </item>
    /// <item>
    /// <term>changeColorUICharacter: </term>
    /// <description>Change the color of the character attached to the UI.</description>
    /// </item>
    /// <item>
    /// <term>launchComparison: </term>
    /// <description>Exist in two versions, used to compare the user position and a frame of a bvh file.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The <c>Start()</c> and <c>Update()</c> methods are used, it might be a good idea to do the processing on another file.
    /// </remarks>
    public class MvtRecognition
    {
        private BvhProperties bvhProp = null;
        private NeuronActor actor = null;
        private int nbFrame = 0;
        private float timePassedBetweenFrame = 0;

        private float
            totalTime = 0; //Since this value does not change, and since it is used regularly, it is kept aside.

        // The gameObject of the character controlled by the player
        private GameObject player = null;

        //The gameObject of the character controlled by the bvh file
        private GameObject characterExample = null;

        //The gameObject of the hips of the
        private GameObject uiHips = null;

        //TODO
        private Store store = null;

        //Values used to check if a movement is launched
        //TODO
        private int nbFirstMvtToCheck = 0; //The number of frame needed to check if the movement is launched

        private int percentageVarianceAccepted = 0;

        private System.Collections.Generic.List<float> tabTimePassedBetweenFrame;
        private bool mvtLaunched;

        private System.Collections.Generic.List<MovementProperties> listOfMvts = null;

        

        public MvtRecognition(GameObject player, GameObject characterExample, GameObject uiHips, Store store,
            int nbFirstMvtToCheck, int percentageVarianceAccepted)
        {
            this.player = player;
            this.characterExample = characterExample;
            this.uiHips = uiHips;
            this.store = store;
            this.nbFirstMvtToCheck = nbFirstMvtToCheck;
            this.percentageVarianceAccepted = percentageVarianceAccepted;
        }

        /// <summary>
        /// Control motion recognition: if a movement is chosen, it will try to recognize the beginning of it. Then, if the movement is started, it will check if they are right.
        /// </summary>
        public void UpdateMvtRecognition()
        {
            var deltaTime = Time.deltaTime;
            if (store.Mode == Mode.Training)
            {
                if (mvtLaunched)
                {
                    // Ghost au même endroit que character
                    characterExample.transform.position = player.transform.position;
                    characterExample.transform.rotation = player.transform.rotation;

                    if (!characterExample.activeSelf) characterExample.SetActive(true);
                    mvtLaunched = CheckIfMvtIsRight(deltaTime);
                    if (characterExample != null)
                        characterExample.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame; //If a character is set, then animate him.
                }
                else
                {
                    CheckBeginningMvt(deltaTime);
                    if (characterExample.activeSelf) characterExample.SetActive(false);
                }
            }else if (store.Mode == Mode.Recognition)
            {
                CheckMultipleMovementsMethode4(deltaTime);
            }
            else
            {
                characterExample.SetActive(false);
            }

        }

        /// <summary>
        /// Initialize the NeuronAnimatorInstance actor.
        /// </summary>
        public void InitActor()
        {
            actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
        }

        /// <summary>
        /// Initialize all the values of the MvtRecognition class that are used with the movement recognition.
        /// </summary>
        public void InitMvtSet()
        {
            listOfMvts = new System.Collections.Generic.List<MovementProperties>();
            string directoryPath = "/BVH/MovementSet/";
            var itemPaths = Directory.GetFiles(Application.dataPath + directoryPath, "*.bvh");
            var itemNames = new string[itemPaths.Length];
            for (int i = 0; i < itemNames.Length; i++)
            {
                string[] splittedPath = itemPaths[i].Split('/');
                itemNames[i] = splittedPath[splittedPath.Length - 1].Split('.')[0];
            }
            for (var i = 0; i < itemPaths.Length; i++)
                listOfMvts.Add(new MovementProperties(itemPaths[i], itemNames[i], percentageVarianceAccepted));
        }



        /// <summary>
        /// Initialize all the values of the MvtRecognition class to work as intended.
        /// </summary>
        public void InitiateValuesBvh()
        {
            nbFrame = 0;
            timePassedBetweenFrame = 0;
            string[] splittedPath = store.Path.Split('/');
            var nameOfBvh = splittedPath[splittedPath.Length - 1].Split('.')[0];
            bvhProp = new BvhProperties(store.Path, nameOfBvh, percentageVarianceAccepted);
            InitActor();
            totalTime = (float) bvhProp.Bvh.FrameTime.TotalSeconds * bvhProp.Bvh.FrameCount;
        }

        /// <summary>
        /// Check if the movements of the user is the same as the one in the bvh file. It will also change the color on the character on the ui.
        /// </summary>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private bool CheckIfMvtIsRight(float deltaTime)
        {
            int margin = store.Margin;
            timePassedBetweenFrame += deltaTime;
            timePassedBetweenFrame %= totalTime;
            nbFrame = (int) ((timePassedBetweenFrame - timePassedBetweenFrame % bvhProp.Bvh.FrameTime.TotalSeconds) /
                             bvhProp.Bvh.FrameTime.TotalSeconds);
            LaunchComparison(bvhProp.Bvh.Root, bvhProp, margin, nbFrame, ChangeColorUiCharacter);
            return LaunchComparison(bvhProp.Bvh.Root, bvhProp, margin, nbFrame);
        }

        /// <summary>
        /// Tries to recognize the movement of the bvh in the user.
        /// </summary>
        /// <returns>Return true if a movement is detected.</returns>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private void CheckBeginningMvt(float deltaTime)
        {
            int margin = store.Margin;
            if (LaunchComparison(bvhProp.Bvh.Root, bvhProp, margin, 0)
            ) //If the user have a position corresponding to the first frame of the movement
            {
                if (tabTimePassedBetweenFrame == null) tabTimePassedBetweenFrame = new System.Collections.Generic.List<float>();
                tabTimePassedBetweenFrame.Add(0f); //It adds a new element to the tabTimePassedBetweenFrame list.
            }
            
            if (tabTimePassedBetweenFrame != null) //If the list is not empty
            {
                for (var i = 0; i < tabTimePassedBetweenFrame.Count; i++) //We go through it
                {
                    tabTimePassedBetweenFrame[i] += deltaTime; //Updating the time since the first frame was detected
                    if (tabTimePassedBetweenFrame[i] >= (float)bvhProp.Bvh.FrameTime.TotalSeconds * nbFirstMvtToCheck
                    ) //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                    {
                        //The first X frames have been detected, we start the movement recognition
                        mvtLaunched = true;
                        timePassedBetweenFrame = tabTimePassedBetweenFrame[i];
                        tabTimePassedBetweenFrame = null;
                        return;
                    }

                    nbFrame = (int) ((tabTimePassedBetweenFrame[i] -
                                      tabTimePassedBetweenFrame[i] % bvhProp.Bvh.FrameTime.TotalSeconds) /
                                     bvhProp.Bvh.FrameTime.TotalSeconds);
                    if (!LaunchComparison(bvhProp.Bvh.Root, bvhProp, margin, nbFrame)
                    ) //If the position of the user does not correspond to that of the frame
                    {
                        tabTimePassedBetweenFrame
                            .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Tries to recognize the movements made by the user from multiple bvh.
        /// </summary>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private void CheckMultipleMovementsMethode1(float deltaTime)
        {
            foreach (var mvt in listOfMvts)
            {
                var margin = store.Margin;
                if (LaunchComparison(mvt.Bvh.Root, mvt, margin, 0)
                ) //If the user have a position corresponding to the first frame of the movement
                {
                    if (mvt.TabTimePassedBetweenFrame == null) mvt.TabTimePassedBetweenFrame = new System.Collections.Generic.List<float>();
                    mvt.TabTimePassedBetweenFrame.Add(0f); //It adds a new element to the tabTimePassedBetweenFrame list.
                }

                if (mvt.TabTimePassedBetweenFrame != null) //If the list is not empty
                {
                    for (var i = mvt.TabTimePassedBetweenFrame.Count-1; i >=0 ; i--) //We go through it
                    {
                        mvt.TabTimePassedBetweenFrame[i] +=
                            deltaTime; //Updating the time since the first frame was detected
                        if (mvt.TabTimePassedBetweenFrame[i] >= (float)mvt.Bvh.FrameTime.TotalSeconds * mvt.Bvh.FrameCount
                        ) //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                        {
                            //The first X frames have been detected, we start the movement recognition
                            Debug.Log(mvt.Name);  //TODO: le remettre dans le code
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            continue;
                        }

                        nbFrame = (int) ((mvt.TabTimePassedBetweenFrame[i] -
                                          mvt.TabTimePassedBetweenFrame[i] % mvt.Bvh.FrameTime.TotalSeconds) /
                                         mvt.Bvh.FrameTime.TotalSeconds);
                        if (!LaunchComparison(mvt.Bvh.Root, mvt, margin, nbFrame)
                        ) //If the position of the user does not correspond to that of the frame
                        {
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to recognize the movements made by the user from multiple bvh.
        /// </summary>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private void CheckMultipleMovementsMethode2(float deltaTime)
        {
            foreach (var mvt in listOfMvts)
            {
                var margin = store.Margin;
                mvt.Score = 0;
                if (LaunchComparison(mvt.Bvh.Root, mvt, margin, 0)
                ) //If the user have a position corresponding to the first frame of the movement
                {
                    if (mvt.TabTimePassedBetweenFrame == null) mvt.TabTimePassedBetweenFrame = new System.Collections.Generic.List<float>();
                    if (mvt.ScoreSeq == null) mvt.ScoreSeq = new System.Collections.Generic.List<float>();
                    mvt.TabTimePassedBetweenFrame.Add(0f); //It adds a new element to the tabTimePassedBetweenFrame list.
                    mvt.ScoreSeq.Add(0f); //TODO commentaires
                }

                if (mvt.TabTimePassedBetweenFrame != null) //If the list is not empty
                {
                    for (var i = mvt.TabTimePassedBetweenFrame.Count - 1; i >= 0; i--) //We go through it
                    {
                        mvt.TabTimePassedBetweenFrame[i] +=
                            deltaTime; //Updating the time since the first frame was detected
                        if (mvt.TabTimePassedBetweenFrame[i] >= (float)mvt.Bvh.FrameTime.TotalSeconds * mvt.Bvh.FrameCount
                        ) //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                        {
                            //The first X frames have been detected, we start the movement recognition
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            continue;
                        }
                        nbFrame = (int)((mvt.TabTimePassedBetweenFrame[i] -
                                         mvt.TabTimePassedBetweenFrame[i] % mvt.Bvh.FrameTime.TotalSeconds) /
                                        mvt.Bvh.FrameTime.TotalSeconds);
                        if (!LaunchComparison(mvt.Bvh.Root, mvt, margin, nbFrame)
                        ) //If the position of the user does not correspond to that of the frame
                        {
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            continue;
                        }

                        mvt.ScoreSeq[i] = LaunchComparison(mvt.Bvh.Root, mvt, nbFrame);
                    }
                    if (mvt.ScoreSeq.Count>0) mvt.Score = (float) Math.Round(mvt.ScoreSeq.Min(), 1);
                    else mvt.Score = -1f;
                }
                else
                {
                    mvt.Score = -1f;
                }
                Debug.Log(mvt.Name+" score: "+mvt.Score);
            }
        }

        /// <summary>
        /// Tries to recognize the movements made by the user from multiple bvh.
        /// </summary>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private void CheckMultipleMovementsMethode3(float deltaTime)
        {
            foreach (var mvt in listOfMvts)
            {
                var margin = store.Margin;
                mvt.Score = 0;
                if (LaunchComparison(mvt.Bvh.Root, mvt, margin, 0)
                ) //If the user have a position corresponding to the first frame of the movement
                {
                    mvt.NewMvt();
                }

                if (mvt.TabTimePassedBetweenFrame != null) //If the list is not empty
                {
                    for (var i = mvt.TabTimePassedBetweenFrame.Count - 1; i >= 0; i--) //We go through it
                    {
                        mvt.TabTimePassedBetweenFrame[i] +=
                            deltaTime; //Updating the time since the first frame was detected
                        if (mvt.TabTimePassedBetweenFrame[i] >= (float)mvt.Bvh.FrameTime.TotalSeconds * mvt.Bvh.FrameCount
                        ) //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                        {
                            //The first X frames have been detected, we start the movement recognition
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            mvt.OldNbFrame.RemoveAt(i);
                            continue;
                        }
                        nbFrame = (int)((mvt.TabTimePassedBetweenFrame[i] -
                                         mvt.TabTimePassedBetweenFrame[i] % mvt.Bvh.FrameTime.TotalSeconds) /
                                        mvt.Bvh.FrameTime.TotalSeconds);
                        if (nbFrame% mvt.Bvh.FrameCount < (mvt.OldNbFrame[i] + 30)%mvt.Bvh.FrameCount) continue;
                        if (!LaunchComparison(mvt.Bvh.Root, mvt, margin, nbFrame)
                        ) //If the position of the user does not correspond to that of the frame
                        {
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            mvt.OldNbFrame.RemoveAt(i);
                            continue;
                        }
                        mvt.OldNbFrame[i] = nbFrame;
                        mvt.ScoreSeq[i] = LaunchComparison(mvt.Bvh.Root, mvt, nbFrame);
                    }
                    if (mvt.ScoreSeq.Count > 0) mvt.Score = (float)Math.Round(mvt.ScoreSeq.Min(), 1);
                    else mvt.Score = -1f;
                }
                else
                {
                    mvt.Score = -1f;
                }
                if(mvt.Score >-1) Debug.Log(mvt.Name + " score: " + mvt.Score);
            }
        }

        //TODO: changer les commentaires, pour l'instant j'ai juste copié collé la fonction CheckBeginningMvt
        /// <summary>
        /// Tries to recognize the movements made by the user from multiple bvh.
        /// </summary>
        /// <returns>Return true if a movement is detected. Debug the movement made.</returns>
        /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
        private void CheckMultipleMovementsMethode4(float deltaTime)
        {
            foreach (var mvt in listOfMvts)
            {
                var margin = store.Margin;
                mvt.Score = 0;

                if (LaunchComparison(mvt.Bvh.Root, mvt, margin, 0)
                ) //If the user have a position corresponding to the first frame of the movement
                {
                    mvt.NewMvt();
                }

                if (mvt.TabTimePassedBetweenFrame != null) //If the list is not empty
                {
                    for (var i = mvt.TabTimePassedBetweenFrame.Count - 1; i >= 0; i--) //We go through it
                    {
                        mvt.TabTimePassedBetweenFrame[i] +=
                            deltaTime; //Updating the time since the first frame was detected
                        if (mvt.TabTimePassedBetweenFrame[i] >= (float)mvt.Bvh.FrameTime.TotalSeconds * mvt.Bvh.FrameCount
                        ) //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                        {
                            //The first X frames have been detected, we start the movement recognition
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            mvt.OldNbFrame.RemoveAt(i);
                            continue;
                        }
                        nbFrame = (int)((mvt.TabTimePassedBetweenFrame[i] -
                                         mvt.TabTimePassedBetweenFrame[i] % mvt.Bvh.FrameTime.TotalSeconds) /
                                        mvt.Bvh.FrameTime.TotalSeconds);

                        if (nbFrame % mvt.Bvh.FrameCount < (mvt.OldNbFrame[i] + 30) % mvt.Bvh.FrameCount) continue;
                        var score = LaunchComparisonUpdated(mvt.Bvh.Root, mvt, nbFrame);
                        if (score<1-margin/90f
                        ) //If the position of the user does not correspond to that of the frame
                        {
                            mvt.TabTimePassedBetweenFrame
                                .RemoveAt(i); //Remove this element of the tabTimePassedBetweenFrame list
                            mvt.ScoreSeq
                                .RemoveAt(i); //Remove this element of the ScoreSeq list
                            mvt.OldNbFrame.RemoveAt(i);
                            continue;
                        }
                        mvt.OldNbFrame[i] = nbFrame;
                        mvt.ScoreSeq[i] = score;
                    }
                    if (mvt.ScoreSeq.Count > 0) mvt.Score = (float)Math.Round(mvt.ScoreSeq.Max(), 3);
                    else mvt.Score = 0f;
                }
                else
                {
                    mvt.Score = 0f;
                }
                if(mvt.Score>=0.5) Debug.Log(mvt.Name + " score: " + mvt.Score);
            }
        }

        /// <summary>
        /// Changes the color of the parts of the character on the user interface to red or green.
        /// </summary>
        /// <returns>The code error. For now it only return 0.</returns>
        /// <param name="node">The node whose color you want to change</param>
        /// <param name="color">The color you want to change: true equal green, false equal red</param>
        private int ChangeColorUiCharacter(BvhNode node, bool color) //If true: color green, else color red
        {
            foreach (var c in uiHips.transform.GetComponentsInChildren<Transform>())
            {
                if (node.Name == c.name)
                {
                    c.GetComponent<RawImage>().color = color ? Color.green : Color.red;
                    break;
                }
            }

            return 0;
        }

        /// <summary>
        /// Compare the position of the actor and a frame of <paramref name="animationToCompare"/>, and launch a function for every node.
        /// </summary>
        /// <example>
        /// <code>
        /// launchComparison(bvhHand, bvhProp, 40 ,23,changeColorUICharacter)
        /// </code>
        /// </example>
        /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
        /// <param name="animationToCompare">The <c>BvhProperties</c> which contain the movement to compare.</param>
        /// <param name="degOfMargin">The margin angle with which the user can make the movement.</param>
        /// <param name="frame">The index of the frame to look.</param>
        /// <param name="functionCalledAtEveryNode">A function taking as arguments a <c>BvhNode</c> and a <c>bool</c>, and which return an int.</param>
        public void LaunchComparison(BvhNode root, BvhProperties animationToCompare, int degOfMargin,
            int frame, Func<BvhNode, bool, int> functionCalledAtEveryNode)
        {
            var valToIgnore = animationToCompare.ValuesToIgnore;
            var bvh = animationToCompare.Bvh;
            if (degOfMargin < 0) throw new ArgumentOutOfRangeException(nameof(degOfMargin));
            foreach (var node in root.Traverse())
            {
                var checkValidity = true;
                var actorRotation = actor.GetReceivedRotation((NeuronBones) Enum.Parse(typeof(NeuronBones), node.Name));
                for (var j = 0; j < 3; j++)
                {
                    if (!valToIgnore[node.Name][j])
                    {
                        continue;
                    }

                    if (Math.Abs(actorRotation[j] -
                                 bvh.GetReceivedPosition(node.Name, frame, true)[j]) >=
                        degOfMargin)
                    {
                        checkValidity = false;
                        break;
                    }
                }
                functionCalledAtEveryNode.Invoke(node, checkValidity);
            }
        }

        /// <summary>
        /// Compare the position of the actor and a frame of <paramref name="animationToCompare"/>.
        /// </summary>
        /// <returns>
        /// The result of the comparison (true or false)
        /// </returns>
        /// <example>
        /// <code>
        /// if(launchComparison(bvhHand, bvhProp, 40,23))
        /// {
        ///     Debug.Log("The movements of the hand are corresponding.");
        /// }
        /// </code>
        /// </example>
        /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
        /// <param name="animationToCompare">The <c>BvhProperties</c> which contain the movement to compare.</param>
        /// <param name="degOfMargin">The margin angle with which the user can make the movement.</param>
        /// <param name="frame">The index of the frame to look.</param>
        public bool LaunchComparison(BvhNode root, BvhProperties animationToCompare, int degOfMargin,
            int frame)
        {
            var valToIgnore = animationToCompare.ValuesToIgnore;
            var bvh = animationToCompare.Bvh;
            if (degOfMargin < 0) throw new ArgumentOutOfRangeException(nameof(degOfMargin));
            foreach (var node in root.Traverse())
            {
                var actorRotation = actor.GetReceivedRotation((NeuronBones)Enum.Parse(typeof(NeuronBones), node.Name));
                for (var j = 0; j < 3; j++)
                {
                    if (!valToIgnore[node.Name][j])
                    {
                        continue;
                    }
                    if (Math.Abs(actorRotation[j] - bvh.GetReceivedPosition(node.Name, frame, true)[j]) >=
                        degOfMargin) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compare the position of the actor and a frame of <paramref name="animationToCompare"/>.
        /// </summary>
        /// <returns>
        /// The result of the comparison, the bigger is not the better
        /// </returns>
        /// <example>
        /// <code>
        /// if(launchComparison(bvhHand, bvhProp, 40, 23)<=1)
        /// {
        ///     Debug.Log("The movements of the hand are corresponding.");
        /// }
        /// </code>
        /// </example>
        /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
        /// <param name="animationToCompare">The <c>BvhProperties</c> which contain the movement to compare.</param>
        /// <param name="frame">The index of the frame to look.</param>
        public float LaunchComparison(BvhNode root, BvhProperties animationToCompare,
            int frame)
        {
            var valToIgnore = animationToCompare.ValuesToIgnore;
            var bvh = animationToCompare.Bvh;
            var checkValidity = 0f;
            var i = 0f;
            var adjustor = 4 / store.Margin;
            foreach (var node in root.Traverse())
            {
                i++;
                var actorRotation = actor.GetReceivedRotation((NeuronBones)Enum.Parse(typeof(NeuronBones), node.Name));
                for (var j = 0; j < 3; j++)
                {
                    if (!valToIgnore[node.Name][j])
                    {
                        continue;
                    }

                    checkValidity += (float)Math.Pow(adjustor * Math.Abs(actorRotation[j] - bvh.GetReceivedPosition(node.Name, frame, true)[j]), 2);
                }
            }
            return checkValidity;
        }

        /// <summary>
        /// Compare the position of the actor and a frame of <paramref name="animationToCompare"/>.
        /// </summary>
        /// <returns>
        /// The result of the comparison (0 to 1, one being exactly the movement)
        /// </returns>
        /// <example>
        /// <code>
        /// if(launchComparison(bvhHand, bvhProp, 40, 23)==1)
        /// {
        ///     Debug.Log("The movements of the hand are corresponding.");
        /// }
        /// </code>
        /// </example>
        /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
        /// <param name="animationToCompare">The <c>BvhProperties</c> which contain the movement to compare.</param>
        /// <param name="frame">The index of the frame to look.</param>
        public float LaunchComparisonUpdated(BvhNode root, BvhProperties animationToCompare,
            int frame)
        {

            var bvh = animationToCompare.Bvh;
            var valToIgnore = animationToCompare.ValuesToIgnore;
            var checkValidity = 0f;
            var i = 0f;
            var nbIgnoredValue = 0;
            foreach (var node in root.Traverse())
            {
                i++;
                var actorRotation = actor.GetReceivedRotation((NeuronBones)Enum.Parse(typeof(NeuronBones), node.Name));
                for (int j = 0; j < 3; j++)
                {
                    if (!valToIgnore[node.Name][j])
                    {
                        nbIgnoredValue++;
                        continue;
                    }

                    checkValidity += (1 - Math.Abs(actorRotation[j] - bvh.GetReceivedPosition(node.Name, frame, true)[j]) / 90f);
                }
            }
            return checkValidity/(3f*i-nbIgnoredValue);
        }

        /// <summary>
        /// Compare the position of the actor and a frame of <paramref name="animationToCompare"/>. Old function, could be better idk.
        /// </summary>
        /// <returns>
        /// The result of the comparison (true or false)
        /// </returns>
        /// <example>
        /// <code>
        /// if(launchComparison(bvhHand, bvh, new String[] {"Thumb"},40,23))
        /// {
        ///     Debug.Log("The movements of the hand are corresponding.");
        /// }
        /// </code>
        /// </example>
        /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
        /// <param name="inputBvh">The <c>Bvh</c> which contain the movement to compare.</param>
        /// <param name="partsToIgnore">The name of the nodes to ignore.</param>
        /// <param name="degOfMargin">The margin angle with which the user can make the movement.</param>
        /// <param name="frame">The index of the frame to look.</param>
        public bool LaunchComparisonPointing(BvhNode root, Bvh inputBvh, string[] partsToIgnore, int degOfMargin,
            int frame)
        {
            var bvh = inputBvh;
            if (degOfMargin < 0) throw new ArgumentOutOfRangeException(nameof(degOfMargin));
            foreach (var node in root.Traverse())
            {
                if (partsToIgnore.Any(node.Name.Contains)) continue;
                var actorRotation = actor.GetReceivedRotation((NeuronBones) Enum.Parse(typeof(NeuronBones), node.Name));
                for (var j = 0; j < 3; j++)
                {
                    if (Math.Abs(actorRotation[j] - bvh.GetReceivedPosition(node.Name, frame, true)[j]) >=
                        degOfMargin) return false;
                }
            }

            return true;
        }
    }
}