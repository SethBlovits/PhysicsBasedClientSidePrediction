using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Movement{
    public static class MovementFunctions
    {
        // Start is called before the first frame update
        //temporarily accept input for method until a settings config is made.
        public static void addMovementForce(Rigidbody rigidbody,float horizontal, float vertical,float moveSpeed){
            Vector3 movementVector = horizontal*rigidbody.transform.right+vertical*rigidbody.transform.forward;
            rigidbody.AddForce(movementVector.normalized*moveSpeed,ForceMode.VelocityChange);
        }
        public static void addJumpForce(Rigidbody rigidbody,bool jump){
            
        }
        public static void checkHorizontalSpeed(Rigidbody rigidbody,float moveSpeed){//limit horizontal speed to what the walk speed is at the moment
            if(true){ //replace this later when a grounded check is implemented in the client prediction script
                Vector3 currentMovementVelocity = new Vector3(rigidbody.velocity.x,0,rigidbody.velocity.z);
                if(currentMovementVelocity.magnitude > moveSpeed){
                    currentMovementVelocity = Vector3.ClampMagnitude(currentMovementVelocity,moveSpeed);
                    rigidbody.velocity = new Vector3(currentMovementVelocity.x,rigidbody.velocity.y,currentMovementVelocity.z);
                } 
            }
        }

        public static void noInputBehaviour(Rigidbody rigidbody,float horizontal,float vertical){
            //also need a check for being grounded in the future
            Vector3 lerpVector = new Vector3(0,rigidbody.velocity.y,0);
            if(true){
                if(horizontal == 0 && vertical == 0){
                    rigidbody.velocity = Vector3.Lerp(rigidbody.velocity,lerpVector,0.1f);
                }
            }
        }
    }

}
