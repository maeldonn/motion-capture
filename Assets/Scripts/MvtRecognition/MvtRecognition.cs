using Neuron;
using System;
using System.Collections.Generic;
using System.Linq;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The <c>MvtRecognition</c> class.
/// Contains all methods to detect and analyze movements.
/// <list type="bullet">
/// <item>
/// <term>initiateValues</term>
/// <description>Initiate the different values of the class.</description>
/// </item>
/// <item>
/// <term>checkIfMvtIsRight</term>
/// <description>Compare the movements made by the user with the one he should reproduce.</description>
/// </item>
/// <item>
/// <term>checkBeginningMvt</term>
/// <description>Check if the user is doing the beginning of a movement.</description>
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
    private Bvh bvh = null;
    private NeuronActor actor = null;
    private int nbFrame = 0;
    private float timePassedBetweenFrame = 0;
    public int degreeOfMargin = 0;
    private float totalTime = 0;        //Since this value does not change, and since it is used regularly, it is kept aside.

    [SerializeField]
    // The gameObject of the character controlled by the player
    private GameObject player = null;

    [SerializeField]
    //The gameObject of the character controlled by the bvh file
    private GameObject characterExample = null;

    [SerializeField]
    //The gameObject of the hips of the
    private GameObject uiHips = null;

    [SerializeField]
    //TODO
    private Store store = null;

    //Values used to check if a movement is launched
    [SerializeField]
    //TODO
    private int nbFirstMvtToCheck=0;      //The number of frame needed to check if the movement is launched
    private List<float> tabTimePassedBetweenFrame;
    private bool mvtLaunched;

    private bool mvtChoosen = false;

    /*public MvtRecognition(GameObject _player, GameObject _characterExample, GameObject _uiHips, Store _store, int _nbFirstMvtToCheck)
    {
        player = _player;
        characterExample = _characterExample;
        uiHips = _uiHips;
        store = _store;
        nbFirstMvtToCheck = _nbFirstMvtToCheck;
    }*/

    private void Start()
    {
        InitActor();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateMvtRecognition();
    }

    /// <summary>
    /// Control motion recognition: if a movement is chosen, it will try to recognize the beginning of it. Then, if the movement is started, it will check if they are right.
    /// </summary>
    private void UpdateMvtRecognition()
    {
        if (mvtChoosen)
        {
            var deltaTime = Time.deltaTime;

            if (mvtLaunched)
            {
                CheckIfMvtIsRight(deltaTime);
            }
            else
            {
                CheckBeginningMvt(deltaTime);
            }
            if (characterExample != null) characterExample.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame;     //If a character is set, then animate him.
        }
    }

    /// <summary>
    /// Initialize the NeuronAnimatorInstance actor.
    /// </summary>
    private void InitActor()
    {
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    /// <summary>
    /// Initialize all the values of the MvtRecognition class to work as intended.
    /// </summary>
    private void InitiateValuesBvh()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = store.Bvh;
        InitActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

    /// <summary>
    /// Check if the movements of the user is the same as the one in the bvh file. It will also change the color on the character on the ui.
    /// </summary>
    /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
    private bool CheckIfMvtIsRight(float deltaTime)
    {
        timePassedBetweenFrame += deltaTime;
        timePassedBetweenFrame %= totalTime;
        nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
        LaunchComparison(bvh.Root, bvh, degreeOfMargin, new [] { "Hips" }, nbFrame, ChangeColorUiCharacter);
        return LaunchComparison(bvh.Root, bvh, degreeOfMargin, new[] { "Hips" }, nbFrame);
    }

    /// <summary>
    /// Tries to recognize the movement of the bvh in the user.
    /// </summary>
    /// <returns>Return true if a movement is detected.</returns>
    /// <param name="deltaTime">A float value representing the time that has passed since the last frame.</param>
    private void CheckBeginningMvt(float deltaTime)
    {
        if (LaunchComparison(bvh.Root, bvh, degreeOfMargin, new[] { "Hips" }, 0))        //If the user have a position corresponding to the first frame of the movement
        {
            if (tabTimePassedBetweenFrame == null) tabTimePassedBetweenFrame = new List<float>();
            tabTimePassedBetweenFrame.Add(0f);          //It adds a new element to the tabTimePassedBetweenFrame list.
        }
        if (tabTimePassedBetweenFrame != null)      //If the list is not empty
        {
            for (var i = 0; i < tabTimePassedBetweenFrame.Count; i++)       //We go through it
            {
                tabTimePassedBetweenFrame[i] += deltaTime;      //Updating the time since the first frame was detected
                if (tabTimePassedBetweenFrame[i] >= (float)bvh.FrameTime.TotalSeconds * nbFirstMvtToCheck)      //If the time passed since the first frame was detected is superior or equal to the time of the X first frame we wanted to test
                {
                    //The first X frames have been detected, we start the movement recognition
                    mvtLaunched = true;     
                    timePassedBetweenFrame = tabTimePassedBetweenFrame[i];
                    tabTimePassedBetweenFrame = null;
                    return;
                }
                nbFrame = (int)((tabTimePassedBetweenFrame[i] - tabTimePassedBetweenFrame[i] % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
                if (!LaunchComparison(bvh.Root, bvh, degreeOfMargin, new[] { "Hips" }, nbFrame))         //If the position of the user does not correspond to that of the frame
                {
                    tabTimePassedBetweenFrame.RemoveAt(i);      //Remove this element of the tabTimePassedBetweenFrame list
                    i--;
                }
            }
        }
    }

    /// <summary>
    /// Changes the color of the parts of the character on the user interface to red or green.
    /// </summary>
    /// <returns>The code error. For now it only return 0.</returns>
    /// <param name="node">The node whose color you want to change</param>
    /// <param name="color">The color you want to change: true equal green, false equal red</param>
    private int ChangeColorUiCharacter(BvhNode node, bool color)     //If true: color green, else color red
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
    /// launchComparison(bvhHand, bvh, 40, new []{ "Leg","Spine2" },23,changeColorUICharacter)
    /// </code>
    /// </example>
    /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
    /// <param name="animationToCompare">The bvh which contain the movement to compare.</param>
    /// <param name="degOfMargin">The margin angle with which the user can make the movement.</param>
    /// <param name="bodyPartsToIgnore">An array of strings containing the names of the parts of the body that we do not want to compare in the movement. The following list show some valid examples:
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
    public void LaunchComparison(BvhNode root, Bvh animationToCompare, int degOfMargin, string[] bodyPartsToIgnore, int frame, Func<BvhNode,bool,int> functionCalledAtEveryNode)
    {
        if (degOfMargin <= 0) throw new ArgumentOutOfRangeException(nameof(degOfMargin));
        foreach (var node in root.Traverse())
        {
            var checkValidity = true;
            if (bodyPartsToIgnore.Any(node.Name.Contains)) continue;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)Enum.Parse(typeof(NeuronBones), node.Name));
            if (Math.Abs(actorRotation.x - animationToCompare.GetReceivedPosition(node.Name, frame, true).x) >= degOfMargin) checkValidity = false;
            else if (Math.Abs(actorRotation.y - animationToCompare.GetReceivedPosition(node.Name, frame, true).y) >= degOfMargin) checkValidity = false;
            else if (Math.Abs(actorRotation.z - animationToCompare.GetReceivedPosition(node.Name, frame, true).z) >= degOfMargin) checkValidity = false;
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
    /// if(launchComparison(bvhHand, bvh, 40, new []{ "Leg","Spine2" },23))
    /// {
    ///     Debug.Log("The movements of the hand are corresponding.");
    /// }
    /// </code>
    /// </example>
    /// <param name="root">The <c>BvhNode</c> from which we want to compare the movement.</param>
    /// <param name="animationToCompare">The bvh which contain the movement to compare.</param>
    /// <param name="degOfMargin">The margin angle with which the user can make the movement.</param>
    /// <param name="bodyPartsToIgnore">An array of strings containing the names of the parts of the body that we do not want to compare in the movement. The following list show some valid examples:
    /// <list type="bullet">
    /// <item><term></term>Hips</item>
    /// <item><term></term>Arm</item>
    /// <item><term></term>Hand</item>
    /// <item><term></term>LeftHandIndex</item>
    /// <item><term></term>...</item>
    /// </list>
    /// </param>
    /// <param name="frame">The index of the frame to look.</param>
    public bool LaunchComparison(BvhNode root, Bvh animationToCompare, int degOfMargin, string[] bodyPartsToIgnore, int frame)
    {
        if (degOfMargin <= 0) throw new ArgumentOutOfRangeException(nameof(degOfMargin));
        foreach (var node in root.Traverse())
        {
            var checkValidity = true;
            if (bodyPartsToIgnore.Any(node.Name.Contains)) continue;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)Enum.Parse(typeof(NeuronBones), node.Name));
            if (Math.Abs(actorRotation.x - animationToCompare.GetReceivedPosition(node.Name, frame, true).x) >= degOfMargin) checkValidity = false;
            else if (Math.Abs(actorRotation.y - animationToCompare.GetReceivedPosition(node.Name, frame, true).y) >= degOfMargin) checkValidity = false;
            else if (Math.Abs(actorRotation.z - animationToCompare.GetReceivedPosition(node.Name, frame, true).z) >= degOfMargin) checkValidity = false;
            if (!checkValidity) { return false; }
        }

        return true;
    }
}
