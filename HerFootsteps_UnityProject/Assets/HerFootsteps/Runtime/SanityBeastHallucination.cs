using UnityEngine;
using UnityEngine.AI;

namespace HerFootsteps
{
    // A collider-free hallucination. Never feeds NoiseChannel or modifies the real cryptid.
    public sealed class SanityBeastHallucination : HallucinationEvent
    {
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private Light beam;
        [SerializeField] private AudioSource scream;
        [SerializeField, Min(.1f)] private float movementSpeed = 4.5f;
        [SerializeField, Min(.1f)] private float reactionWindow = 4;
        [SerializeField, Min(0)] private float requiredIllumination = .25f;
        [SerializeField, Min(.1f)] private float reachDistance = 1.5f;
        [SerializeField] private LayerMask occlusionLayers = Physics.DefaultRaycastLayers;
        private float reactionRemaining, illuminated;
        private NavMeshPath path;
        private float repath;
        public float ReactionRemaining => reactionRemaining;
        public bool Illuminated
        {
            get
            {
                var target = transform.position + Vector3.up * 1.1f;
                if (flashlight == null || !flashlight.IsActive || beam == null || !beam.isActiveAndEnabled || beam.intensity <= 0) return false;
                var delta = target - beam.transform.position;
                if (delta.magnitude > beam.range || Vector3.Angle(beam.transform.forward, delta) > beam.spotAngle * .5f) return false;
                foreach (var hit in Physics.RaycastAll(beam.transform.position, delta.normalized, delta.magnitude, occlusionLayers, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(Player)) return false;
                return true;
            }
        }
        protected override void OnStarted()
        {
            reactionRemaining = reactionWindow; illuminated = 0; repath = 0; path = new NavMeshPath();
        }
        protected override void OnActiveTick(float dt)
        {
            bool lit = Illuminated;
            illuminated = lit ? illuminated + Mathf.Min(dt, reactionRemaining) : 0;
            if (lit && illuminated >= requiredIllumination) { Finish(HallucinationEnd.Dismissed); return; }
            reactionRemaining -= dt;
            if (reactionRemaining <= 0 || Vector3.Distance(Player.position, transform.position) <= reachDistance)
            { Finish(HallucinationEnd.FailedReaction); return; }
            repath -= dt;
            if (repath <= 0)
            {
                repath = .25f;
                if (!NavMesh.SamplePosition(Player.position, out var destination, 2, NavMesh.AllAreas) ||
                    !NavMesh.CalculatePath(transform.position, destination.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                { Finish(HallucinationEnd.Disabled); return; }
            }
            var corners = path.corners;
            var next = corners.Length > 1 ? corners[1] : Player.position;
            var direction = next - transform.position; direction.y = 0;
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
            var candidate = Vector3.MoveTowards(transform.position, next, movementSpeed * dt);
            if (!NavMesh.Raycast(transform.position, candidate, out _, NavMesh.AllAreas) && NavMesh.SamplePosition(candidate, out var sample, .5f, NavMesh.AllAreas))
                transform.position = sample.position;
        }
        protected override void OnFinished(HallucinationEnd reason)
        {
            if ((reason == HallucinationEnd.FailedReaction || reason == HallucinationEnd.Expired) && scream != null)
            {
                scream.transform.position = Player.position + Vector3.up * 1.4f;
                scream.Play();
            }
        }
    }
}
