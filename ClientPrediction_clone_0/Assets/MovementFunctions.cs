using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//could make this not longer a static class and have the config parameters be variables
namespace Movement{
    public class MovementFunctions
    {
        public float moveSpeed;
        public float jumpHeight;
        public bool isGrounded;
        public float gravityStrength;
        public bool isJumping;
        public bool isCrounching;
        public bool isSliding;
        public float minTimeBetweenTicks;
        public float maxWalkSpeed;
        public float maxCrouchSpeed;
        public float slideSpeedThreshold;
        public float slideFriction;
        public float walkFriction;

        // Start is called before the first frame update
        //temporarily accept input for method until a settings config is made.
        public MovementFunctions(){
            moveSpeed = 1f;
            jumpHeight = 1f;
            isGrounded=false;
            gravityStrength=1f;
        }
        public void addMovementForce(Rigidbody rigidbody,float horizontal, float vertical){
            Vector3 movementVector = horizontal*rigidbody.transform.right+vertical*rigidbody.transform.forward;
            Vector3 aerialForce = movementVector.normalized*moveSpeed*minTimeBetweenTicks;
            if(isGrounded && !isSliding){
                rigidbody.AddForce(movementVector.normalized*moveSpeed*minTimeBetweenTicks,ForceMode.VelocityChange);
            }
            else{
                rigidbody.AddForce(movementVector.normalized*moveSpeed/4f*minTimeBetweenTicks,ForceMode.VelocityChange);
                Vector3 currentMovementVelocity = new Vector3(rigidbody.velocity.x,0,rigidbody.velocity.z);
                if(currentMovementVelocity.magnitude > maxCrouchSpeed){
                    currentMovementVelocity = Vector3.ClampMagnitude(currentMovementVelocity,maxWalkSpeed);
                    rigidbody.velocity = Vector3.Lerp(rigidbody.velocity,new Vector3(currentMovementVelocity.x,rigidbody.velocity.y,currentMovementVelocity.z),0.7f);
                }

            }
        }
        public void rotatePlayer(Rigidbody rigidbody,float horizontal,float vertical){
            Vector3 currentAngle = rigidbody.transform.rotation.eulerAngles;
            currentAngle.y += horizontal; 
            rigidbody.transform.rotation = Quaternion.Euler(0,currentAngle.y,0);
        }
        public void addJumpForce(Rigidbody rigidbody,bool jump,int tick){//need to make some way to be able to reference movement config
            if(jump && isGrounded){
                //Debug.Log("Jump function is called at tick: " + tick);
                rigidbody.AddForce(rigidbody.transform.up * jumpHeight,ForceMode.VelocityChange);
                isGrounded = false;
            }
            //else{
            //    Debug.Log("Tick: " + tick + " jump: "+jump+ " isGrounded: "+ isGrounded + " velocity: "+rigidbody.velocity.y);
            //}
        }
        public void addGravity(Rigidbody rigidbody){
            rigidbody.AddForce(Vector3.down * gravityStrength,ForceMode.VelocityChange);
        }
        public void checkHorizontalSpeed(Rigidbody rigidbody){//limit horizontal speed to what the walk speed is at the moment
            if(isGrounded && !isSliding){ //replace this later when a grounded check is implemented in the client prediction script
                Vector3 currentMovementVelocity = new Vector3(rigidbody.velocity.x,0,rigidbody.velocity.z);
                float limitSpeed = isCrounching ? maxCrouchSpeed : maxWalkSpeed;
                if(currentMovementVelocity.magnitude > limitSpeed){
                    currentMovementVelocity = Vector3.ClampMagnitude(currentMovementVelocity,limitSpeed);
                    rigidbody.velocity = Vector3.Lerp(rigidbody.velocity,new Vector3(currentMovementVelocity.x,rigidbody.velocity.y,currentMovementVelocity.z),0.7f);
                } 
            }
        }
        public void checkForGround(Rigidbody rigidbody){
            
            isGrounded = Physics.Raycast(rigidbody.transform.position,Vector3.down,rigidbody.transform.localScale.y+0.05f,~LayerMask.NameToLayer("Ground"));
            //Debug.Log("In class check: "+Physics.Raycast(rigidbody.transform.position,Vector3.down,rigidbody.transform.localScale.y+0.05f,~LayerMask.NameToLayer("Ground")));
        }
        public void slideBehaviour(Rigidbody rigidbody,PhysicMaterial physicMaterial, bool slideInput){
            isCrounching = slideInput;
            Vector3 currentMovementVelocity = new Vector3(rigidbody.velocity.x,0,rigidbody.velocity.z);
            
            if(currentMovementVelocity.magnitude > slideSpeedThreshold && isCrounching){
                isSliding = true;
                physicMaterial.dynamicFriction = slideFriction;
            }
            else{
                isSliding = false;
                physicMaterial.dynamicFriction = walkFriction; 
            }
            
        }
        public void noInputBehaviour(Rigidbody rigidbody,float horizontal,float vertical){//for whatever reason this causes desync issues in multiplayer
            //also need a check for being grounded in the future
            Vector3 lerpVector = new Vector3(0,rigidbody.velocity.y,0);
            if(isGrounded){
                if(horizontal == 0 && vertical == 0){
                    rigidbody.velocity = Vector3.Lerp(rigidbody.velocity,lerpVector,0.1f);
                }
            }
        }
        
    }

}
