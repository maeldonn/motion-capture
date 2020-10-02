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

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private GameObject uiHips;

    // Start is called before the first frame update
    void Start()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = player.GetComponent<BvhImporter>().GetBvh();
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    // Update is called once per frame
    void Update()
    {
        timePassedBetweenFrame += Time.deltaTime;
        if (bvh.FrameTime.TotalSeconds <= timePassedBetweenFrame)
        {
            timePassedBetweenFrame = 0;
            if (nbFrame < bvh.FrameCount - 1) nbFrame++; else nbFrame = 0;
        }
        compareMvt();
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
}
