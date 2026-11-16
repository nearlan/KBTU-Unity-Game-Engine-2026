using UnityEngine;

namespace KBTU.Players
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;

        [SerializeField] private PlayerInputReader input;

        [Header("Speed")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] private float aimSpeed = 2.5f;
        [SerializeField, Min(0f)] private float acceleration = 12f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalSpeed;

        public bool SprintBlocked { get; set; }
        public bool IsAiming { get; set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller.isGrounded;
        public float MoveAmount => enabled ? Mathf.Clamp01(input.Move.magnitude) : 0f;
        public float PlanarSpeed => new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnDisable()
        {
            IsSprinting = false;
            planarVelocity = Vector3.zero;
        }

        private void Update()
        {
            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            IsSprinting = input.SprintHeld && !SprintBlocked && moveInput.y > 0.1f;

            Vector3 targetVelocity = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * GetCurrentSpeed();
            planarVelocity = Vector3.Lerp(planarVelocity, targetVelocity, 1f - Mathf.Exp(-acceleration * Time.deltaTime));

            UpdateVerticalSpeed();
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private float GetCurrentSpeed()
        {
            if (IsSprinting)
                return sprintSpeed;

            return IsAiming ? aimSpeed : walkSpeed;
        }

        private void UpdateVerticalSpeed()
        {
            if (controller.isGrounded)
            {
                verticalSpeed = GroundedVerticalSpeed;

                if (input.JumpPressed)
                    verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalSpeed += gravity * Time.deltaTime;
        }
    }
}
