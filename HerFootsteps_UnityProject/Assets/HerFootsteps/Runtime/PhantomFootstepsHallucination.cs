using UnityEngine;

namespace HerFootsteps
{
    // Perceptual audio only. Intentionally has no NoiseChannel dependency.
    public sealed class PhantomFootstepsHallucination : HallucinationEvent
    {
        [SerializeField] private AudioSource footsteps;
        [SerializeField, Min(1)] private float nearbyDistance = 4.5f;
        [SerializeField, Min(0)] private float driftSpeed = 0.35f;
        private Vector3 direction;
        protected override bool ActivationCondition() => footsteps != null && footsteps.clip != null;
        protected override void OnStarted()
        {
            float side = Random.value < 0.5f ? -1 : 1;
            footsteps.transform.position = Player.position + Player.right * side * nearbyDistance + Player.forward * 2 + Vector3.up * 0.2f;
            direction = Player.forward; // Snapshot; audio does not follow player rotation/position.
            footsteps.Play();
        }
        protected override void OnActiveTick(float deltaTime) => footsteps.transform.position += direction * driftSpeed * deltaTime;
        protected override void OnFinished(HallucinationEnd reason) { if (footsteps != null) footsteps.Stop(); }
    }
}
