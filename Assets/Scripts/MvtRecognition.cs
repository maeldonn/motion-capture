using Neuron;
using System.Collections;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;

public class MvtRecognition : MonoBehaviour
{
    Bvh bvh;
    NeuronActor actor;
    int nbFrame;
    float timePassedBetweenFrame;
    public float degreOfMargin;
    private float totalTime;        //Etant donné que cette valeur ne change pas, et puisqu'elle est utilisée régulièrement, on la garde de coté.

    [SerializeField]
    private GameObject player = null;

    [SerializeField]
    private GameObject characterExemple = null;

    [SerializeField]
    private GameObject uiHips = null;

    [SerializeField]
    private int nbFirstMvtToCheck;      //The number of frame needed to check if the mouvement is launched
    private List<List<Vector3>> firstMvt = null;            //The list containing all the frame to check to see if the user is doing the mouvement
    //private List<List<Vector3>> currentfirstMvt = null;         //The list containing the last mouvement made by the user; will be compared with firstMvt
    private List<bool> checkedValues = null;    //Corresponding
    bool mvtLaunched = false;


    // Start is called before the first frame update
    void Start()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = GetComponent<BvhImporter>().GetBvh();
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
        firstMvt = bvh.GetListFrame(0,nbFirstMvtToCheck);
        checkedValues = new List<bool>();
        resetNbFirstMvtToCheck();
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
            compareMvt();
        }
        else
        {
            timePassedBetweenFrame = timePassedBetweenFrame % (float)bvh.FrameTime.TotalSeconds * 50;
            nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
            //checkedValues
        }
        if (characterExemple != null) characterExemple.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame;
    }

    void resetNbFirstMvtToCheck()
    {
        for (int i = 0; i < nbFirstMvtToCheck; i++) checkedValues.Add(false);
    }

    void compareMvt()
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
                        c.GetComponent<RawImage>().color= new Color(1f,0f,0f);
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
                        c.GetComponent<RawImage>().color = new Color(0f, 1f, 0f);
                        break;
                    }
                }
            }
        }
    }

    void launchComparison()
    {

    }
}
