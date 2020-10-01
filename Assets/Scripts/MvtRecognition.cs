using Neuron;
using System.Collections;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;

public class MvtRecognition : MonoBehaviour
{

    Bvh bvh;
    NeuronActor actor;
    int nbFrame;
    float timePassedBetweenFrame;
    public float degreOfMargin;

    // Start is called before the first frame update
    void Start()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = gameObject.GetComponent<BvhImporter>().GetBvh();
        actor = gameObject.GetComponent<NeuronAnimatorInstance>().GetActor();
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
            bool checkValidity = true;
            var neuronBone = (NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name);
            if (System.Math.Abs(actor.GetReceivedRotation(neuronBone).x - bvh.GetReceivedPosition(node.Name, nbFrame, true).x) >= degreOfMargin) checkValidity = false;
            else if (System.Math.Abs(actor.GetReceivedRotation(neuronBone).y - bvh.GetReceivedPosition(node.Name, nbFrame, true).y) >= degreOfMargin) checkValidity = false;
            else if (System.Math.Abs(actor.GetReceivedRotation(neuronBone).z - bvh.GetReceivedPosition(node.Name, nbFrame, true).z) >= degreOfMargin) checkValidity = false;
            if (!checkValidity)
            {
                //Debug.Log(node.Name+"not corresponding");
            }
        }
    }
}
