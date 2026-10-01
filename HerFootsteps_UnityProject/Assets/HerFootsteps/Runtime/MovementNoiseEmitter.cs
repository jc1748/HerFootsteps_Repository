using UnityEngine;

namespace HerFootsteps
{
    public sealed class MovementNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private NoiseChannel channel;
        [Header("Provisional walking noise")]
        [SerializeField, Min(0.01f)] private float walkStride = 1.5f;
        [SerializeField, Min(0)] private float walkIntensity = 0.2f;
        [SerializeField, Min(0)] private float walkRange = 4;
        [Header("Provisional sprint noise")]
        [SerializeField, Min(0.01f)] private float sprintStride = 2;
        [SerializeField, Min(0)] private float sprintIntensity = 0.6f;
        [SerializeField, Min(0)] private float sprintRange = 10;
        [Header("Emission guards")]
        [SerializeField, Min(0)] private float cooldown = 0.15f;
        [SerializeField, Min(0)] private float minimumSpeed = 0.1f;
        private float distance, elapsed;

        public void Configure(FirstPersonMotor source, NoiseChannel noiseChannel)
        {
            if (motor != null) motor.Moved -= OnMoved;
            motor = source; channel = noiseChannel;
            if (isActiveAndEnabled && motor != null) motor.Moved += OnMoved;
        }

        private void OnEnable()
        {
            distance = 0; elapsed = 0;
            if (motor != null) motor.Moved += OnMoved;
        }

        private void OnDisable() { if (motor != null) motor.Moved -= OnMoved; }

        private void OnMoved(FirstPersonMotor.MovementStep step)
        {
            elapsed += Mathf.Max(0, step.DeltaTime);
            if (!step.Grounded || step.DeltaTime <= 0 ||
                step.HorizontalDistance / step.DeltaTime < minimumSpeed) return;
            distance += step.HorizontalDistance;
            float stride = Mathf.Max(0.01f, step.Sprinting ? sprintStride : walkStride);
            if (distance < stride || elapsed < cooldown) return;
            distance %= stride;
            elapsed = 0;
            if (channel != null) channel.Emit(step.Sprinting ? NoiseKind.Sprinting : NoiseKind.Walking,
                step.Position, step.Sprinting ? sprintIntensity : walkIntensity,
                step.Sprinting ? sprintRange : walkRange, gameObject, gameObject);
        }
    }
}
