using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CustomMovement
{
    
    public class MovementScript : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField]
        Rigidbody m_rigidbody;
        [SerializeField]
        MovementConfig m_movementConfig;
        [SerializeField]
        SettingsConfig settingsConfig;
        Vector3 movementVector;
        void Start()
        {
            movementVector = Vector3.zero;
            m_movementConfig.grounded = true;
            
        }

        // Update is called once per frame
        //
        void FixedUpdate()
        {
            InputRead();
            applyExtraGravity(m_movementConfig.gravity);
            AirMovement(movementVector);
            //slowOnZeroSpeed();
            
            horizontalSpeedCheck();
            slowOnZeroSpeed();
            
            movementVector = Vector3.zero;
            
        }

        public void InputRead(){

            if(Input.GetKey(settingsConfig.forward)){
                movementVector += transform.forward * m_movementConfig.maxInputSpeed;
            }
            if(Input.GetKey(settingsConfig.back)){
                movementVector -= transform.forward * m_movementConfig.maxInputSpeed;
            }
            if(Input.GetKey(settingsConfig.left)){
                movementVector -= transform.right * m_movementConfig.maxInputSpeed;
            }
            if(Input.GetKey(settingsConfig.right)){
                movementVector += transform.right * m_movementConfig.maxInputSpeed;
            }
            if(Input.GetKey(settingsConfig.jump)){
                movementVector += transform.up * m_movementConfig.jumpHeight;
            }
            if(Input.GetKey(settingsConfig.sprint)){
                m_movementConfig.sprinting = true;
            }
            if(m_movementConfig.grounded){
                m_rigidbody.AddForce(movementVector,ForceMode.VelocityChange);
            }
            
            
        }

        public void horizontalSpeedCheck(){
            if(m_movementConfig.grounded){
                Vector3 tempSpeed = new Vector3(m_rigidbody.velocity.x,0,m_rigidbody.velocity.z);
                switch(m_movementConfig.sprinting){
                    case true:
                        if(tempSpeed.magnitude > m_movementConfig.walkSpeed && m_movementConfig.grounded){
                            tempSpeed = Vector3.ClampMagnitude(tempSpeed,m_movementConfig.walkSpeed);
                            m_rigidbody.velocity = new Vector3(tempSpeed.x,m_rigidbody.velocity.y,tempSpeed.z);
                            //Debug.Log(new Vector3(tempSpeed.x,m_rigidbody.velocity.y,tempSpeed.z));
                        }
                        break;
                    case false:
                        if(tempSpeed.magnitude > m_movementConfig.sprintSpeed && m_movementConfig.grounded){
                            tempSpeed = Vector3.ClampMagnitude(tempSpeed,m_movementConfig.sprintSpeed);
                            m_rigidbody.velocity = new Vector3(tempSpeed.x,m_rigidbody.velocity.y,tempSpeed.z);
                            //Debug.Log(new Vector3(tempSpeed.x,m_rigidbody.velocity.y,tempSpeed.z));
                        }
                        break;
                }
            }
        }
        public void slowOnZeroSpeed(){
            if(movementVector == Vector3.zero && m_movementConfig.grounded){
                m_rigidbody.velocity = new Vector3(0,m_rigidbody.velocity.y,0);
            }
        }
      
        public void AirMovement(Vector3 direction){
            if(!m_movementConfig.grounded){
                Vector3 projection = Vector3.Project(m_rigidbody.velocity,direction);
                bool isAway = Vector3.Dot(projection.normalized , direction.normalized) <= 0f;
               
                if(projection.magnitude < m_movementConfig.aerialSpeed || isAway){
                    
                    Vector3 vc = direction.normalized * m_movementConfig.airStrafeForce;

                    if(!isAway){
                        vc = Vector3.ClampMagnitude(vc,m_movementConfig.aerialSpeed - projection.magnitude);
                        
                    }
                    else{
                        vc = Vector3.ClampMagnitude(vc, m_movementConfig.aerialSpeed+projection.magnitude);
                    }
                    m_rigidbody.AddForce(vc,ForceMode.VelocityChange);
                }
            }
        }
        public void applyExtraGravity(float gravityMultiplier){
            if(!m_movementConfig.grounded){
                m_rigidbody.AddForce(-transform.up*gravityMultiplier,ForceMode.VelocityChange);
            }
            
        }
        private void OnCollisionEnter(Collision other) {
            if(other.gameObject.layer == 6){
                m_movementConfig.grounded = true;
            }
        }
        private void OnCollisionExit(Collision other) {
            if(other.gameObject.layer == 6){
                m_movementConfig.grounded = false;
            }
        }

    }
}
