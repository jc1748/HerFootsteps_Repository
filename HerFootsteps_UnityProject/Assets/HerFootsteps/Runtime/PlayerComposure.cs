using System;
using System.Collections.Generic;
using UnityEngine;

namespace HerFootsteps
{
    public enum ComposureState { High, Medium, Low, Broken }

    public readonly struct ComposureChange
    {
        public readonly PlayerComposure Player;
        public readonly float Previous, Current;
        public readonly ComposureState PreviousState, State;
        public readonly string Source;
        public ComposureChange(PlayerComposure player, float previous, float current,
            ComposureState previousState, ComposureState state, string source)
        { Player = player; Previous = previous; Current = current; PreviousState = previousState; State = state; Source = source; }
    }

    public sealed class PlayerComposure : MonoBehaviour
    {
        [SerializeField, Min(1)] private float capacity = 100;
        [SerializeField, Min(0)] private float startingValue = 100;
        [Header("Provisional lower bounds: High >= 70, Medium >= 35, Low > 0")]
        [SerializeField, Min(0)] private float highThreshold = 70;
        [SerializeField, Min(0)] private float mediumThreshold = 35;
        [SerializeField, Min(0)] private float brokenThreshold = 0;
        [SerializeField] private ComposureChannel channel;
        private float current;
        private bool initialized;
        private readonly Dictionary<UnityEngine.Object, string> sources = new Dictionary<UnityEngine.Object, string>();
        public event Action<ComposureChange> Changed;
        public event Action<ComposureChange> StateChanged;
        public event Action<ComposureChange> BrokenEntered;
        public float Capacity => Mathf.Max(1, capacity);
        public float Current { get { Initialize(); return current; } }
        public ComposureState State => Classify(Current);
        public bool IsBroken => State == ComposureState.Broken;
        public string LastSource { get; private set; } = "Initial state";
        public string LastTransition { get; private set; } = "None";
        public int BrokenEntryCount { get; private set; }
        public string ActiveSources => sources.Count == 0 ? "None" : string.Join("; ", sources.Values);
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true; current = Mathf.Clamp(startingValue, 0, Capacity);
        }
        public ComposureState Classify(float value)
        {
            float broken = Mathf.Clamp(brokenThreshold, 0, Capacity);
            float medium = Mathf.Clamp(mediumThreshold, broken, Capacity);
            float high = Mathf.Clamp(highThreshold, medium, Capacity);
            if (value <= broken) return ComposureState.Broken;
            if (value >= high) return ComposureState.High;
            return value >= medium ? ComposureState.Medium : ComposureState.Low;
        }
        // Reusable entry point for environment, items and future scripted/narrative events.
        // Returns the actual signed change so finite sources consume only what was applied.
        public float Apply(float amount, string source)
        {
            Initialize();
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount == 0) return 0;
            float previous = current;
            var before = Classify(previous);
            current = Mathf.Clamp(current + amount, 0, Capacity);
            if (Mathf.Approximately(previous, current)) return 0;
            LastSource = source;
            var change = new ComposureChange(this, previous, current, before, State, source);
            Changed?.Invoke(change);
            if (before != change.State)
            {
                LastTransition = before + " -> " + change.State + " (" + source + ")";
                StateChanged?.Invoke(change);
                if (change.State == ComposureState.Broken) { BrokenEntryCount++; BrokenEntered?.Invoke(change); }
            }
            channel?.Publish(change);
            return change.Current - change.Previous;
        }
        public void ReportSource(UnityEngine.Object owner, string label)
        {
            if (owner == null) return;
            if (string.IsNullOrEmpty(label)) sources.Remove(owner); else sources[owner] = label;
        }
    }
}
