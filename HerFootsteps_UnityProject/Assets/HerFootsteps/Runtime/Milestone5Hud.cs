using UnityEngine;

namespace HerFootsteps
{
    public sealed class Milestone5Hud : MonoBehaviour
    {
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private ComposureRateSource passive;
        [SerializeField] private CryptidComposureSource cryptidSource;
        [SerializeField] private FalseTrailHallucination trail;
        [SerializeField] private WildlifeHallucination wildlife;
        [SerializeField] private string controlHint = "Tab: inventory / debug controls   F7: trail   F8: wildlife";
        private void Update()
        {
            if (input == null) return;
            if (input.DebugTrailPressed && trail != null) trail.TryActivate();
            if (input.DebugWildlifePressed && wildlife != null) wildlife.TryActivate();
        }
        private void OnGUI()
        {
            if (composure == null || (input != null && input.ModalOpen)) return;
            GUI.Box(new Rect(16, 280, 560, 216), "MILESTONE 5 — COMPOSURE / INVENTORY / HALLUCINATIONS");
            string[] rows = {
                $"Composure: {composure.Current:0.0}/{composure.Capacity:0}  {composure.State}  Broken: {composure.IsBroken}",
                "Sources: " + composure.ActiveSources,
                "Last change: " + composure.LastSource,
                "Transition: " + composure.LastTransition,
                "Broken entries: " + composure.BrokenEntryCount + " (no hunt trigger)",
                controlHint,
                trail != null ? $"Trail: {trail.Status}  active {trail.Remaining:0.0}s / cooldown {trail.CooldownRemaining:0.0}s" : "",
                wildlife != null ? $"Wildlife: {wildlife.Status}  react {wildlife.ReactionRemaining:0.0}s / cooldown {wildlife.CooldownRemaining:0.0}s" : "",
                inventory != null ? inventory.Status : ""
            };
            for (int i = 0; i < rows.Length; i++) GUI.Label(new Rect(28, 306 + i * 20, 536, 20), rows[i]);
        }
        public void DrawControls()
        {
            GUILayout.Space(12); GUILayout.Label("PROTOTYPE DEBUG — independent conditions (not final UI)");
            if (composure != null)
            {
                GUILayout.Label($"Composure {composure.Current:0.0} — {composure.State}; {composure.LastTransition}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Test loss -25")) composure.Apply(-25, "Debug loss");
                if (GUILayout.Button("Test recovery +25")) composure.Apply(25, "Debug recovery");
                GUILayout.EndHorizontal();
            }
            if (passive != null) passive.SourceActive = GUILayout.Toggle(passive.SourceActive, "Enable passive dangerous-environment loss");
            if (cryptidSource != null)
            {
                cryptidSource.ProximityEnabled = GUILayout.Toggle(cryptidSource.ProximityEnabled, "Enable cryptid proximity loss");
                cryptidSource.PursuitEnabled = GUILayout.Toggle(cryptidSource.PursuitEnabled, "Enable pursuit loss");
                cryptidSource.DetectionEnabled = GUILayout.Toggle(cryptidSource.DetectionEnabled, "Enable loss on detection");
            }
            if (trail != null)
            {
                trail.Automatic = GUILayout.Toggle(trail.Automatic, "Automatic false trail at Low/Broken");
                if (GUILayout.Button("Trigger false trail (state/range/cooldown still required)")) trail.TryActivate();
            }
            if (wildlife != null)
            {
                wildlife.Automatic = GUILayout.Toggle(wildlife.Automatic, "Automatic wildlife at Low/Broken and in view");
                if (GUILayout.Button("Trigger wildlife (state/range/view/cooldown still required)")) wildlife.TryActivate();
            }
        }
    }
}
