using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SamR
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;

        [Header("Base Movement")]
        public float runAcceleration = 0.25f;
        public float runSpeed = 4f;
        public float sprintAcceleration = 0.5f;
        public float sprintSpeed = 7f;
        public float drag = 0.1f;        
        public float gravity = 25f;
        public float jumpSpeed = 1.0f;
        public float movingThreshold = 0.01f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.01f;
        public float lookSenseV = 0.01f;
        public float lookLimitV = 89f;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerState playerState;
        private Vector2 cameraRotation = Vector2.zero;
        private Vector2 playerTargetRotation = Vector2.zero;

        [Header("Output Debug Values")]
        [SerializeField] private float Velocity = 0f;

        private float verticalVelocity = 0f;

        #endregion

        #region Startup 
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();
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
            bool isMovementInput = playerLocomotionInput.MovementInput != Vector2.zero;         // Order of these
            bool isMovingLaterally = IsMovingLaterally();                                       // operations
            bool isSprinting = playerLocomotionInput.SprintToggledOn && isMovingLaterally;      // matter
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState =  isSprinting ? PlayerMovementState.Sprinting :
                                                isMovingLaterally || isMovementInput ? PlayerMovementState.Running :
                                                PlayerMovementState.Idling;

            playerState.SetPlayerMovementState(lateralState);

            // Control airborne state
            if (!isGrounded && characterController.velocity.y >= 0f )
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
            }
            else if(!isGrounded && characterController.velocity.y < 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            }
        }
        private void HandleVerticalMovement()
        {
            bool isGrounded = playerState.InGroundedState();

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }

            verticalVelocity -= gravity * Time.deltaTime;

            if (playerLocomotionInput.JumpPressed && isGrounded)
            {
                verticalVelocity += MathF.Sqrt(jumpSpeed * 3 * gravity);
            }


           
        }
            
        private void HandleLateralMovement()
        {
            // Create a quick reference to the player's current state
            bool isSprinting = playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = playerState.InGroundedState();

            // State dependant acceleration and speed
            float clampLateralMagnitude = isSprinting ? sprintSpeed : runSpeed;
            float lateralAcceleration = isSprinting ? sprintAcceleration : runAcceleration;

            Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;

            Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;
            Vector3 newVelocity = characterController.velocity + movementDelta;

            // add drag to player
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(newVelocity, clampLateralMagnitude);
            newVelocity.y += verticalVelocity;

            // Move Character (unity Suggests only calling this once per frame)
            characterController.Move(newVelocity * Time.deltaTime);
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            cameraRotation.x += lookSenseH * playerLocomotionInput.LookInput.x;
            cameraRotation.y = Mathf.Clamp(cameraRotation.y - lookSenseV * playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

            playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * playerLocomotionInput.LookInput.x;
            transform.rotation = Quaternion.Euler(0f, playerTargetRotation.x, 0f);

            playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0f);
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
            return characterController.isGrounded;
        }
        #endregion










    }
}
