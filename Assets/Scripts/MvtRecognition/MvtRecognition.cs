using Neuron;
using System;
using System.Collections.Generic;
using System.Linq;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The <c>MvtRecognition</c> class.
/// Contains all methods to detect and analyse mouvements.
/// <list type="bullet">
/// <item>
/// <term>initiateValues</term>
/// <description>Initiate the different values of the class.</description>
/// </item>
/// <item>
/// <term>checkIfMvtIsRight</term>
/// <description>Compare the mouvements made by the user with the one he should reproduce.</description>
/// </item>
/// <item>
/// <term>checkBeginningMvt</term>
/// <description>Check if the user is doing the beginning of a mouvement.</description>
/// </item>
/// <item>
/// <term>changeColorUICharacter</term>
/// <description>Change the color of the character attached to the UI.</description>
/// </item>
/// <item>
/// <term>launchComparison</term>
/// <description>Exist in two versions, used to compare the user position and a frame of a bvh file.</description>
/// </item>
/// </list>
/// </summary>
/// <remarks>
/// The <c>Start()</c> and <c>Update()</c> methods are used, it might be a good idea to do the treatment on another file.
/// </remarks>
public class MvtRecognition : MonoBehaviour
{
    Bvh bvh = null;
    NeuronActor actor = null;
    int nbFrame = 0;
    float timePassedBetweenFrame = 0;
    public int degreOfMargin = 0;
    private float totalTime = 0;        //Etant donné que cette valeur ne change pas, et puisqu'elle est utilisée régulièrement, on la garde de coté.

    [SerializeField]
    /// <value>The gameObject of the character controlled by the player</value>
    private GameObject player = null;

    [SerializeField]
    /// <value>The gameObject of the character controlled by the bvh file</value>
    private GameObject characterExemple = null;

    [SerializeField]
    /// <value>The gameObject of the hips of the </value>
    private GameObject uiHips = null;

    [SerializeField]
    /// <value>TODO</value>
    private Store store = null;

    //Values used to check if a mouvement is launched
    private int nbFirstMvtToCheck=0;      //The number of frame needed to check if the mouvement is launched
    private List<float> tabTimePassedBetweenFrame = null;
    bool mvtLaunched = false;

    bool mvtChoosen = false;

    private void Start()
    {
        initActor();
    }

    // Update is called once per frame
    void Update()
    {
        updateMvtRecognition();
    }

    /// <summary>
    /// Control motion recognition: if a movement is chosen, it will try to recognise the beginning of it. Then, if the movement is started, it will check if they are right.
    /// </summary>
    void updateMvtRecognition()
    {
        if (mvtChoosen)
        {
            float deltaTime = Time.deltaTime;

            if (mvtLaunched)
            {
                checkIfMvtIsRight(deltaTime);
            }
            else
            {
                checkBeginningMvt(deltaTime);
            }
            if (characterExemple != null) characterExemple.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame;     //If a character is set, then animate him.
        }
    }

