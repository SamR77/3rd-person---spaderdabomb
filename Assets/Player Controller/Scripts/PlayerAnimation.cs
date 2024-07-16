using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.Timeline.Actions;
using UnityEngine;


namespace SamR
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float locomotionBlendSpeed = 0.02f;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerState playerState;
        private PlayerController playerController;
        private PlayerActionsInput playerActionsInput;


        // Locomotion
        private static int inputXHash = Animator.StringToHash("inputX");
        private static int inputYHash = Animator.StringToHash("inputY");
        private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
        private static int isIdlingHash = Animator.StringToHash("isIdling");
        private static int isGroundedHash = Animator.StringToHash("isGrounded");
        private static int isFallingHash = Animator.StringToHash("isFalling");
        private static int isJumpingHash = Animator.StringToHash("isJumping");

        
        // Actions
        private static int isAttackingHash = Animator.StringToHash("isAttacking");
        private static int isGatheringHash = Animator.StringToHash("isGathering");
        private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
        private int[] actionHashes;

        // Camera/Rotation
        private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
        private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");

        private Vector3 currentBlendInput = Vector3.zero;

        private float sprintMaxBlendValue = 1.5f;
        private float runMaxBlendValue = 1.0f;
        private float walkMaxBlendValue = 0.5f;

        private void Awake()
        {
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();
            playerController = GetComponent<PlayerController>();
            playerActionsInput = GetComponent<PlayerActionsInput>();

            actionHashes = new int[] { isAttackingHash, isGatheringHash, isPlayingActionHash };
        }


        void Update()
        {
            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            bool isIdling = playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            bool isRunning = playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
            bool isSprinting = playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isJumping = playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
            bool isFalling = playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
            bool isGrounded = playerState.InGroundedState();
            bool isPlayingAction = actionHashes.Any(hash => animator.GetBool(hash));

            bool isRunBlendValue = isRunning || isJumping || isFalling;

            Vector2 inputTarget = isSprinting       ? playerLocomotionInput.MovementInput * sprintMaxBlendValue :
                                  isRunBlendValue   ? playerLocomotionInput.MovementInput * runMaxBlendValue :  
                                                      playerLocomotionInput.MovementInput * walkMaxBlendValue ; // walking


            currentBlendInput = Vector3.Lerp(currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

            animator.SetBool(isGroundedHash, isGrounded);
            animator.SetBool(isIdlingHash, isIdling);
            animator.SetBool(isFallingHash, isFalling);
            animator.SetBool(isJumpingHash, isJumping);
            animator.SetBool(isRotatingToTargetHash, playerController.IsRotatingToTarget);

            animator.SetBool(isAttackingHash, playerActionsInput.AttackPressed);
            animator.SetBool(isGatheringHash, playerActionsInput.GatherPressed);
            animator.SetBool(isPlayingActionHash, isPlayingAction);           

            animator.SetFloat(inputXHash, currentBlendInput.x);
            animator.SetFloat(inputYHash, currentBlendInput.y);
            animator.SetFloat(inputMagnitudeHash, currentBlendInput.magnitude);
            animator.SetFloat(rotationMismatchHash, playerController.RotationMismatch);

        }
    }
}
