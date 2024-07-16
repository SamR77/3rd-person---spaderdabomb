using UnityEngine.InputSystem;
using UnityEngine;
using Cinemachine;

namespace SamR
{
    [DefaultExecutionOrder(-2)]
    public class ThirdPersonInput : MonoBehaviour, PlayerControls.IThirdPersonMapActions
    {
        #region Class Variables
  
        public Vector2 ScrollInput { get; private set; }

        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private float cameraZoomSpeed = 0.2f;
        [SerializeField] private float cameraMinZoom = 1f;
        [SerializeField] private float cameraMaxZoom = 5f;

        private Cinemachine3rdPersonFollow thirdPersonfollow;

        #endregion

        #region Startup

        private void Awake()
        {
            thirdPersonfollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        }


        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("PlayerControls is not initialized - cannot enable");
                return;
            }

            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Enable();
            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.PlayerControls == null)
            {
                Debug.LogError("PlayerControls is not initialized - cannot disable");
                return;
            }
            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Disable();
            PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.RemoveCallbacks(this);
        }


        #endregion

        #region Update

        private void Update()
        {
            thirdPersonfollow.CameraDistance = Mathf.Clamp(thirdPersonfollow.CameraDistance + ScrollInput.y, cameraMinZoom, cameraMaxZoom);            
        }

        #endregion

        private void LateUpdate()
        {
            ScrollInput = Vector2.zero;
        }


        #region Input Callbacks

        public void OnScrollCamera(InputAction.CallbackContext context)
        {
           if(!context.performed)
           {
                return;
           }

           Vector2 scrollInput = context.ReadValue<Vector2>();
           ScrollInput = -1f * scrollInput.normalized * cameraZoomSpeed;
           

        }


        #endregion
    }
}
