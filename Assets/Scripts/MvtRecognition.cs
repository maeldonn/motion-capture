using Neuron;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;

public class MvtRecognition : MonoBehaviour
{
    Bvh bvh;
    NeuronActor actor;
    int nbFrame;
    float timePassedBetweenFrame;
    public int degreOfMargin;
    private float totalTime;        //Etant donné que cette valeur ne change pas, et puisqu'elle est utilisée régulièrement, on la garde de coté.

    [SerializeField]
    private GameObject player = null;

    [SerializeField]
    private GameObject characterExemple = null;

    [SerializeField]
    private GameObject uiHips = null;

    [SerializeField]
    private int nbFirstMvtToCheck;      //The number of frame needed to check if the mouvement is launched
    private List<float> tabTimePassedBetweenFrame = null;    //Corresponding
    bool mvtLaunched = false;


    // Start is called before the first frame update
    void Start()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = GetComponent<BvhImporter>().GetBvh();
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

    // Update is called once per frame
    void Update()
    {
        //Les 7 lignes suivantes servent à calculer la frame de l'animation suivant le temps passé.
        timePassedBetweenFrame += Time.deltaTime;

        if (mvtLaunched)
        {
            timePassedBetweenFrame = timePassedBetweenFrame % totalTime;
            nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
            Debug.Log(bvh.FrameTime.TotalSeconds);
            //compareMvt();
            launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" },nbFrame, changeColorUICharacter);
        }
        else
        {
            if(launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, 0))
            {
                if (tabTimePassedBetweenFrame == null) tabTimePassedBetweenFrame = new List<float>();
                tabTimePassedBetweenFrame.Add(0f);
            }
            if (tabTimePassedBetweenFrame != null)
            {
                for (int i = 0; i < tabTimePassedBetweenFrame.Count; i++)
                {
                    tabTimePassedBetweenFrame[i] += Time.deltaTime;
                    if (tabTimePassedBetweenFrame[i] >= (float)bvh.FrameTime.TotalSeconds * nbFirstMvtToCheck)
                    {
                        mvtLaunched = true;
                        timePassedBetweenFrame = tabTimePassedBetweenFrame[i];
                        tabTimePassedBetweenFrame = null;
                        break;
                    }
                    nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
                    if (!launchComparison(bvh.Root, bvh, degreOfMargin, new string[1] { "Hips" }, nbFrame))
                    {
                        tabTimePassedBetweenFrame.Remove(i);
                        i--;
                    }
                }
            }
        }
        if (characterExemple != null) characterExemple.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame;
    }

    /*void compareMvt()             //Remplacée par launchComparison. Je la laisse là au cas ou, mais normalement on en a plus besoin
    {
        foreach (var node in bvh.Root.Traverse())
        {
            if(node.Name == "Hips") { continue; }      //La rotation générale de la personne n'est pas pertinante quand il s'agit de comparer des mouvements
            bool checkValidity = true;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
            if (System.Math.Abs(actorRotation.x - bvh.GetReceivedPosition(node.Name, nbFrame, true).x) >= degreOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.y - bvh.GetReceivedPosition(node.Name, nbFrame, true).y) >= degreOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.z - bvh.GetReceivedPosition(node.Name, nbFrame, true).z) >= degreOfMargin) checkValidity = false;
            if (!checkValidity)
            {
                //Debug.Log(node.Name+"not corresponding");
                foreach (var c in uiHips.transform.GetComponentsInChildren<Transform>())
                {
                    if (node.Name == c.name)
                    {
                        c.GetComponent<RawImage>().color = Color.red;
                        break;
                    }
                }
            }
            else
            {
                //Debug.Log(node.Name+" corresponding");
                foreach (var c in uiHips.transform.GetComponentsInChildren<Transform>())
                {
                    if (node.Name == c.name)
                    {
                        c.GetComponent<RawImage>().color = Color.green;
                        break;
                    }
                }
            }
        }
    }*/

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
