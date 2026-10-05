using UnityEngine;

namespace HerFootsteps
{
    [RequireComponent(typeof(CryptidBrain), typeof(CryptidSenses), typeof(CryptidNavigation))]
    public sealed class CryptidDebugView : MonoBehaviour
    {
        [SerializeField] private bool showHud = true;
        [SerializeField] private bool drawGizmos = true;
        private void OnGUI()
        {
            if (!showHud) return;
            var brain = GetComponent<CryptidBrain>();
            var senses = GetComponent<CryptidSenses>();
            var navigation = GetComponent<CryptidNavigation>();
            float width = Mathf.Min(610, Screen.width * 0.48f);
            float left = Screen.width - width - 16;
            GUI.Box(new Rect(left, 16, width, 280), "MILESTONE 4 - CRYPTID BEHAVIOR (NO HUNT / DEATH)");
            string noise = brain.HasNoise ? $"{brain.LastNoise.Kind}, {brain.LastEffectiveNoiseRange:0.0} m effective" : "None";
            string[] rows = {
                $"State: {brain.State}   |   Visible: {brain.VisuallyDetected}",
                brain.TransitionReason,
                $"Sight: {senses.SightReason}",
                $"Sight range: {senses.SightRange:0.0} m   FOV: {senses.FieldOfView:0} degrees",
                $"Last event: {noise}",
                brain.HearingReason,
                $"Investigation: {brain.InvestigationTarget:F1}",
                brain.HasLastKnownPosition ? $"Last known: {brain.LastKnownPosition:F1}" : "Last known: none (memory cleared)",
                $"Search: {brain.SearchRemaining:0.0} / {brain.SearchDuration:0.0} sec   |   Nav: {navigation.Status}",
                $"Player debug only: hidden {senses.PlayerHidden}, holding {senses.PlayerHoldingBreath}",
                $"Flashlight: {senses.PlayerLightActive} (no light detection rule)",
                "Scene Gizmos: yellow sound; red memory; cyan sight; blue search"
            };
            for (int i = 0; i < rows.Length; i++)
                GUI.Label(new Rect(left + 12, 42 + i * 20, width - 24, 20), rows[i]);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            var senses = GetComponent<CryptidSenses>();
            var brain = GetComponent<CryptidBrain>();
            if (senses == null || brain == null) return;
            Vector3 eye = senses.EyePosition;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eye, senses.SightRange);
            Gizmos.DrawRay(eye, Quaternion.AngleAxis(-senses.FieldOfView * 0.5f, Vector3.up) * senses.EyeForward * senses.SightRange);
            Gizmos.DrawRay(eye, Quaternion.AngleAxis(senses.FieldOfView * 0.5f, Vector3.up) * senses.EyeForward * senses.SightRange);
            if (!Application.isPlaying) return;
            if (brain.HasNoise)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(brain.LastNoise.Position, brain.LastEffectiveNoiseRange);
                Gizmos.DrawLine(transform.position, brain.LastNoise.Position);
            }
            if (brain.HasLastKnownPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(brain.LastKnownPosition, 0.35f);
                Gizmos.DrawLine(transform.position, brain.LastKnownPosition);
            }
            if (brain.State == CryptidState.Search)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(brain.SearchCenter, brain.SearchRadius);
            }
            var navigation = GetComponent<CryptidNavigation>();
            if (navigation != null && navigation.Ready && navigation.Agent.hasPath)
            {
                Gizmos.color = Color.white;
                var corners = navigation.Agent.path.corners;
                for (int i = 1; i < corners.Length; i++) Gizmos.DrawLine(corners[i - 1], corners[i]);
            }
        }
    }
}
