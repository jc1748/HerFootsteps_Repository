using UnityEngine;

namespace HerFootsteps
{
    public enum CryptidState { Idle, Investigate, Search, Chase, Disengage }

    [RequireComponent(typeof(CryptidSenses), typeof(CryptidNavigation))]
    public sealed class CryptidBrain : MonoBehaviour
    {
        [SerializeField] private NoiseChannel channel;
        [Header("Provisional behavior tuning")]
        [SerializeField, Min(0)] private float investigateSpeed = 2.5f;
        [SerializeField, Min(0)] private float chaseSpeed = 4.5f;
        [SerializeField, Min(0)] private float searchSpeed = 1.8f;
        [SerializeField, Min(0)] private float returnSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float searchDuration = 8;
        [SerializeField, Min(0)] private float searchRadius = 3;
        [SerializeField, Min(0.1f)] private float searchPointSeconds = 2;
        [SerializeField, Min(0)] private float scanDegreesPerSecond = 90;
        [SerializeField, Min(0.1f)] private float travelTimeout = 15;
        [SerializeField, Min(0.02f)] private float chaseRepathInterval = 0.2f;
        [Header("Runtime debug (do not author state here)")]
        [SerializeField] private CryptidState state;
        [SerializeField] private Vector3 lastKnownPosition;
        [SerializeField] private Vector3 investigationTarget;
        [SerializeField] private bool hasLastKnownPosition;
        [SerializeField] private bool visuallyDetected;
        [SerializeField] private float searchRemaining;
        [SerializeField] private string transitionReason = "Waiting for sight or sound";
        private CryptidSenses senses;
        private CryptidNavigation navigation;
        private Vector3 homePosition, searchCenter;
        private float travelRemaining, pointRemaining, repathRemaining;
        private int searchPoint;
        private bool initialized;

        public CryptidState State => state;
        public bool VisuallyDetected => visuallyDetected;
        public bool HasLastKnownPosition => hasLastKnownPosition;
        public Vector3 LastKnownPosition => lastKnownPosition;
        public Vector3 InvestigationTarget => investigationTarget;
        public Vector3 SearchCenter => searchCenter;
        public float SearchRemaining => searchRemaining;
        public float SearchDuration => searchDuration;
        public float SearchRadius => searchRadius;
        public string TransitionReason => transitionReason;
        public NoiseEvent LastNoise { get; private set; }
        public bool HasNoise { get; private set; }
        public float LastEffectiveNoiseRange { get; private set; }
        public string HearingReason { get; private set; } = "No noise received";

        public void Configure(NoiseChannel noiseChannel)
        {
            if (channel != null) channel.Emitted -= OnNoise;
            channel = noiseChannel;
            if (isActiveAndEnabled && channel != null) channel.Emitted += OnNoise;
        }
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            senses = GetComponent<CryptidSenses>();
            navigation = GetComponent<CryptidNavigation>();
            homePosition = transform.position;
        }
        private void OnEnable()
        {
            Initialize();
            state = CryptidState.Idle;
            hasLastKnownPosition = false; visuallyDetected = false; HasNoise = false;
            searchRemaining = 0;
            transitionReason = "Waiting for sight or sound";
            if (channel != null) channel.Emitted += OnNoise;
        }
        private void OnDisable()
        {
            if (channel != null) channel.Emitted -= OnNoise;
            if (navigation != null) navigation.Stop();
        }

