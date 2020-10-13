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
    /// <value>The gameObject of the character controlled by the player</value>
    private GameObject characterExemple = null;

    [SerializeField]
    private GameObject uiHips = null;

    [SerializeField]
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

    void initActor()
    {
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    void initiateValuesBvh()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = store.Bvh;
        initActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

    bool checkIfMvtIsRight(float deltaTime)
    {
        timePassedBetweenFrame += deltaTime;
        timePassedBetweenFrame = timePassedBetweenFrame % totalTime;
        nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
        launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame, changeColorUICharacter);
        return launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame);
    }


    bool checkBeginningMvt(float deltaTime)
    {
        if (launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, 0))
        {
            if (tabTimePassedBetweenFrame == null) tabTimePassedBetweenFrame = new List<float>();
            tabTimePassedBetweenFrame.Add(0f);
        }
        if (tabTimePassedBetweenFrame != null)
        {
            for (int i = 0; i < tabTimePassedBetweenFrame.Count; i++)
            {
                tabTimePassedBetweenFrame[i] += deltaTime;
                if (tabTimePassedBetweenFrame[i] >= (float)bvh.FrameTime.TotalSeconds * nbFirstMvtToCheck)
                {
                    mvtLaunched = true;
                    timePassedBetweenFrame = tabTimePassedBetweenFrame[i];
                    tabTimePassedBetweenFrame = null;
                    return true;
                }
                nbFrame = (int)((tabTimePassedBetweenFrame[i] - tabTimePassedBetweenFrame[i] % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
                if (!launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame))
                {
                    tabTimePassedBetweenFrame.RemoveAt(i);
                    i--;
                }
            }
        }
        return false;
    }

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
