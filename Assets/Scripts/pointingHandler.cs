using Neuron;
using System.Collections;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;

public class pointingHandler : MonoBehaviour
{
    Bvh idlePointing;
    NeuronActor actor;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    int degreeOfMargin;

    [SerializeField]
    LineRenderer lineMenu;

    private BvhNode BVHleftHand;
    [SerializeField]
    GameObject leftHand;

    // Start is called before the first frame update
    void Start()
    {
        BvhImporter tmp = new BvhImporter("pointeur_3.bvh");
        tmp.Parse();
        idlePointing = tmp.GetBvh();
        BVHleftHand = idlePointing.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0];
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    // Update is called once per frame
    void Update()
    {
        if (compareHandPosition())
        {
            Debug.Log("Hand pointing");
            lineMenu.gameObject.SetActive(true);
            Vector3 unitVector = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position- leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position;
            lineMenu.SetPositions(new [] { leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position, leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position+unitVector * 100 });
        }
        else
        {
            Debug.Log("Hand not pointing");
            lineMenu.gameObject.SetActive(false);
        }
    }

    bool compareHandPosition()  //position de la main == idlePointing.frame[0]
    {
        bool checkValidity = true;
        foreach (var node in BVHleftHand.Traverse())
        {
            
            var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
            if (System.Math.Abs(actorRotation.x - idlePointing.GetReceivedPosition(node.Name, 0, true).x) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.y - idlePointing.GetReceivedPosition(node.Name, 0, true).y) >= degreeOfMargin) checkValidity = false;
            else if (System.Math.Abs(actorRotation.z - idlePointing.GetReceivedPosition(node.Name, 0, true).z) >= degreeOfMargin) checkValidity = false;
            if (!checkValidity) return false;
        }

        return true;
    }
}
