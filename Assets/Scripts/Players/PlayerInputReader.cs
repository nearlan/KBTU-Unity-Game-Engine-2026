using UnityEngine;
using UnityEngine.InputSystem;

namespace KBTU.Players
{
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMapName = "Player";

        private InputActionMap actionMap;
        private InputAction move;
        private InputAction look;
        private InputAction attack;
        private InputAction aim;
        private InputAction reload;
        private InputAction drop;
        private InputAction interact;
        private InputAction sprint;
        private InputAction jump;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool AttackPressed => attack.WasPressedThisFrame();
        public bool AttackHeld => attack.IsPressed();
        public bool AimPressed => aim.WasPressedThisFrame();
        public bool AimHeld => aim.IsPressed();
        public bool ReloadPressed => reload.WasPressedThisFrame();
        public bool DropPressed => drop.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool JumpPressed => jump.WasPressedThisFrame();

        private void Awake()
        {
            InputActionAsset asset = actions != null ? actions : InputSystem.actions;
            actionMap = asset.FindActionMap(actionMapName, true);

            move = actionMap.FindAction("Move", true);
            look = actionMap.FindAction("Look", true);
            attack = actionMap.FindAction("Attack", true);
            aim = actionMap.FindAction("Aim", true);
            reload = actionMap.FindAction("Reload", true);
            drop = actionMap.FindAction("Drop", true);
            interact = actionMap.FindAction("Interact", true);
            sprint = actionMap.FindAction("Sprint", true);
            jump = actionMap.FindAction("Jump", true);
        }

        private void OnEnable()
        {
            actionMap.Enable();
        }

        private void OnDisable()
        {
            actionMap.Disable();
        }
    }
}