        private void OnNoise(NoiseEvent noise)
        {
            Initialize();
            LastNoise = noise; HasNoise = true;
            if (noise.Source != null && noise.Source.transform.IsChildOf(transform))
            { HearingReason = "Ignored own noise"; return; }
            bool heard = senses.CanHear(noise, out float range);
            LastEffectiveNoiseRange = range;
            if (!heard) { HearingReason = "Rejected: distance / intensity"; return; }
            // Sample current visibility rather than relying on last frame's cached flag.
            if (senses.TryObserve(out _))
            { HearingReason = "Audible; direct sight takes priority"; return; }
            HearingReason = "Accepted: investigate sound snapshot";
            lastKnownPosition = noise.Position;
            hasLastKnownPosition = true;
            BeginInvestigation(noise.Position, "Heard " + noise.Kind);
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            Initialize();
            if (deltaTime <= 0 || !isActiveAndEnabled) return;
            visuallyDetected = senses.TryObserve(out Vector3 observed);
            if (visuallyDetected)
            {
                lastKnownPosition = observed;
                hasLastKnownPosition = true;
                if (state != CryptidState.Chase)
                {
                    state = CryptidState.Chase;
                    transitionReason = "Confirmed visual contact";
                    repathRemaining = 0;
                    searchRemaining = 0;
                }
                repathRemaining -= deltaTime;
                if (repathRemaining <= 0)
                {
                    navigation.MoveTo(observed, chaseSpeed);
                    repathRemaining = Mathf.Max(0.02f, chaseRepathInterval);
                }
                return;
            }
            if (state == CryptidState.Chase)
                BeginInvestigation(lastKnownPosition, "Lost sight: go to last seen position");

            switch (state)
            {
                case CryptidState.Idle: break;
                case CryptidState.Investigate:
                    travelRemaining -= deltaTime;
                    if (!navigation.HasDestination || navigation.Arrived || travelRemaining <= 0)
                        BeginSearch(travelRemaining <= 0 ? "Travel timed out: search locally"
                            : !navigation.HasDestination ? "Search fallback: " + navigation.Status
                            : "Reached information location / reachable edge");
                    break;
                case CryptidState.Search:
                    searchRemaining = Mathf.Max(0, searchRemaining - deltaTime);
                    if (searchRemaining <= 0) { BeginDisengage(); break; }
                    pointRemaining -= deltaTime;
                    if (pointRemaining <= 0) NextSearchPoint();
                    if (!navigation.HasDestination || navigation.Arrived)
                    {
                        navigation.Stop();
                        transform.Rotate(0, scanDegreesPerSecond * deltaTime, 0);
                    }
                    break;
                case CryptidState.Disengage:
                    travelRemaining -= deltaTime;
                    if (!navigation.HasDestination || navigation.Arrived || travelRemaining <= 0)
                    {
                        navigation.Stop(); state = CryptidState.Idle;
                        hasLastKnownPosition = false;
                        transitionReason = "Disengaged; waiting for new evidence";
                    }
                    break;
            }
        }

        private void BeginInvestigation(Vector3 position, string reason)
        {
            investigationTarget = position;
            state = CryptidState.Investigate;
            transitionReason = reason;
            searchRemaining = 0;
            travelRemaining = Mathf.Max(0.1f, travelTimeout);
            navigation.MoveTo(position, investigateSpeed);
        }
        private void BeginSearch(string reason)
        {
            // The center is evidence, never the hidden player's live transform.
            searchCenter = investigationTarget;
            state = CryptidState.Search;
            transitionReason = reason;
            searchRemaining = Mathf.Max(0.1f, searchDuration);
            searchPoint = 0;
            navigation.Stop();
            pointRemaining = Mathf.Max(0.1f, searchPointSeconds);
        }
        private void NextSearchPoint()
        {
            // Golden-angle samples avoid requiring authored patrol/search nodes.
            float angle = searchPoint++ * 137.5f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * searchRadius;
            navigation.MoveTo(searchCenter + offset, searchSpeed);
            pointRemaining = Mathf.Max(0.1f, searchPointSeconds);
        }
        private void BeginDisengage()
        {
            state = CryptidState.Disengage;
            transitionReason = "Search expired: return to initial position";
            travelRemaining = Mathf.Max(0.1f, travelTimeout);
            navigation.MoveTo(homePosition, returnSpeed);
        }
    }
}
