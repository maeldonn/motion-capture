using Neuron;
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
    private Store store = null;

    // Start is called before the first frame update
    void Start()
    {
        nbFrame = 0;
        timePassedBetweenFrame = 0;
        bvh = store.Bvh;
        actor = player.GetComponent<NeuronAnimatorInstance>().GetActor();
        totalTime = (float)bvh.FrameTime.TotalSeconds * bvh.FrameCount;
    }

    // Update is called once per frame
    void Update()
    {
        //Les 7 lignes suivantes servent à calculer la frame de l'animation suivant le temps passé.
        timePassedBetweenFrame += Time.deltaTime;
        timePassedBetweenFrame = timePassedBetweenFrame % totalTime;
        nbFrame = (int)((timePassedBetweenFrame - timePassedBetweenFrame % bvh.FrameTime.TotalSeconds) / bvh.FrameTime.TotalSeconds);
        characterExemple.GetComponent<NeuronAnimatorInstanceBVH>().NbFrame = nbFrame;
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
