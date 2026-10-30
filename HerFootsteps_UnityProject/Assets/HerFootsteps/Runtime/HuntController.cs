using System;
using UnityEngine;

namespace HerFootsteps
{
    public enum HuntState { SafePeriod, Exploration, Warning, Active, Cooldown, Failed }

    // Owns encounter timing, never the cryptid's internal state machine or player resources.
    [DefaultExecutionOrder(-50)]
    public sealed class HuntController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private CryptidBrain cryptid;
        [SerializeField] private NoiseChannel noise;
        [Tooltip("Disable when accumulated NoiseExposure owns noise-triggered requests. AI hearing remains unchanged.")]
        [SerializeField] private bool individualNoiseTriggers = true;
        [SerializeField] private HuntPlacement placement;
        [Header("Provisional encounter tuning")]
        [SerializeField, Min(0)] private float safePeriod = 60;
        [SerializeField, Min(0)] private float warningDuration = 4;
        [SerializeField, Min(0)] private float cooldownDuration = 30;
        [SerializeField, Min(0)] private float noiseThreshold = 0.8f;
        [SerializeField, Min(0)] private float captureDistance = 1.4f;
        [Tooltip("Optional search area for Broken. Without one, search starts at the cryptid, never at the player's hidden position.")]
        [SerializeField] private Transform brokenSearchOrigin;
        private CryptidSenses senses;
        private bool clockStarted;
        private Vector3 evidence;
        private bool spatialEvidence;
        public bool IndividualNoiseTriggers => individualNoiseTriggers;
        public HuntState State { get; private set; } = HuntState.SafePeriod;
        public float SafeRemaining { get; private set; }
        public float WarningRemaining { get; private set; }
        public float ActiveSeconds { get; private set; }
        public float CooldownRemaining { get; private set; }
        public string TriggerSource { get; private set; } = "None";
        public string LastGateResult { get; private set; } = "Waiting for player control";
        public Vector3 InitialEvidence => evidence;
        public CryptidBrain Cryptid => cryptid;
        public event Action<HuntState> StateChanged;

        private void OnEnable()
        {
            SafeRemaining = Mathf.Max(0, safePeriod); clockStarted = false;
            State = HuntState.SafePeriod;
            if (cryptid != null) { senses = cryptid.GetComponent<CryptidSenses>(); cryptid.enabled = false; }
            if (noise != null) noise.Emitted += OnNoise;
            if (composure != null) composure.BrokenEntered += OnBroken;
        }
        private void OnDisable()
        {
            if (noise != null) noise.Emitted -= OnNoise;
            if (composure != null) composure.BrokenEntered -= OnBroken;
            if (cryptid != null) cryptid.enabled = false;
        }
        private void Update() => Tick(Time.deltaTime, input != null && input.HasControl);
        public void Tick(float deltaTime, bool playerHasControl)
        {
            if (!isActiveAndEnabled || deltaTime <= 0) return;
            if (State == HuntState.SafePeriod)
            {
                if (!clockStarted && !playerHasControl) return;
                clockStarted = true;
                SafeRemaining = Mathf.Max(0, SafeRemaining - deltaTime);
                if (SafeRemaining <= 0) Transition(HuntState.Exploration);
                return; // Expiration never replays or generates a trigger.
            }
            if (State == HuntState.Warning)
            {
                WarningRemaining = Mathf.Max(0, WarningRemaining - deltaTime);
                if (WarningRemaining <= 0)
                {
                    if (placement != null && !placement.TryEnter(out var entry))
                    {
                        LastGateResult = "Hunt cancelled: " + placement.Status;
                        BeginCooldown(); return;
                    }
                    if (placement != null && !spatialEvidence) evidence = cryptid.transform.position;
                    cryptid.enabled = true;
                    cryptid.InvestigateEvidence(evidence, "Hunt: " + TriggerSource);
                    ActiveSeconds = 0;
                    Transition(HuntState.Active);
                }
            }
            else if (State == HuntState.Active)
            {
                ActiveSeconds += deltaTime;
                // TryObserve checks both head/body occlusion and existing hiding state.
                // Being hidden alone is not immunity; concealed players cannot be captured.
                if (CanCapture()) { cryptid.enabled = false; Transition(HuntState.Failed); }
                else if (cryptid.State == CryptidState.Idle || (placement != null && cryptid.State == CryptidState.Disengage))
                {
                    cryptid.enabled = false;
                    if (placement != null) placement.BeginReturn();
                    BeginCooldown();
                }
            }
            else if (State == HuntState.Cooldown)
            {
                CooldownRemaining = Mathf.Max(0, CooldownRemaining - deltaTime);
                if (placement != null) placement.TickReturn(deltaTime);
                if (CooldownRemaining <= 0 && (placement == null || !placement.Returning)) Transition(HuntState.Exploration);
            }
        }
        public bool CanCapture() => State == HuntState.Active && cryptid != null && composure != null &&
            cryptid.State == CryptidState.Chase &&
            Vector3.Distance(cryptid.transform.position, composure.transform.position) <= captureDistance &&
            senses != null && senses.TryObserve(out _);

        private void OnNoise(NoiseEvent sound)
        {
            if (!individualNoiseTriggers) return;
            // Only gameplay events attributable to this player can initiate an encounter.
            if (composure == null || sound.Intensity < noiseThreshold || senses == null || !senses.CanHear(sound, out _)) return;
            bool fromPlayer = sound.Instigator == composure.gameObject ||
                (sound.Source != null && sound.Source.transform.IsChildOf(composure.transform));
            if (fromPlayer) TryRequestHunt(sound.Position, "Noise: " + sound.Kind);
        }
        private void OnBroken(ComposureChange change)
        {
            Vector3 origin = brokenSearchOrigin != null ? brokenSearchOrigin.position : cryptid != null ? cryptid.transform.position : transform.position;
            TryRequestHunt(origin, "Broken composure", false);
        }
        public bool TryRequestHunt(Vector3 position, string source, bool isSpatialEvidence = true)
        {
            if (State != HuntState.Exploration) { LastGateResult = "Discarded " + source + " during " + State; return false; }
            if (cryptid == null || !cryptid.GetComponent<CryptidNavigation>().Ready)
            { LastGateResult = "Rejected: cryptid navigation unavailable"; return false; }
            if (placement != null && placement.Returning) { LastGateResult = "Discarded while returning home"; return false; }
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
            evidence = position; spatialEvidence = isSpatialEvidence; TriggerSource = source; LastGateResult = "Accepted";
            WarningRemaining = Mathf.Max(0, warningDuration);
            Transition(HuntState.Warning); return true;
        }
        private void BeginCooldown()
        {
            CooldownRemaining = Mathf.Max(0, cooldownDuration);
            Transition(HuntState.Cooldown);
        }
        private void Transition(HuntState next) { State = next; StateChanged?.Invoke(next); }
    }
}
