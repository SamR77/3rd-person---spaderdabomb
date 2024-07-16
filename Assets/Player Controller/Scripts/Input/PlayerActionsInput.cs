using UnityEngine.InputSystem;
using UnityEngine;
using Cinemachine;

namespace SamR
{
    [DefaultExecutionOrder(-2)]
    public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        #region Class Variables
        public PlayerLocomotionInput playerLocomotionInput;
        public PlayerState playerState;
        public bool GatherPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        #endregion

        #region Startup
        private void Awake()
        {            
            playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            playerState = GetComponent<PlayerState>();           

            Debug.Log("Awake playerLocomotionInput = " + playerLocomotionInput);
            Debug.Log("Awake playerState = " + playerState);
        }

        private void Start()
        {
            Debug.Log("Start playerLocomotionInput = " + playerLocomotionInput);
            Debug.Log("Start playerState " + playerState);
        }
    




        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot enable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("Player controls is not initialized - cannot disable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
            PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(this);
        }
        #endregion

        #region Update
        private void Update()
        {
            if (playerLocomotionInput != null)
            {
                Debug.Log(playerLocomotionInput.MovementInput);

                if (playerLocomotionInput.MovementInput != Vector2.zero ||
                    (playerState != null &&
                     (playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                      playerState.CurrentPlayerMovementState == PlayerMovementState.Falling)))
                {
                    GatherPressed = false;
                }
            }
            //else
            //{
            //    Debug.LogError("PlayerLocomotionInput is null in Update!");
            //}
        }

        public void SetGatherPressedFalse()
        {
            GatherPressed = false;
        }

        public void SetAttackPressedFalse()
        {
            AttackPressed = false;
        }
        #endregion

        #region Input Callbacks

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            AttackPressed = true;
            print("attack pressed");
        }

        public void OnGather(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            GatherPressed = true;

            print("gather pressed");
        }
        #endregion
    }
}
