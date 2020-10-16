using CERV.MouvementRecognition.Interactions;
using CERV.MouvementRecognition.Recognition;
using UnityEngine;

namespace CERV.MouvementRecognition.Main
{
    public class GameManager : MonoBehaviour
    {
        public GameObject Player = null;
        public Store Store = null;
        [Space(2)]
        [Header("Movement Manager:")]
        public GameObject CharacterExample = null;
        public GameObject UiHips = null;
        public int NbFirstMvtToCheck = 0;
        public int DegreeOfMargin = 0;
        [Space(2)]
        [Header("Interactions:")]
        public int DegreeOfMarginPointing = 0;
        public int DegreeOfMarginValidating = 0;
        public LineRenderer LineMenu = null;
        public GameObject LeftHand = null;
        public AudioClip ClipConfirm = null;
        public AudioClip ClipPointing = null;
        public Transform NeuronEyePosition = null;
        public GameObject GOcam = null;

        public MvtRecognition mvtRecognition
        {
            get;
            private set;
        }
        public PointingHandler pointingHandler
        {
            get;
            private set;
        }

        // Start is called before the first frame update
        void Start()
        {
            mvtRecognition = new MvtRecognition(Player, CharacterExample, UiHips, Store, NbFirstMvtToCheck, DegreeOfMargin);
            mvtRecognition.InitActor();
            pointingHandler = new PointingHandler(Player, DegreeOfMarginPointing, DegreeOfMarginValidating, LineMenu, LeftHand, ClipConfirm, ClipPointing, mvtRecognition);
            pointingHandler.InitPointingHandler();
        }

        // Update is called once per frame
        void Update()
        {
            pointingHandler.UpdateUserInputs();
            mvtRecognition.UpdateMvtRecognition();
        }
    }
}