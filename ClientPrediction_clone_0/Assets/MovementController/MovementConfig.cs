using UnityEngine;


namespace CustomMovement{
    [System.Serializable]
    public class MovementConfig
    {
        [Header ("General Physics Parameters")]
        public float friction = 1f;
        public float maxInputSpeed = 5f;
        public float maxVelocity = 50f;
        public float gravity = 2f;

        [Header ("Ground Movement")]
        public float walkSpeed = 7f;
        public float sprintSpeed = 12f;
        public float acceleration = 14f;
        public float deceleration = 10f;

        [Header ("Status")]
        public bool grounded = false;
        public bool sprinting = false;

        [Header ("Air Movement")]
        public float jumpHeight = 5f;
        public float aerialSpeed = 4f;
        public float airStrafeForce = 1f;

    }
}
