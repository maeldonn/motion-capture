using Neuron;
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
    Bvh idlePointing = null;
    Bvh BVHactivating = null;
    NeuronActor actor = null;

    [SerializeField]
    private GameObject player = null;

    [SerializeField]
    int degreeOfMarginPointing = 0;

    [SerializeField]
    int degreeOfMarginValidating = 0;

    [SerializeField]
    LineRenderer lineMenu = null;

    [SerializeField]
    GameObject leftHand = null;

    confirmState stateConfirm;
    pointingState statePointing;

    [SerializeField]
    public AudioClip clipConfirm = null;

    [SerializeField]
    public AudioClip clipPointing = null;

    [SerializeField]
    MvtRecognition mvtRecognition = null;

    // Start is called before the first frame update
    void Start()
    {
        idlePointing = new Bvh().GetBvhFromPath("Assets/BVH/Pointer/pointeur_3.bvh");
        BVHactivating = new Bvh().GetBvhFromPath("Assets/BVH/Pointer/pointeur_2.bvh");
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
                    if(mvtRecognition.LaunchComparison(BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0].Children[0], BVHactivating, degreeOfMarginValidating, new string[0],1))
                    {
                        SoundManager.PlaySound(clipConfirm, leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position);
                        stateConfirm = confirmState.click;
                        lineMenu.endColor = Color.green;
                    }
                    /*bool checkValidity = true;            //A ENLEVER
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
                    }*/
                    break;

                case confirmState.click:
                    stateConfirm = confirmState.releaseClick;
                    break;

                case confirmState.releaseClick:
                    if (mvtRecognition.LaunchComparison(BVHactivating.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0].Children[0], BVHactivating, degreeOfMarginValidating, new string[0],0))
                    {
                        stateConfirm = confirmState.idle;
                        lineMenu.endColor = Color.white;
                    }
                    /*checkValidity = true;         //A ENLEVER
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
                    }*/
                    break;
            }
        }

        if(statePointing== pointingState.pointing)
        { 
            SoundManager.PlaySound(clipPointing, leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position);
            statePointing = pointingState.idlePointing;
        }
        //if (compareHandPosition())
        if(mvtRecognition.LaunchComparison(idlePointing.Root.Children[2].Children[0].Children[0].Children[0].Children[2].Children[0].Children[0].Children[0], idlePointing, degreeOfMarginPointing,new string[1] { "Thumb" },0))
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
                tmpPos = leftHand.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).position+unitVector * 1000;
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

    /*public bool compareHandPosition()  //position de la main == idlePointing.frame[0]         //A ENLEVER
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
    }*/
}
