using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BasicMovement : NetworkBehaviour
{
    // Start is called before the first frame update
    Rigidbody m_rigidBody;
    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.isKinematic = true;
        m_rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; 
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W)){
            Debug.Log(IsOwner);
            m_rigidBody.MovePosition(transform.forward+transform.position);
        }
        
    }
}
