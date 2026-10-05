using UnityEngine;

namespace HerFootsteps
{
    // Only this component reads live player transforms. The brain receives observations.
    public sealed class CryptidSenses : MonoBehaviour
    {
        [SerializeField] private Transform eyes;
        [SerializeField] private FirstPersonMotor player;
        [SerializeField] private Transform playerHead;
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private PlayerBreath breath;
        [SerializeField] private PlayerFlashlight flashlight;
        [Header("Provisional sight")]
        [SerializeField, Min(0)] private float sightRange = 12;
        [SerializeField, Range(1, 360)] private float fieldOfView = 100;
        [SerializeField] private Vector3 bodyOffset = new Vector3(0, 0.9f, 0);
        [SerializeField] private LayerMask occlusionLayers = Physics.DefaultRaycastLayers;
        [Header("Provisional hearing")]
        [SerializeField, Min(0)] private float hearingRangeMultiplier = 1;
        [SerializeField, Min(0)] private float maximumHearingRange = 20;
        [SerializeField, Min(0)] private float minimumNoiseIntensity = 0.05f;

        public bool PlayerHidden => hiding != null && hiding.IsHidden;
        public bool PlayerHoldingBreath => breath != null && breath.IsHoldingBreath;
        public bool PlayerLightActive => flashlight != null && flashlight.IsActive;
        public string SightReason { get; private set; } = "Not sampled";
        public Vector3 EyePosition => eyes != null ? eyes.position : transform.position + Vector3.up * 1.7f;
        public Vector3 EyeForward => eyes != null ? eyes.forward : transform.forward;
        public float SightRange => sightRange;
        public float FieldOfView => fieldOfView;
        public float MaximumHearingRange => maximumHearingRange;

        public void Configure(FirstPersonMotor target, Transform head, Transform eyeTransform)
        {
            player = target; playerHead = head; eyes = eyeTransform;
            hiding = target != null ? target.GetComponent<PlayerHiding>() : null;
            breath = target != null ? target.GetComponent<PlayerBreath>() : null;
            flashlight = target != null ? target.GetComponent<PlayerFlashlight>() : null;
        }

        public bool CanHear(NoiseEvent noise, out float effectiveRange)
        {
            effectiveRange = Mathf.Min(Mathf.Max(0, maximumHearingRange),
                Mathf.Max(0, noise.Range) * Mathf.Max(0, hearingRangeMultiplier));
            return noise.Intensity >= minimumNoiseIntensity && effectiveRange > 0 &&
                (noise.Position - transform.position).sqrMagnitude <= effectiveRange * effectiveRange;
        }

        public bool TryObserve(out Vector3 observedPosition)
        {
            observedPosition = default;
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                SightReason = "No active player";
                return false;
            }
            // Hiding is queried, but is not an invisibility flag. Actual cover must block
            // both samples. Light/breath are exposed for future rules, not used as sight cheats.
            bool hidden = PlayerHidden;
            Vector3 body = player.transform.TransformPoint(bodyOffset);
            Vector3 head = playerHead != null ? playerHead.position : body + Vector3.up * 0.75f;
            bool headInCone = InCone(head), bodyInCone = InCone(body);
            if (!headInCone && !bodyInCone)
            {
                SightReason = "Outside sight range / FOV";
                return false;
            }
            if ((headInCone && Unoccluded(head)) || (bodyInCone && Unoccluded(body)))
            {
                SightReason = hidden ? "Visible through exposed cover" : "Visible";
                observedPosition = player.transform.position;
                return true;
            }
            SightReason = hidden ? "Hidden and occluded by geometry" : "Occluded by geometry";
            return false;
        }

        private bool InCone(Vector3 point)
        {
            Vector3 direction = point - EyePosition;
            if (direction.sqrMagnitude > sightRange * sightRange) return false;
            return direction.sqrMagnitude < 0.0001f ||
                Vector3.Angle(EyeForward, direction) <= fieldOfView * 0.5f;
        }

        private bool Unoccluded(Vector3 point)
        {
            Vector3 direction = point - EyePosition;
            float distance = direction.magnitude;
            if (distance < 0.001f) return true;
            // Explicitly ignore our own visual/colliders and the target's colliders.
            // The existing player's Ignore Raycast layer does not break point-based sight.
            foreach (var hit in Physics.RaycastAll(EyePosition, direction / distance, distance,
                occlusionLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player.transform)) continue;
                return false;
            }
            return true;
        }
    }
}
