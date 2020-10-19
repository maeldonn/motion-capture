using CERV.MouvementRecognition.Main;
using UnityEngine;
using UnityEngine.EventSystems;

//Used the following video to make this class: https://www.youtube.com/watch?v=h_BMXDWv10I

namespace CERV.MouvementRecognition.Interactions
{
    public class mocapInputModule : BaseInputModule
    {
        public GameManager Gm;

        private Camera m_camera; //used for the GraphicRaycast

        private GameObject m_CurrentObject = null;
        private PointerEventData m_Data = null;


        protected override void Awake()
        {
            base.Awake();
            m_Data = new PointerEventData(eventSystem);
            m_camera = Gm.GOcam.transform.GetChild(0).gameObject.GetComponent<Camera>();
        }

        public override void Process()
        {
            //Reset data
            m_Data.Reset();
            m_Data.position = new Vector2(m_camera.pixelWidth / 2f, m_camera.pixelHeight / 2f);

            //Raycast
            eventSystem.RaycastAll(m_Data, m_RaycastResultCache);
            m_Data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_CurrentObject = m_Data.pointerCurrentRaycast.gameObject;

            //Clear
            m_RaycastResultCache.Clear();

            //Hover
            HandlePointerExitAndEnter(m_Data, m_CurrentObject);

            if (Gm.pointingHandler == null) return;
            //Press
            if (Gm.pointingHandler.GetConfirmState().ToString() == "Action") ProcessPress(m_Data);

            //Release
            else if (Gm.pointingHandler.GetConfirmState().ToString() == "Idle") ProcessRelease(m_Data);
        }



        public PointerEventData GetData()
        {
            return m_Data;
        }

        private void ProcessPress(PointerEventData data)
        {
            //Set raycast
            data.pointerPressRaycast = data.pointerCurrentRaycast;

            //Check if object hit, get the down handler, call
            GameObject newPointerPress =
                ExecuteEvents.ExecuteHierarchy(m_CurrentObject, data, ExecuteEvents.pointerDownHandler);

            //If no down handler, try and get click handler
            if (newPointerPress == null)
                newPointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_CurrentObject);

            //Set data
            data.pressPosition = data.position;
            data.pointerPress = newPointerPress;
            data.rawPointerPress = m_CurrentObject;
        }

        private void ProcessRelease(PointerEventData data)
        {
            //Execute pointer up
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

            //Check for click handler
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_CurrentObject);

            //Check if actual
            if (data.pointerPress == pointerUpHandler)
            {
                ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
            }

            //Clear selected gameObject
            eventSystem.SetSelectedGameObject(null);

            //Reset data
            data.pressPosition = Vector2.zero;
            data.pointerPress = null;
            data.rawPointerPress = null;
        }
    }
}