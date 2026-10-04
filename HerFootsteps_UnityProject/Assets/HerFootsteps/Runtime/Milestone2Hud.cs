using System.Collections.Generic;
using UnityEngine;

namespace HerFootsteps
{
    public sealed class Milestone2Hud : MonoBehaviour
    {
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private NoiseChannel channel;
        [SerializeField, Min(1)] private int historySize = 5;
        [SerializeField, Min(0.1f)] private float historySeconds = 10;
        [SerializeField] private bool drawRangeGizmos = true;
        private readonly List<NoiseEvent> recent = new List<NoiseEvent>();

        public void Configure(PlayerFlashlight playerLight, NoiseChannel noiseChannel)
        {
            if (channel != null) channel.Emitted -= OnNoise;
            flashlight = playerLight; channel = noiseChannel;
            if (isActiveAndEnabled && channel != null) channel.Emitted += OnNoise;
        }

        private void OnEnable()
        {
            recent.Clear();
            if (channel != null) channel.Emitted += OnNoise;
        }
        private void OnDisable() { if (channel != null) channel.Emitted -= OnNoise; }

        private void OnNoise(NoiseEvent noise)
        {
            recent.Insert(0, noise);
            while (recent.Count > Mathf.Max(1, historySize)) recent.RemoveAt(recent.Count - 1);
        }

        private void OnGUI()
        {
            if (flashlight == null) return;
            float top = Mathf.Max(140, Screen.height - 185);
            float width = Mathf.Min(560, Screen.width - 32);
            GUI.Box(new Rect(16, top, width, 173), "MILESTONE 2 - FLASHLIGHT / NOISE TEST");
            GUI.Label(new Rect(28, top + 25, width - 24, 24),
                $"F: flashlight {(flashlight.IsActive ? "ON" : "OFF")}   Battery: {flashlight.Battery:0.0} / {flashlight.Capacity:0}");
            GUI.Label(new Rect(28, top + 48, width - 24, 24), "E: battery pickup / debris   |   Noise events (newest first):");
            int row = 0;
            foreach (var noise in recent)
            {
                if (Time.time - noise.Time > historySeconds) continue;
                GUI.color = ColorFor(noise.Kind);
                GUI.Label(new Rect(28, top + 72 + row++ * 18, width - 24, 20),
                    $"{noise.Kind}: intensity {noise.Intensity:0.00} | range {noise.Range:0} m | {Time.time - noise.Time:0.0}s ago");
            }
            GUI.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !drawRangeGizmos) return;
            foreach (var noise in recent)
            {
                if (Time.time - noise.Time > historySeconds) continue;
                Gizmos.color = ColorFor(noise.Kind);
                Gizmos.DrawWireSphere(noise.Position, noise.Range);
            }
        }

        private static Color ColorFor(NoiseKind kind) => kind == NoiseKind.Walking
            ? Color.cyan : kind == NoiseKind.Sprinting ? Color.yellow
            : kind == NoiseKind.Breathing ? Color.green
            : kind == NoiseKind.ForcedBreath ? Color.magenta : new Color(1, 0.55f, 0.3f);
    }
}
