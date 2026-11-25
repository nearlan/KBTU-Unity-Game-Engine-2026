using UnityEngine;
using UnityEngine.InputSystem;

namespace KBTU.Players
{
    public class PlayerInputReader : MonoBehaviour
    {
        private const string MoveAction = "Move";
        private const string LookAction = "Look";
        private const string AttackAction = "Attack";
        private const string AimAction = "Aim";
        private const string ReloadAction = "Reload";
        private const string DropAction = "Drop";
        private const string InteractAction = "Interact";
        private const string SprintAction = "Sprint";
        private const string JumpAction = "Jump";

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

            move = actionMap.FindAction(MoveAction, true);
            look = actionMap.FindAction(LookAction, true);
            attack = actionMap.FindAction(AttackAction, true);
            aim = actionMap.FindAction(AimAction, true);
            reload = actionMap.FindAction(ReloadAction, true);
            drop = actionMap.FindAction(DropAction, true);
            interact = actionMap.FindAction(InteractAction, true);
            sprint = actionMap.FindAction(SprintAction, true);
            jump = actionMap.FindAction(JumpAction, true);
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
