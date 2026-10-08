using UnityEngine;

namespace HerFootsteps
{
    public sealed class WildlifeHallucination : HallucinationEvent
    {
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private Light beam;
        [SerializeField] private Camera view;
        [SerializeField] private NoiseChannel noise;
        [SerializeField] private Transform reactionTarget;
        [SerializeField] private bool requireViewToStart = true;
        [SerializeField, Range(1, 180)] private float activationViewAngle = 40;
        [SerializeField, Min(0.1f)] private float reactionSeconds = 4;
        [SerializeField, Min(0)] private float requiredIlluminationSeconds = 0.4f;
        [SerializeField, Min(0)] private float screamIntensity = 1.2f;
        [SerializeField, Min(0)] private float screamRange = 18;
        [Header("Optional placeholder playback; hearing still uses NoiseChannel")]
        [SerializeField] private AudioClip failureClip;
        [SerializeField, Range(0, 1)] private float failureVolume = 0.35f;
        [SerializeField] private LayerMask occlusionLayers = Physics.DefaultRaycastLayers;
        private float reactionRemaining, litTime;
        public float ReactionRemaining => reactionRemaining;
        private Vector3 Target => reactionTarget != null ? reactionTarget.position : transform.position + Vector3.up;
        protected override bool ActivationCondition()
        {
            if (!requireViewToStart) return true;
            return view != null && Vector3.Angle(view.transform.forward, Target - view.transform.position) <= activationViewAngle && ClearLine(view.transform.position);
        }
        protected override void OnStarted() { reactionRemaining = Mathf.Max(0.1f, reactionSeconds); litTime = 0; }
        protected override void OnActiveTick(float deltaTime)
        {
            bool lit = flashlight != null && flashlight.IsActive && beam != null &&
                Vector3.Distance(beam.transform.position, Target) <= beam.range &&
                Vector3.Angle(beam.transform.forward, Target - beam.transform.position) <= beam.spotAngle * 0.5f && ClearLine(beam.transform.position);
            // Only credit illumination time remaining inside the reaction window.
            litTime = lit ? litTime + Mathf.Min(deltaTime, reactionRemaining) : 0;
            if (lit && litTime >= requiredIlluminationSeconds) { Finish(HallucinationEnd.Dismissed); return; }
            reactionRemaining -= deltaTime;
            if (reactionRemaining <= 0) Finish(HallucinationEnd.FailedReaction);
        }
        private bool ClearLine(Vector3 from)
        {
            Vector3 direction = Target - from;
            foreach (var hit in Physics.RaycastAll(from, direction.normalized, direction.magnitude, occlusionLayers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && (Player == null || !hit.transform.IsChildOf(Player))) return false;
            return true;
        }
        protected override void OnFinished(HallucinationEnd reason)
        {
            if ((reason == HallucinationEnd.FailedReaction || reason == HallucinationEnd.Expired) && Player != null)
            {
                if (noise != null) noise.Emit(NoiseKind.Scream, Player.position, screamIntensity, screamRange, gameObject, Player.gameObject);
                if (failureClip != null) AudioSource.PlayClipAtPoint(failureClip, Player.position + Vector3.up, failureVolume);
            }
        }
    }
}
