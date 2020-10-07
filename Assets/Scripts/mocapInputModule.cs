using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


//Used the following video to make this class: https://www.youtube.com/watch?v=h_BMXDWv10I
public class mocapInputModule : BaseInputModule
{

    public Camera m_camera;     //used for the GraphicRaycast

    private GameObject m_CurrentObject= null;
    private PointerEventData m_Data = null;
    [SerializeField]
    private pointingHandler pointhandler;

    protected override void Awake()
    {
        base.Awake();
        m_Data = new PointerEventData(eventSystem);
    }

    public override void Process()
    {
        //Reset data
        m_Data.Reset();
        m_Data.position = new Vector2(m_camera.pixelWidth / 2, m_camera.pixelHeight / 2);

        //Raycast
        eventSystem.RaycastAll(m_Data,m_RaycastResultCache);
        m_Data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_CurrentObject = m_Data.pointerCurrentRaycast.gameObject;

        //Clear
        m_RaycastResultCache.Clear();

        //Hover
        HandlePointerExitAndEnter(m_Data,m_CurrentObject);

        //Press
        if (pointhandler.GetState().ToString() == "click") ProcessPress(m_Data);

        //Release
        else ProcessRelease(m_Data);
    }

    public PointerEventData GetData()
    {
        return m_Data;
    }

    private void ProcessPress(PointerEventData data)
    {

    }

    private void ProcessRelease(PointerEventData data)
    {

    }
}
