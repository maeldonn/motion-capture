using UnityEngine;
using System.Collections;

public class CameraResetToNeuron : MonoBehaviour {

    public Transform neuronEyePosition;
    Camera cam;

	void Start ()
    {
        cam = GetComponentInChildren<Camera>();
        Invoke("DoReset", 1f);
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.R))
            DoReset();
    }

    void DoReset()
    {
        Quaternion camQ = cam.transform.localRotation;
        Quaternion neuronQ = neuronEyePosition.rotation;

        camQ = Quaternion.Euler(0f, camQ.eulerAngles.y, 0f);
        neuronQ = Quaternion.Euler(0f, neuronQ.eulerAngles.y, 0f);

        Quaternion deltQ = neuronQ * Quaternion.Inverse(camQ);
        transform.localRotation = deltQ;

        Vector3 deltP = neuronEyePosition.position - transform.localRotation * cam.transform.localPosition;
        transform.localPosition = deltP;
    }
}
