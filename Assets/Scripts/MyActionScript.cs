using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
public class MyActionScript : MonoBehaviour
{
    // a reference to the action
    public SteamVR_Action_Boolean setCameraOnHead;
    
    // a reference to the hand
    public SteamVR_Input_Sources handType;
    
    public GameObject headGameObject;
    public GameObject hipsGameObject;
    public GameObject CameraGameobject;

    private void Start()
    {
        setCameraOnHead.AddOnStateUpListener(setCameraOnHeadFunction, handType);
        
    }

    private void Update()
    {
    }

    public void setCameraOnHeadFunction(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("Head set to camera");
        hipsGameObject.transform.position = new Vector3(CameraGameobject.transform.position.x, CameraGameobject.transform.position.y - headGameObject.transform.position.y + hipsGameObject.transform.localPosition.y, CameraGameobject.transform.position.z);
        hipsGameObject.transform.rotation = Quaternion.Euler(hipsGameObject.transform.rotation.eulerAngles.x, CameraGameobject.transform.rotation.eulerAngles.y, hipsGameObject.transform.rotation.eulerAngles.z);
    }
}