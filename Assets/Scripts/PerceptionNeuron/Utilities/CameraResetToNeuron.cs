﻿using UnityEngine;

public class CameraResetToNeuron : MonoBehaviour
{
    public Transform neuronEyePosition;
    public GameObject GOcam;
    private Camera cam;

    private void Start()
    {
        cam = GOcam.GetComponentInChildren<Camera>();
        Invoke("DoReset", 1f);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            DoReset();
    }

    private void DoReset()
    {
        Quaternion camQ = cam.transform.localRotation;
        Quaternion neuronQ = neuronEyePosition.rotation;

        camQ = Quaternion.Euler(0f, camQ.eulerAngles.y, 0f);
        neuronQ = Quaternion.Euler(0f, neuronQ.eulerAngles.y, 0f);

        Quaternion deltQ = neuronQ * Quaternion.Inverse(camQ);
        GOcam.transform.localRotation = deltQ;

        Vector3 deltP = neuronEyePosition.position - GOcam.transform.localRotation * cam.transform.localPosition;
        GOcam.transform.localPosition = deltP;
    }
}