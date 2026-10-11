using UnityEngine;
using UnityEngine.InputSystem;

namespace HerFootsteps
{
    public sealed class PresentationDebugMode : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] panels;
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private ComposureRateSource passive;
        [SerializeField] private string toggleBinding = "<Keyboard>/f3";
        [SerializeField] private string lossBinding = "<Keyboard>/f4", recoveryBinding = "<Keyboard>/f5";
        private InputAction toggle, loss, recovery;
        public bool Visible { get; private set; }
        private void OnEnable()
        {
            if (!Debug.isDebugBuild && !Application.isEditor) { SetVisible(false); enabled = false; return; }
            toggle = new InputAction("Development overlay", InputActionType.Button, toggleBinding);
            loss = new InputAction("Development loss", InputActionType.Button, lossBinding);
            recovery = new InputAction("Development recovery", InputActionType.Button, recoveryBinding);
            toggle.Enable(); loss.Enable(); recovery.Enable(); SetVisible(false);
        }
        private void OnDisable() { toggle?.Dispose(); loss?.Dispose(); recovery?.Dispose(); SetVisible(false); }
        public void SetVisible(bool visible) { Visible = visible; if (panels != null) foreach (var panel in panels) if (panel != null) panel.enabled = visible; }
        private void Update()
        {
            if (!Application.isFocused) return;
            if (toggle.WasPressedThisFrame()) SetVisible(!Visible);
            if (!Visible) return;
            if (loss.WasPressedThisFrame()) composure.Apply(-25, "Development loss");
            if (recovery.WasPressedThisFrame()) composure.Apply(25, "Development recovery");
        }
        private void OnGUI()
        {
            if (Visible) GUI.Label(new Rect(16, 500, 750, 30), "F3 close debug | F4 -25 | F5 +25 | F7 trail | F8 wildlife | tune source switches in Inspector");
        }
    }
}
