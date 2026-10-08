using System;
using UnityEngine;

namespace HerFootsteps
{
    [CreateAssetMenu(menuName = "Her Footsteps/Composure Channel")]
    public sealed class ComposureChannel : ScriptableObject
    {
        public event Action<ComposureChange> Changed;
        public event Action<ComposureChange> StateChanged;
        // Future Hunt Controller may subscribe, but MUST apply its early-game safe-period gate.
        // Publishing this event neither requests a hunt nor changes the cryptid state machine.
        public event Action<ComposureChange> BrokenEntered;
        public void Publish(ComposureChange change)
        {
            Changed?.Invoke(change);
            if (change.PreviousState == change.State) return;
            StateChanged?.Invoke(change);
            if (change.State == ComposureState.Broken) BrokenEntered?.Invoke(change);
        }
    }
}
