using UnityEngine;
using UnityEngine.InputSystem;

namespace HerFootsteps
{
    // Owns only a runtime instance of the existing project actions, never new bindings.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset instance;
        private InputAction move, look, sprint, interact, cancel, click, flashlight, holdBreath;
        private InputAction inventory, debugTrail, debugWildlife;
        public bool ModalOpen { get; private set; }
        private bool captured;
        private int capturedFrame;

        public bool HasControl => !ModalOpen && captured && Application.isFocused &&
            Cursor.lockState == CursorLockMode.Locked && Time.frameCount > capturedFrame;
        public Vector2 Move => HasControl ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => HasControl ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool Sprint => HasControl && sprint.IsPressed();
        public bool InteractPressed => HasControl && interact.WasPressedThisFrame();
        public bool InteractHeld => HasControl && interact.IsPressed();
        public bool FlashlightPressed => HasControl && flashlight != null && flashlight.WasPressedThisFrame();
        public bool HoldBreathHeld => HasControl && holdBreath != null && holdBreath.IsPressed();
        public bool InventoryPressed => Application.isFocused && inventory != null && inventory.WasPressedThisFrame();
        public bool CancelPressed => Application.isFocused && cancel != null && cancel.WasPressedThisFrame();
        public bool DebugTrailPressed => HasControl && debugTrail != null && debugTrail.WasPressedThisFrame();
        public bool DebugWildlifePressed => HasControl && debugWildlife != null && debugWildlife.WasPressedThisFrame();

        public void SetModalOpen(bool open)
        {
            ModalOpen = open;
            SetCapture(!open && isActiveAndEnabled && Application.isFocused);
        }

        public void Configure(InputActionAsset source) => actions = source;

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerInputReader needs the project Input Actions asset.", this);
                enabled = false;
                return;
            }

            instance = Instantiate(actions);
            instance.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            move = instance.FindAction("Player/Move", true);
            look = instance.FindAction("Player/Look", true);
            sprint = instance.FindAction("Player/Sprint", true);
            interact = instance.FindAction("Player/Interact", true);
            cancel = instance.FindAction("UI/Cancel", true);
            click = instance.FindAction("UI/Click", true);
            flashlight = instance.FindAction("Player/Flashlight", false);
            flashlight?.Enable();
            holdBreath = instance.FindAction("Player/HoldBreath", false);
            holdBreath?.Enable();
            inventory = instance.FindAction("Player/Inventory", false);
            debugTrail = instance.FindAction("Player/DebugFalseTrail", false);
            debugWildlife = instance.FindAction("Player/DebugWildlife", false);
            inventory?.Enable(); debugTrail?.Enable(); debugWildlife?.Enable();
            foreach (var action in new[] { move, look, sprint, interact, cancel, click })
                action.Enable();
            SetCapture(true);
        }

        private void Update()
        {
            if (ModalOpen) return; // Inventory owns cursor; UI clicks must not recapture world input.
            if (cancel.WasPressedThisFrame()) SetCapture(false);
            else if (!captured && Application.isFocused && click.WasPressedThisFrame())
                SetCapture(true);
            if (captured && Cursor.lockState != CursorLockMode.Locked) SetCapture(false);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SetCapture(false);
        }

        private void SetCapture(bool value)
        {
            captured = value;
            capturedFrame = Time.frameCount;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }

        private void OnDisable()
        {
            ModalOpen = false;
            SetCapture(false);
            if (instance == null) return;
            instance.Disable();
            Destroy(instance);
            instance = null;
        }
    }
}
