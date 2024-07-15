using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace SamR
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        public float RotationMismatch { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;


        [Header("Base Movement")]
        public float walkAcceleration = 25f;
        public float walkSpeed = 2f;
        public float runAcceleration = 35f;
        public float runSpeed = 4f;
        public float sprintAcceleration = 50f;
        public float inAirAcceleration = 25f;
        public float sprintSpeed = 7f;
        public float drag = 20f;
        public float gravity = 25f;
        public float jumpSpeed = 1.0f;
        public float movingThreshold = 0.01f;

        [Header("Animation")]
        public float playerModelRotationSpeed = 10.0f;
        public float rotateToTargetTime = 0.25f;

        [Header("Environment Details")]
        [SerializeField] private LayerMask groundLayers;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerState playerState;
        
        private Vector2 cameraRotation = Vector2.zero;
        private Vector2 playerTargetRotation = Vector2.zero;

        private bool jumpedLastFrame = false;
        private bool isRotatingClockwise = false;
        private float rotatingToTargetTimer = 0f;
        private float verticalVelocity = 0f;
        private float antiBump;
        private float stepOffset;

        private PlayerMovementState lastMovementState = PlayerMovementState.Falling;
        
        
        
        [Header("Output Debug Values")]
        [SerializeField] private float Velocity = 0f;

        #endregion

        #region Startup 
        private void Awake()
        {
            //characterController = GetComponent<CharacterController>();
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();

            antiBump = sprintSpeed;
            stepOffset = characterController.stepOffset;
            
        }
        #endregion

        #region Update Logic

        private void Update()
        {
            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();

            // Debug value, Rounded to 3 digits passed the decimal 0.000
            Velocity = Mathf.Round(characterController.velocity.magnitude * 1000f) / 1000f;
        }

        private void UpdateMovementState()
        {
            lastMovementState = playerState.CurrentPlayerMovementState;

            bool canRun = CanRun();
            bool isMovementInput = playerLocomotionInput.MovementInput != Vector2.zero;                                     // Order of these operations matters...
            bool isMovingLaterally = IsMovingLaterally();                                                                   // Order of these operations matters...
            bool isSprinting = playerLocomotionInput.SprintToggledOn && isMovingLaterally;                                  // Order of these operations matters...
            bool isWalking = (isMovingLaterally == true && canRun == false || playerLocomotionInput.WalkToggledOn == true); // Order of these operations matters...
            bool isGrounded = IsGrounded();                                                                                 // Order of these operations matters...

            PlayerMovementState lateralState =  isWalking == true                                       ? PlayerMovementState.Walking :
                                                isSprinting == true                                     ? PlayerMovementState.Sprinting :
                                                isMovingLaterally == true || isMovementInput == true    ? PlayerMovementState.Running :
                                                PlayerMovementState.Idling;

            playerState.SetPlayerMovementState(lateralState);

            // Control Airborne State
            if ((isGrounded == false || jumpedLastFrame == true) && characterController.velocity.y > 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                jumpedLastFrame = false;
                characterController.stepOffset = 0f;
            }

            else if ((isGrounded == false || jumpedLastFrame == true) && characterController.velocity.y <= 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                jumpedLastFrame = false;
                characterController.stepOffset = 0f;
            }
            else
            {
                characterController.stepOffset = stepOffset;
            }
        }
        private void HandleVerticalMovement()
        {
            bool isGrounded = playerState.InGroundedState();

            verticalVelocity -= gravity * Time.deltaTime;

            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -antiBump;
            }

            if (playerLocomotionInput.JumpPressed && isGrounded)
            {
                verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                jumpedLastFrame = true;
            }

            if (playerState.IsStateGroundedState(lastMovementState) && isGrounded == false)                
            {
                verticalVelocity += antiBump;
            }

            //Clamp terminal Velocity
         

        }
        private void HandleLateralMovement()
        {
            // Create a quick reference to the player's current state
            bool isSprinting = playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = playerState.InGroundedState();
            bool isWalking = playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;

            // State dependant acceleration and speed
            float lateralAcceleration = isGrounded  == false ? inAirAcceleration :
                                        isWalking   == true  ? walkAcceleration :
                                        isSprinting == true  ? sprintAcceleration :
                                                              runAcceleration;

            float clampLateralMagnitude = isGrounded  == false ? sprintSpeed : 
                                          isWalking   == true  ? walkSpeed :
                                          isSprinting == true  ? sprintSpeed :
                                                                 runSpeed;
            

            Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;

            Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;
            Vector3 newVelocity = characterController.velocity + movementDelta;

            // add drag to player
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z),  clampLateralMagnitude);
            newVelocity.y += verticalVelocity;
            newVelocity = isGrounded == false ? HandleSteepWalls(newVelocity) : newVelocity;


            // Move Character (unity Suggests only calling this once per frame)
            characterController.Move(newVelocity * Time.deltaTime);
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        { 
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= characterController.slopeLimit;
        
            if(validAngle == false && verticalVelocity < 0f)
            {
                velocity = Vector3.ProjectOnPlane(velocity, normal);
            }

            return velocity;
        }


        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            UpdateCameraRotation();
        }

        private void UpdateCameraRotation()
        {
            cameraRotation.x += lookSenseH * playerLocomotionInput.LookInput.x;
            cameraRotation.y = Mathf.Clamp(cameraRotation.y - lookSenseV * playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);                    

            playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * playerLocomotionInput.LookInput.x;
                                    
            float rotationTolerance = 90f;
            bool isIdling = playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = rotatingToTargetTimer > 0;

            // also rotate if we are not idling
            if (isIdling == false) 
            {
                RotatePlayerToTarget(); 
            }

            else if (MathF.Abs(RotationMismatch) > rotationTolerance || IsRotatingToTarget == true)
            {
                UpdateIdleRotation(rotationTolerance);
            }            

            playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0f);

            //get XZ angle between camera and player
            Vector3 camForwardProjectedXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            RotationMismatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);

            
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            // initiate a new rotation direction
            if (MathF.Abs(RotationMismatch) > rotationTolerance)
            {
                rotatingToTargetTimer = rotateToTargetTime;
                isRotatingClockwise = RotationMismatch > rotationTolerance;
            }
            rotatingToTargetTimer -= Time.deltaTime;

            // Rotate player
            if (isRotatingClockwise == true && RotationMismatch > 0f ||
                isRotatingClockwise == false && RotationMismatch < 0f  )
            {
                RotatePlayerToTarget();
            } 
        }

        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
        }
        #endregion

        #region State Checks

        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
            return lateralVelocity.magnitude > movingThreshold;

        }
        private bool IsGrounded()
        {
            bool grounded = playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

            return grounded;
        }

        private bool IsGroundedWhileGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - characterController.radius, transform.position.z);

            bool grounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayers, QueryTriggerInteraction.Ignore);

            return grounded == true;            
        }

        private bool IsGroundedWhileAirborne()
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            print(angle);
            bool validAngle = angle <= characterController.slopeLimit;

            return characterController.isGrounded && validAngle;
        }

       

            
        private bool CanRun()
        { 
        // Returns true if the player is moving forward or diagonally (45 degrees) (strafing or backwards movement will return false)
        return playerLocomotionInput.MovementInput.y >= MathF.Abs(playerLocomotionInput.MovementInput.x);
        }

        #endregion










    }
}
