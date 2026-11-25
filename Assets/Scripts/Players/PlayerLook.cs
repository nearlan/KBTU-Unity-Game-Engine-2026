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

        private float pitch;
        private bool isAiming;

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
            Vector2 delta = input.Look * sensitivity;

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, pitchLimits.x, pitchLimits.y);
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float targetFieldOfView = isAiming ? aimFieldOfView : defaultFieldOfView;
            float blend = 1f - Mathf.Exp(-fieldOfViewSpeed * Time.deltaTime);
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFieldOfView, blend);
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
