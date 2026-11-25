using RUSH.Core;
using UnityEngine;

namespace RUSH.Players
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;
        private const float StandUpClearance = 0.05f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private Transform viewPivot;

        [Header("Speed")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] private float aimSpeed = 2.5f;
        [SerializeField, Min(0f)] private float crouchSpeed = 2f;
        [SerializeField, Min(0f)] private float acceleration = 12f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;

        [Header("Crouch")]
        [SerializeField] private bool toggleCrouch;
        [SerializeField, Min(0.5f)] private float crouchHeight = 1.2f;
        [SerializeField, Min(0f)] private float crouchTransitionSpeed = 10f;
        [SerializeField] private LayerMask ceilingMask = ~0;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalSpeed;
        private float standingHeight;
        private Vector3 standingCenter;
        private float standingViewHeight;
        private bool wantsCrouch;

        public bool SprintBlocked { get; set; }
        public bool IsAiming { get; set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded => controller.isGrounded;
        public float MoveAmount => enabled ? Mathf.Clamp01(input.Move.magnitude) : 0f;
        public float PlanarSpeed => new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            standingViewHeight = viewPivot.localPosition.y;
        }

        private void OnDisable()
        {
            IsSprinting = false;
            planarVelocity = Vector3.zero;
        }

        private void Update()
        {
            UpdateCrouch();

            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            IsSprinting = input.SprintHeld && !SprintBlocked && !IsCrouching && stamina.CanSprint && moveInput.y > 0.1f;

            if (IsSprinting)
                stamina.ConsumeSprint(Time.deltaTime);

            Vector3 targetVelocity = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * GetCurrentSpeed();
            planarVelocity = Vector3.Lerp(planarVelocity, targetVelocity, 1f - Mathf.Exp(-acceleration * Time.deltaTime));

            UpdateVerticalSpeed();
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private float GetCurrentSpeed()
        {
            if (IsSprinting)
                return sprintSpeed;

            float speed = IsAiming ? aimSpeed : walkSpeed;
            return IsCrouching ? Mathf.Min(speed, crouchSpeed) : speed;
        }

        private void UpdateVerticalSpeed()
        {
            if (controller.isGrounded)
            {
                verticalSpeed = GroundedVerticalSpeed;

                if (input.JumpPressed && !IsCrouching && stamina.TryConsumeJump())
                    verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalSpeed += gravity * Time.deltaTime;
        }

        private void UpdateCrouch()
        {
            if (toggleCrouch)
            {
                if (input.CrouchPressed)
                    wantsCrouch = !wantsCrouch;
            }
            else
            {
                wantsCrouch = input.CrouchHeld;
            }

            if (wantsCrouch)
                IsCrouching = true;
            else if (IsCrouching && CanStandUp())
                IsCrouching = false;

            float targetHeight = IsCrouching ? crouchHeight : standingHeight;
            float blend = 1f - Mathf.Exp(-crouchTransitionSpeed * Time.deltaTime);
            float height = Mathf.Lerp(controller.height, targetHeight, blend);
            if (Mathf.Abs(height - targetHeight) < 0.001f)
                height = targetHeight;

            ApplyHeight(height);
        }

        private void ApplyHeight(float height)
        {
            float offset = standingHeight - height;

            controller.height = height;
            controller.center = standingCenter - Vector3.up * (offset * 0.5f);

            Vector3 viewPosition = viewPivot.localPosition;
            viewPosition.y = standingViewHeight - offset;
            viewPivot.localPosition = viewPosition;
        }

        private bool CanStandUp()
        {
            float radius = controller.radius;
            float distance = standingHeight - controller.height + StandUpClearance;
            Vector3 origin = transform.TransformPoint(controller.center) + Vector3.up * (controller.height * 0.5f - radius);

            return !PhysicsQuery.SphereCast(new Ray(origin, Vector3.up), radius, distance, ceilingMask, transform, out _);
        }
    }
}
