using UnityEngine;

namespace RUSH.Players
{
    [DefaultExecutionOrder(-50)]
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private Camera viewCamera;

        [Header("Sensitivity")]
        [SerializeField, Min(0f)] private float sensitivity = 0.1f;
        [SerializeField] private Vector2 pitchLimits = new(-80f, 80f);

        [Header("Field Of View")]
        [SerializeField, Range(30f, 120f)] private float defaultFieldOfView = 70f;
        [SerializeField, Range(30f, 120f)] private float aimFieldOfView = 55f;
        [SerializeField, Min(0f)] private float fieldOfViewSpeed = 10f;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float recoilSnappiness = 25f;
        [SerializeField, Min(0f)] private float recoilReturnSpeed = 6f;
        [SerializeField, Min(0f)] private float recoilReturnDelay = 0.15f;

        private float pitch;
        private bool isAiming;
        private Vector2 targetRecoil;
        private Vector2 currentRecoil;
        private float recoilReturnCooldown;

        public Camera ViewCamera => viewCamera;

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Start()
        {
            viewCamera.fieldOfView = defaultFieldOfView;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Vector2 delta = input.Look * sensitivity;

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            delta.y += AbsorbRecoilCompensation(delta.y);
            pitch = Mathf.Clamp(pitch - delta.y, pitchLimits.x, pitchLimits.y);

            UpdateRecoil(deltaTime);
            float viewPitch = Mathf.Clamp(pitch - currentRecoil.x, pitchLimits.x, pitchLimits.y);
            pitchPivot.localRotation = Quaternion.Euler(viewPitch, currentRecoil.y, 0f);

            float targetFieldOfView = isAiming ? aimFieldOfView : defaultFieldOfView;
            float blend = 1f - Mathf.Exp(-fieldOfViewSpeed * deltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFieldOfView, blend);
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        public void AddRecoil(Vector2 kick)
        {
            targetRecoil += kick;
            recoilReturnCooldown = recoilReturnDelay;
        }

        private void UpdateRecoil(float deltaTime)
        {
            if (recoilReturnCooldown > 0f)
                recoilReturnCooldown -= deltaTime;
            else
                targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, 1f - Mathf.Exp(-recoilReturnSpeed * deltaTime));

            currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, 1f - Mathf.Exp(-recoilSnappiness * deltaTime));
        }

        private float AbsorbRecoilCompensation(float verticalDelta)
        {
            if (verticalDelta >= 0f || targetRecoil.x <= 0f)
                return 0f;

            float absorbed = Mathf.Min(-verticalDelta, targetRecoil.x);
            targetRecoil.x -= absorbed;
            currentRecoil.x -= absorbed;
            return absorbed;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
