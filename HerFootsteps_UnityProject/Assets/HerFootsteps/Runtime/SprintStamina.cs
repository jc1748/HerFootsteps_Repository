using UnityEngine;

namespace HerFootsteps
{
    // Pure state: usable by the motor and testable without an environment/model.
    public sealed class SprintStamina
    {
        public float Current { get; private set; }
        public bool Exhausted { get; private set; }

        public SprintStamina(float capacity) => Current = Mathf.Max(0, capacity);

        // Returns the fraction of this timestep for which sprinting was affordable.
        public float Step(bool requested, bool moving, float deltaTime,
            float capacity, float drain, float recovery)
        {
            capacity = Mathf.Max(0.01f, capacity);
            Current = Mathf.Clamp(Current, 0, capacity);
            if (!requested) Exhausted = false;
            if (deltaTime <= 0) return 0;

            if (requested && moving && !Exhausted && Current > 0)
            {
                float cost = Mathf.Max(0, drain) * deltaTime;
                float fraction = cost > 0 ? Mathf.Min(1, Current / cost) : 1;
                Current = Mathf.Max(0, Current - cost);
                if (Current <= 0) Exhausted = true;
                return fraction;
            }

            Current = Mathf.Min(capacity, Current + Mathf.Max(0, recovery) * deltaTime);
            return 0;
        }
    }
}
