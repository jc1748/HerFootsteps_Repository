using UnityEngine;

namespace HerFootsteps
{
    public sealed class NoiseObstacle : Interactable
    {
        [SerializeField] private NoiseChannel channel;
        [SerializeField, Min(0)] private float intensity = 1;
        [SerializeField, Min(0)] private float range = 18;
        [SerializeField, Min(0)] private float cooldown = 2;
        [SerializeField, Min(0)] private float minimumSpeed = 0.1f;
        private float readyAt = float.NegativeInfinity;
        public override bool CanInteract => base.CanInteract && Time.time >= readyAt;
        public override string Prompt => "Crunch test debris";

        public void Configure(NoiseChannel noiseChannel) => channel = noiseChannel;

        private void OnEnable() => readyAt = float.NegativeInfinity;

        public override void Interact(PlayerInteractor interactor)
        {
            if (interactor != null) Emit(interactor.gameObject);
        }

        private void OnTriggerEnter(Collider other) => TryMovement(other);
        private void OnTriggerStay(Collider other) => TryMovement(other);

        private void TryMovement(Collider other)
        {
            var motor = other.GetComponentInParent<FirstPersonMotor>();
            if (motor == null) return;
            var controller = motor.GetComponent<CharacterController>();
            if (controller == null || !controller.isGrounded) return;
            Vector3 velocity = controller.velocity;
            velocity.y = 0;
            if (velocity.magnitude > minimumSpeed) Emit(motor.gameObject);
        }

        private void Emit(GameObject instigator)
        {
            if (!CanInteract || channel == null) return;
            readyAt = Time.time + cooldown;
            channel.Emit(NoiseKind.Environment, transform.position, intensity, range, gameObject, instigator);
        }
    }
}
