using System;
using UnityEngine;

namespace HerFootsteps
{
    public enum NoiseKind { Walking, Sprinting, Environment }

    public readonly struct NoiseEvent
    {
        public readonly NoiseKind Kind;
        public readonly Vector3 Position;
        public readonly float Intensity, Range, Time;
        public readonly GameObject Source, Instigator;

        public NoiseEvent(NoiseKind kind, Vector3 position, float intensity, float range,
            float time, GameObject source, GameObject instigator)
        {
            Kind = kind; Position = position; Intensity = intensity; Range = range;
            Time = time; Source = source; Instigator = instigator;
        }
    }

    [CreateAssetMenu(menuName = "Her Footsteps/Noise Channel")]
    public sealed class NoiseChannel : ScriptableObject
    {
        // Explicit shared asset reference; no singleton, scene lookup, AI, or hunt rules.
        public event Action<NoiseEvent> Emitted;

        public void Emit(NoiseKind kind, Vector3 position, float intensity, float range,
            GameObject source, GameObject instigator = null)
        {
            if (!IsFinite(intensity) || !IsFinite(range) || intensity <= 0 || range <= 0 ||
                !IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) return;
            Emitted?.Invoke(new NoiseEvent(kind, position, intensity, range, UnityEngine.Time.time,
                source, instigator));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
