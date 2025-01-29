using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraControlNetwork : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField]Camera m_camera;
    Vector3 currentAngle = Vector3.zero;
    void onNetworkSpawn(){
        if(!IsOwner){
            m_camera.enabled = false;
        }
    }
    void Update(){
        if(IsOwner){
            float horiz  = Input.GetAxisRaw("Mouse X");
            float vert = -Input.GetAxisRaw("Mouse Y");
            currentAngle += new Vector3(vert,horiz,0);
            m_camera.transform.rotation = Quaternion.Euler(currentAngle);
            transform.rotation = Quaternion.Euler(0,currentAngle.y,0);
        }

    }
}
