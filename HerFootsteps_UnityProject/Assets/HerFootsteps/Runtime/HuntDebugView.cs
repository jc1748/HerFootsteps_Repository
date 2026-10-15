using UnityEngine;

namespace HerFootsteps
{
    public sealed class HuntDebugView : MonoBehaviour
    {
        [SerializeField] private HuntController hunt;
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PhantomFootstepsHallucination phantom;
        private void Update() { if (input != null && input.DebugTrailPressed) phantom.TryActivate(); }
        private void OnGUI()
        {
            if (hunt == null) return;
            float x = Mathf.Max(590, Screen.width - 480);
            GUI.Box(new Rect(x, 280, 465, 215), "HUNT — provisional tuning");
            string[] rows = {
                $"State {hunt.State} | Safe {hunt.SafeRemaining:0.0}s",
                $"Warning {hunt.WarningRemaining:0.0}s | Active {hunt.ActiveSeconds:0.0}s",
                $"Cooldown {hunt.CooldownRemaining:0.0}s | Trigger {hunt.TriggerSource}",
                $"AI {hunt.Cryptid.State} | Hidden {hiding.IsHidden}",
                hunt.LastGateResult,
                $"Evidence {hunt.InitialEvidence}",
                $"Phantom {phantom.Status} | {phantom.Remaining:0.0}s / cooldown {phantom.CooldownRemaining:0.0}s",
                "F7 phantom (normal composure/cooldown gates)",
                "Safe period starts on first player control; no queued triggers."
            };
            for(int i=0;i<rows.Length;i++) GUI.Label(new Rect(x+10,306+i*20,445,20),rows[i]);
        }
    }
}