    /// <summary>
    /// Initialize the NeuronAnimatorInstance actor.
    /// </summary>
    void initActor()
    {
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    /// <summary>
    /// Initialize all the values of the MvtRecognition class to work as intended.
    /// </summary>
    void initiateValuesBvh()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = store.Bvh;
        initActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

    /// <summary>
    /// Check if the mouvements of the user is the same as the one in the bvh file. It will also change the color on the character on the ui.
    /// </summary>
    /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
    bool checkIfMvtIsRight(float deltaTime)
    {
        timePassedBetweenFrame += deltaTime;
        timePassedBetweenFrame = timePassedBetweenFrame % totalTime;
        nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
        launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame, changeColorUICharacter);
        return launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame);
    }

    /// <summary>
    /// Tries to recognize the movement of the bvh in the user.
    /// </summary>
    /// <returns>Return true if a mouvement is detected.</returns>
    /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
    bool checkBeginningMvt(float deltaTime)
    {
        if (launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, 0))        //If the user have a position corresponding to the first frame of the mouvement
        {
            if (tabTimePassedBetweenFrame == null) tabTimePassedBetweenFrame = new List<float>();
            tabTimePassedBetweenFrame.Add(0f);          //It adds a new element to the tabTimePassedBetweenFrame list.
        }
        if (tabTimePassedBetweenFrame != null)      //If the list is not empty
        {
            for (int i = 0; i < tabTimePassedBetweenFrame.Count; i++)       //We go through it
            {
                tabTimePassedBetweenFrame[i] += deltaTime;      //Updating the time since the first frame was detected
                if (tabTimePassedBetweenFrame[i] >= (float)bvh.FrameTime.TotalSeconds * nbFirstMvtToCheck)      //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                {
                    //The first X frames have been detected, we start the mouvement recognition
                    mvtLaunched = true;     
                    timePassedBetweenFrame = tabTimePassedBetweenFrame[i];
                    tabTimePassedBetweenFrame = null;
                    return true;
                }
                nbFrame = (int)((tabTimePassedBetweenFrame[i] - tabTimePassedBetweenFrame[i] % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
                if (!launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame))         //If the position of the user does not correspond to that of the frame
                {
                    tabTimePassedBetweenFrame.RemoveAt(i);      //Remove this element of the tabTimePassedBetweenFrame list
                    i--;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Changes the color of the parts of the character on the user interface to red or green.
    /// </summary>
    /// <returns>The code error. For now it only return 0.</returns>
    /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
    int changeColorUICharacter(BvhNode node, bool color)     //If true: color green, else color red
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
    /// launchComparison(bvhHand, bvh, 40, new string[2]{ "Leg","Spine2" },23,changeColorUICharacter)
    /// </code>
    /// </example>
    /// <param name="Root">The <c>BvhNode</c> from which we want to compare the mouvement.</param>
    /// <param name="animationToCompare">The bvh which contain the mouvement to compare.</param>
    /// <param name="degreeOfMargin">The margin angle with which the user can make the movement.</param>
    /// <param name="bodyPartsToIgnore">An array of strings containing the names of the parts of the body that we do not want to compare in the movement. The following list show some valid exemples:
    /// <list type="bullet">
    /// <item><term></term>Hips</item>
    /// <item><term></term>Arm</item>
    /// <item><term></term>Hand</item>
    /// <item><term></term>LeftHandIndex</item>
    /// <item><term></term>...</item>
    /// </list>
    /// </param>
    /// <param name="frame">The index of the frame to look.</param>
    /// <param name="functionCalledAtEveryNode">A function taking as arguments a <c>BvhNode</c> and a <c>bool</c>, and which return an int.</param>
    public void launchComparison(BvhNode Root, Bvh animationToCompare, int degreeOfMargin, string[] bodyPartsToIgnore, int frame, Func<BvhNode,bool,int> functionCalledAtEveryNode)
    {
        foreach (var node in Root.Traverse())
        {
            bool checkValidity = true;
            if (bodyPartsToIgnore.Any(node.Name.Contains)) continue;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
            if (System.Math.Abs(actorRotation.x - animationToCompare.GetReceivedPosition(node.Name, frame, true).x) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.y - animationToCompare.GetReceivedPosition(node.Name, frame, true).y) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.z - animationToCompare.GetReceivedPosition(node.Name, frame, true).z) >= degreeOfMargin) checkValidity = false;
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
    /// if(launchComparison(bvhHand, bvh, 40, new string[2]{ "Leg","Spine2" },23))
    /// {
    ///     Debug.Log("The mouvements of the hand are corresponding.");
    /// }
    /// </code>
    /// </example>
    /// <param name="Root">The <c>BvhNode</c> from which we want to compare the mouvement.</param>
    /// <param name="animationToCompare">The bvh which contain the mouvement to compare.</param>
    /// <param name="degreeOfMargin">The margin angle with which the user can make the movement.</param>
    /// <param name="bodyPartsToIgnore">An array of strings containing the names of the parts of the body that we do not want to compare in the movement. The following list show some valid exemples:
    /// <list type="bullet">
    /// <item><term></term>Hips</item>
    /// <item><term></term>Arm</item>
    /// <item><term></term>Hand</item>
    /// <item><term></term>LeftHandIndex</item>
    /// <item><term></term>...</item>
    /// </list>
    /// </param>
    /// <param name="frame">The index of the frame to look.</param>
    public bool launchComparison(BvhNode Root, Bvh animationToCompare, int degreeOfMargin, string[] bodyPartsToIgnore, int frame)
    {
        foreach (var node in Root.Traverse())
        {
            bool checkValidity = true;
            if (bodyPartsToIgnore.Any(node.Name.Contains)) continue;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
            if (System.Math.Abs(actorRotation.x - animationToCompare.GetReceivedPosition(node.Name, frame, true).x) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.y - animationToCompare.GetReceivedPosition(node.Name, frame, true).y) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.z - animationToCompare.GetReceivedPosition(node.Name, frame, true).z) >= degreeOfMargin) checkValidity = false;
            if (!checkValidity) { return false; }
        }

        return true;
    }
}
