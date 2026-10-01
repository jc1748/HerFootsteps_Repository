using UnityEngine;

namespace HerFootsteps
{
    // Resource and switch state are independent of any flashlight mesh or animation.
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Light beam;
        [SerializeField, Min(0.01f)] private float batteryCapacity = 100;
        [SerializeField, Min(0)] private float drainPerSecond = 2;
        [SerializeField] private bool startsOn;
        private float battery;
        private bool initialized, switchedOn;

        public float Battery { get { Initialize(); return battery; } }
        public float Capacity => Mathf.Max(0.01f, batteryCapacity);
        public bool IsActive => isActiveAndEnabled && switchedOn && Battery > 0;

        public void Configure(PlayerInputReader controls, Light light)
        {
            input = controls;
            beam = light;
            SyncLight();
        }

        private void Awake() => Initialize();
        private void OnEnable() { Initialize(); SyncLight(); }
        private void OnDisable() { if (beam != null) beam.enabled = false; }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            battery = Capacity;
            switchedOn = startsOn;
        }

        private void Update()
        {
            if (input != null && input.FlashlightPressed) Toggle();
            Tick(Time.deltaTime);
        }

        public void Toggle()
        {
            Initialize();
            switchedOn = !switchedOn && battery > 0;
            SyncLight();
        }

        public void Tick(float deltaTime)
        {
            Initialize();
            battery = Mathf.Clamp(battery, 0, Capacity);
            if (IsActive) battery = Mathf.Max(0, battery - Mathf.Max(0, drainPerSecond) * Mathf.Max(0, deltaTime));
            if (battery <= 0) switchedOn = false;
            SyncLight();
        }

        public float RestoreBattery(float amount)
        {
            Initialize();
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return 0;
            float previous = battery;
            battery = Mathf.Clamp(battery + amount, 0, Capacity);
            SyncLight();
            return battery - previous;
        }

        private void SyncLight()
        {
            if (beam != null) beam.enabled = IsActive;
        }
    }
}
