using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    SettingsConfig settingsConfig;
    Vector3 currentAngle  = Vector3.zero;
    void Start()
    {
        cam.transform.rotation = transform.rotation;
        Cursor.lockState = CursorLockMode.Locked;
        
    }

    // Update is called once per frame
    void Update()
    {
        float horiz  = settingsConfig.horiz_sens * Input.GetAxis("Mouse X");
        float vert = -settingsConfig.vert_sens * Input.GetAxis("Mouse Y");
        
        currentAngle += new Vector3(vert,horiz,0);
        cam.transform.rotation = Quaternion.Euler(currentAngle);
        transform.rotation = Quaternion.Euler(0,currentAngle.y,0);
    }
}

