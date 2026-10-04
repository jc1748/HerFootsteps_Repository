using UnityEngine;

namespace HerFootsteps
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        public readonly struct MovementStep
        {
            public readonly Vector3 Position;
            public readonly float HorizontalDistance, DeltaTime;
            public readonly bool Grounded, Sprinting;
            public MovementStep(Vector3 position, float distance, float deltaTime, bool grounded, bool sprinting)
            {
                Position = position; HorizontalDistance = distance; DeltaTime = deltaTime;
                Grounded = grounded; Sprinting = sprinting;
            }
        }

        public event System.Action<MovementStep> Moved;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform view;
        [Header("Provisional movement tuning")]
        [SerializeField, Min(0)] private float walkSpeed = 3f;
        [SerializeField, Min(0)] private float sprintSpeed = 6f;
        [SerializeField, Min(0)] private float mouseSensitivity = 0.1f;
        [SerializeField, Range(1, 89)] private float pitchLimit = 85f;
        [SerializeField] private float gravity = -20f;
        [Header("Provisional stamina tuning")]
        [SerializeField, Min(0.01f)] private float staminaCapacity = 100f;
        [SerializeField, Min(0)] private float staminaDrain = 20f;
        [SerializeField, Min(0)] private float staminaRecovery = 15f;
        private CharacterController controller;
        private SprintStamina stamina;
        private float verticalSpeed, pitch;

        public float Stamina => stamina?.Current ?? staminaCapacity;
        public float StaminaCapacity => staminaCapacity;
        public bool Exhausted => stamina != null && stamina.Exhausted;
        public bool IsSprinting { get; private set; }
        public bool MovementLocked { get; private set; }

        public void SetMovementLocked(bool locked)
        {
            MovementLocked = locked;
            verticalSpeed = 0;
            IsSprinting = false;
        }

        public void Teleport(Vector3 position)
        {
            Initialize();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
            verticalSpeed = 0;
        }

        public void Configure(PlayerInputReader source, Transform cameraTransform)
        {
            input = source;
            view = cameraTransform;
        }

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (stamina == null) stamina = new SprintStamina(staminaCapacity);
        }

        private void Update()
        {
            if (input == null || view == null) return;
            if (input.HasControl) ApplyLook(input.Look);
            Simulate(input.Move, input.Sprint, Time.deltaTime);
        }

        public void ApplyLook(Vector2 delta)
        {
            if (view == null) return;
            // Mouse delta is already displacement this frame: do not multiply by dt.
            transform.Rotate(0, delta.x * mouseSensitivity, 0);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -pitchLimit, pitchLimit);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public void Simulate(Vector2 movement, bool sprintRequested, float deltaTime)
        {
            Initialize();
            if (deltaTime <= 0 || !controller.enabled) return;
            if (MovementLocked)
            {
                stamina.Step(false, false, deltaTime, staminaCapacity, staminaDrain, staminaRecovery);
                IsSprinting = false;
                return;
            }
            movement = Vector2.ClampMagnitude(movement, 1);
            float fraction = stamina.Step(sprintRequested,
                movement.sqrMagnitude > 0.001f && controller.isGrounded,
                deltaTime, staminaCapacity, staminaDrain, staminaRecovery);
            IsSprinting = fraction > 0;
            float speed = Mathf.Lerp(walkSpeed, sprintSpeed, fraction);
            Vector3 direction = transform.right * movement.x + transform.forward * movement.y;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += gravity * deltaTime;
            Vector3 before = transform.position;
            var flags = controller.Move((direction * speed + Vector3.up * verticalSpeed) * deltaTime);
            if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -2;
            Vector3 displacement = transform.position - before;
            displacement.y = 0;
            Moved?.Invoke(new MovementStep(transform.position, displacement.magnitude, deltaTime,
                controller.isGrounded, IsSprinting));
        }
    }
}
