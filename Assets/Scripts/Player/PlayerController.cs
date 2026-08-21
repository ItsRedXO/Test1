using ActionRPG.Input;
using UnityEngine;

namespace ActionRPG.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float rotationSpeed = 1200f;
        [SerializeField] private float gravity = -20f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;

        private CharacterController characterController;
        private UnityEngine.Camera mainCamera;
        private float verticalVelocity;

        public float MoveSpeedMultiplier { get; set; } = 1f;
        public bool CanRotate { get; set; } = true;
        public Vector3 CurrentFacingDirection { get; private set; } = Vector3.forward;
        public Vector3 LastMoveVector { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            mainCamera = UnityEngine.Camera.main;
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            if (InputHandler.Instance == null) return;

            HandleMovement();
            HandleRotation();
        }

        private void HandleMovement()
        {
            Vector2 input = InputHandler.Instance.MoveInput;

            // Camera-aligned horizontal movement
            Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
            Vector3 cameraRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraRight * input.x + cameraForward * input.y);
            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

            LastMoveVector = moveDirection;

            // Gravity
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * (moveSpeed * MoveSpeedMultiplier);
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (!CanRotate) return;

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
                if (mainCamera == null) return;
            }

            Ray ray = mainCamera.ScreenPointToRay(InputHandler.Instance.MousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = hitPoint - transform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    lookDirection.Normalize();
                    CurrentFacingDirection = lookDirection;
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                    float effectiveRotationSpeed = rotationSpeed < 360f ? 1200f : rotationSpeed;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, effectiveRotationSpeed * Time.deltaTime);
                }
            }
        }
    }
}

