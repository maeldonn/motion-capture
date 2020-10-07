using Neuron;
using System.Collections;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum confirmState
{
    idle,
    click,
    releaseClick
}

public enum pointingState
{
    idle,
    pointing,
    idlePointing
}

public class pointingHandler : MonoBehaviour
{
    Bvh idlePointing;
    Bvh BVHactivating;
    NeuronActor actor;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    int degreeOfMarginPointing;
    [SerializeField]
    int degreeOfMarginValidating;

    [SerializeField]
    LineRenderer lineMenu;

    private BvhNode BVHleftHand;
    [SerializeField]
    GameObject leftHand;

    confirmState stateConfirm;
    pointingState statePointing;

    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    [SerializeField]
    public AudioClip clipConfirm;
    [SerializeField]
    public AudioClip clipPointing;

    // Start is called before the first frame update
    void Start()
    {
        BvhImporter tmp = new BvhImporter("pointeur_3.bvh");
        tmp.Parse();
        idlePointing = tmp.GetBvh();
        tmp.BvhPath = "pointeur_2.bvh";
        tmp.Parse();
        BVHactivating = tmp.GetBvh();
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
    }

    // Update is called once per frame
    void Update()
    {
        if (statePointing != pointingState.idle)
        {
            switch (stateConfirm)
            {
                case confirmState.idle:
                    //TODO check if the intermediate position is used

                    bool checkValidity = true;
                    foreach (var node in BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0].Children[0].Traverse())     //Traverse on left thumb
                    {
                        var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
                        if (System.Math.Abs(actorRotation.x - BVHactivating.GetReceivedPosition(node.Name, 1, true).x) >= degreeOfMarginValidating) { checkValidity = false; break; }
                        else if (System.Math.Abs(actorRotation.y - BVHactivating.GetReceivedPosition(node.Name, 1, true).y) >= degreeOfMarginValidating) { checkValidity = false; break; }
                        else if (System.Math.Abs(actorRotation.z - BVHactivating.GetReceivedPosition(node.Name, 1, true).z) >= degreeOfMarginValidating) { checkValidity = false; break; }
                    }

                    if (checkValidity)
                    {
                        SoundManager.PlaySound(clipConfirm, leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position);
                        stateConfirm = confirmState.click;
                        lineMenu.endColor = Color.green;
                    }
                    break;
                case confirmState.click:
                    //TODO interact with environment and change state

                    stateConfirm = confirmState.releaseClick;
                    break;
                case confirmState.releaseClick:
                    //TODO check if the initial position is used
                    checkValidity = true;
                    foreach (var node in BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0].Children[0].Traverse())     //Traverse on left thumb
                    {
                        var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
                        if (System.Math.Abs(actorRotation.x - BVHactivating.GetReceivedPosition(node.Name, 0, true).x) >= degreeOfMarginValidating) { checkValidity = false; break; }
                        else if (System.Math.Abs(actorRotation.y - BVHactivating.GetReceivedPosition(node.Name, 0, true).y) >= degreeOfMarginValidating) { checkValidity = false; break; }
                        else if (System.Math.Abs(actorRotation.z - BVHactivating.GetReceivedPosition(node.Name, 0, true).z) >= degreeOfMarginValidating) { checkValidity = false; break; }
                    }

                    if (checkValidity)
                    {
                        stateConfirm = confirmState.idle;
                        lineMenu.endColor = Color.white;
                    }
                    break;
            }
        }

        if(statePointing== pointingState.pointing)
        { 
            SoundManager.PlaySound(clipPointing, leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position);
            statePointing = pointingState.idlePointing;
        }
        if (compareHandPosition())
        {
            if (statePointing == pointingState.idle)
            {
                statePointing = pointingState.pointing;
            }else
            {
                statePointing = pointingState.idlePointing;
            }
            //Debug.Log("Hand pointing");
            lineMenu.gameObject.SetActive(true);
            Vector3 unitVector = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position - leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position;

            Vector3 tmpPos;
            RaycastHit hitPoint;
            Ray ray = new Ray(leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position, unitVector);
            if (Physics.Raycast(ray, out hitPoint, Mathf.Infinity))
            {
                //Debug.Log("Hit Something");
                tmpPos = hitPoint.point;
            }
            else
            {
                //Debug.Log("No collider hit");
                tmpPos = unitVector*1000;
            }

            lineMenu.SetPositions(new [] { leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position, tmpPos });
            lineMenu.transform.position = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position;
            lineMenu.transform.LookAt(tmpPos);
        }
        else
        {
            //Debug.Log("Hand not pointing");
            statePointing = pointingState.idle;
            lineMenu.gameObject.SetActive(false);
        }
    }

    public confirmState GetState()
    {
        return stateConfirm;
    }

    public bool compareHandPosition()  //position de la main == idlePointing.frame[0]
    {
        bool checkValidity = true;
        foreach (var node in idlePointing.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0].Traverse())  //Traverse on left hand
        {
            if (node.Name.Contains("Thumb")) continue;
            var actorRotation = actor.GetReceivedRotation((NeuronBones)System.Enum.Parse(typeof(NeuronBones), node.Name));
            if (System.Math.Abs(actorRotation.x - idlePointing.GetReceivedPosition(node.Name, 0, true).x) >= degreeOfMarginPointing) checkValidity = false;
            else if (System.Math.Abs(actorRotation.y - idlePointing.GetReceivedPosition(node.Name, 0, true).y) >= degreeOfMarginPointing) checkValidity = false;
            else if (System.Math.Abs(actorRotation.z - idlePointing.GetReceivedPosition(node.Name, 0, true).z) >= degreeOfMarginPointing) checkValidity = false;
            if (!checkValidity) return false;
        }

        return true;
    }
}
