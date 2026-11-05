using System;
using UnityEngine;

namespace HerFootsteps
{
    public enum HallucinationEnd { Expired, Recovered, Dismissed, FailedReaction, Disabled }

    // Every descendant is explicitly a hallucination; it owns only its separate visual root.
    public abstract class HallucinationEvent : MonoBehaviour
    {
        [SerializeField] protected PlayerComposure composure;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private ComposureState minimumState = ComposureState.Low;
        [SerializeField, Min(0.1f)] private float duration = 8;
        [SerializeField, Min(0)] private float cooldown = 20;
        [SerializeField, Min(0)] private float activationRange = 12;
        [SerializeField] private bool automatic = true;
        [SerializeField] private HallucinationDirector director;
        private float remaining, cooldownRemaining;
        public bool IsHallucination => true;
        public bool IsActive { get; private set; }
        public bool Automatic { get => automatic; set => automatic = value; }
        public float Remaining => remaining;
        public float CooldownRemaining => cooldownRemaining;
        public string Status { get; private set; } = "Waiting for low composure";
        public ComposureState MinimumState => minimumState;
        public string BlockReason
        {
            get
            {
                if (!isActiveAndEnabled) return "Disabled / inactive hierarchy";
                if (IsActive) return "Active";
                if (composure == null) return "Missing composure reference";
                if (!StateAllows) return "Requires " + minimumState + " or worse";
                if (cooldownRemaining > 0) return $"Event cooldown {cooldownRemaining:F1}s";
                if (director != null && director.BlockReason(this) != null) return director.BlockReason(this);
                if (Vector3.Distance(Player.position, transform.position) > activationRange) return "Outside activation range";
                if (!ActivationCondition()) return "View / occlusion / audio condition";
                return null;
            }
        }
        public event Action<HallucinationEvent, HallucinationEnd> Ended;
        protected Transform Player => composure != null ? composure.transform : null;

        protected virtual void OnEnable()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
            if (composure != null) composure.StateChanged += OnComposureChanged;
        }
        protected virtual void OnDisable()
        {
            if (composure != null) composure.StateChanged -= OnComposureChanged;
            if (IsActive) Finish(HallucinationEnd.Disabled);
            else if (visualRoot != null) visualRoot.SetActive(false);
        }
        private bool StateAllows => composure != null && composure.State >= minimumState;
        private void OnComposureChanged(ComposureChange change)
        {
            if (IsActive && !StateAllows) Finish(HallucinationEnd.Recovered);
            else if (!IsActive && automatic) TryActivate();
        }
        public bool TryActivate()
        {
            string reason = BlockReason;
            if (reason != null) { Status = reason; return false; }
            IsActive = true; remaining = Mathf.Max(0.1f, duration); Status = "Active";
            if (visualRoot != null) visualRoot.SetActive(true);
            OnStarted(); return true;
        }
        protected virtual bool ActivationCondition() => true;
        protected virtual void OnStarted() { }
        protected virtual void OnActiveTick(float deltaTime) { }
        protected virtual void OnFinished(HallucinationEnd reason) { }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime <= 0) return;
            cooldownRemaining = Mathf.Max(0, cooldownRemaining - deltaTime);
            if (!IsActive) { if (automatic) TryActivate(); return; }
            if (!StateAllows) { Finish(HallucinationEnd.Recovered); return; }
            OnActiveTick(deltaTime);
            if (!IsActive) return;
            remaining -= deltaTime;
            if (remaining <= 0) Finish(HallucinationEnd.Expired);
        }
        protected void Finish(HallucinationEnd reason)
        {
            if (!IsActive) return;
            IsActive = false; remaining = 0; cooldownRemaining = Mathf.Max(0, cooldown);
            Status = reason.ToString();
            if (visualRoot != null) visualRoot.SetActive(false);
            OnFinished(reason); Ended?.Invoke(this, reason);
        }
    }
}
