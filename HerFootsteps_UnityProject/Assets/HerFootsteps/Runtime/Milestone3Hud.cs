using UnityEngine;

namespace HerFootsteps
{
    public sealed class Milestone3Hud : MonoBehaviour
    {
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private PlayerBreath breath;
        private void OnGUI()
        {
            if (hiding == null || breath == null) return;
            GUI.Box(new Rect(16, 140, 460, 130), "MILESTONE 3 - HIDING / BREATH TEST");
            GUI.Label(new Rect(28, 164, 440, 24), $"Hidden: {hiding.IsHidden}   |   Breath: {breath.Breath:0.0} / {breath.Capacity:0}");
            GUI.Label(new Rect(28, 188, 440, 24), $"Holding: {breath.IsHoldingBreath}   Forced recovery: {breath.IsForcedRecovery} ({breath.RecoveryRemaining:0.0}s)");
            GUI.Label(new Rect(28, 212, 440, 24), "E: enter / leave   |   Hold Left Ctrl: hold breath   |   F: light");
            GUI.Label(new Rect(28, 236, 440, 24), hiding.ExitBlocked ? "Exit blocked: clear the exit and press E again."
                : breath.ReleaseRequired ? "Release Left Ctrl before holding breath again."
                : "Noise: green = breathing; magenta = forced breath");
        }
    }
}
