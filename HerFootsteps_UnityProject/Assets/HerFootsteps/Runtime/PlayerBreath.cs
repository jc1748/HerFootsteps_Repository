using UnityEngine;

namespace HerFootsteps
{
    public sealed class PlayerBreath : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private NoiseChannel channel;
        [Header("Provisional breath tuning")]
        [SerializeField, Min(0.01f)] private float breathCapacity = 100;
        [SerializeField, Min(0.01f)] private float depletionPerSecond = 20;
        [SerializeField, Min(0.01f)] private float recoveryPerSecond = 25;
        [SerializeField, Min(0.01f)] private float forcedRecoverySeconds = 3;
        [Header("Provisional gameplay noise (not audio volume)")]
        [SerializeField, Min(0.01f)] private float breathingInterval = 2;
        [SerializeField, Min(0)] private float breathingIntensity = 0.1f;
        [SerializeField, Min(0)] private float breathingRange = 2;
        [SerializeField, Min(0)] private float forcedIntensity = 0.8f;
        [SerializeField, Min(0)] private float forcedRange = 12;
        private float breath, cooldown, breathingTime;
        private bool initialized, releaseRequired;
        public float Capacity => Mathf.Max(0.01f, breathCapacity);
        public float Breath { get { Initialize(); return breath; } }
        public bool IsHoldingBreath { get; private set; }
        public bool IsForcedRecovery => cooldown > 0;
        public float RecoveryRemaining => cooldown;
        public bool ReleaseRequired => releaseRequired;

        public void Configure(PlayerInputReader controls, PlayerHiding player, NoiseChannel noiseChannel)
        {
            if (hiding != null) hiding.HiddenChanged -= OnHiddenChanged;
            input = controls; hiding = player; channel = noiseChannel;
            if (isActiveAndEnabled && hiding != null) hiding.HiddenChanged += OnHiddenChanged;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            breath = Capacity;
        }

        private void OnEnable()
        {
            Initialize();
            if (hiding != null) hiding.HiddenChanged += OnHiddenChanged;
        }
        private void OnDisable()
        {
            IsHoldingBreath = false;
            if (hiding != null) hiding.HiddenChanged -= OnHiddenChanged;
        }
        private void OnHiddenChanged(bool hidden)
        {
            IsHoldingBreath = false;
            breathingTime = 0;
        }
        private void Update() => Tick(input != null && input.HoldBreathHeld, Time.deltaTime);

        public void Tick(bool requested, float deltaTime)
        {
            Initialize();
            if (deltaTime <= 0) return;
            breath = Mathf.Clamp(breath, 0, Capacity);
            bool recovering = IsForcedRecovery;
            cooldown = Mathf.Max(0, cooldown - deltaTime);
            if (!requested) releaseRequired = false;
            IsHoldingBreath = hiding != null && hiding.IsHidden && requested &&
                !recovering && !releaseRequired && breath > 0;
            if (IsHoldingBreath)
            {
                breathingTime = 0;
                breath = Mathf.Max(0, breath - Mathf.Max(0.01f, depletionPerSecond) * deltaTime);
                if (breath > 0) return;
                IsHoldingBreath = false;
                cooldown = Mathf.Max(0.01f, forcedRecoverySeconds);
                releaseRequired = true;
                Emit(NoiseKind.ForcedBreath, forcedIntensity, forcedRange);
                return;
            }
            breath = Mathf.Min(Capacity, breath + Mathf.Max(0.01f, recoveryPerSecond) * deltaTime);
            if (hiding == null || !hiding.IsHidden) { breathingTime = 0; return; }
            breathingTime += deltaTime;
            if (breathingTime < Mathf.Max(0.01f, breathingInterval)) return;
            breathingTime %= Mathf.Max(0.01f, breathingInterval);
            Emit(NoiseKind.Breathing, breathingIntensity, breathingRange);
        }

        private void Emit(NoiseKind kind, float intensity, float range)
        {
            if (channel != null) channel.Emit(kind, transform.position, intensity, range, gameObject, gameObject);
        }
    }
}
