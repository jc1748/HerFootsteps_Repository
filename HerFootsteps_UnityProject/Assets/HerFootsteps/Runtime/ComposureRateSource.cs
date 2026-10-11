using UnityEngine;

namespace HerFootsteps
{
    // Optional volume + signed rate supports dangerous environments, safe areas and scripted modifiers.
    public sealed class ComposureRateSource : MonoBehaviour
    {
        [SerializeField] private PlayerComposure player;
        [SerializeField] private BoxCollider area;
        [Tooltip("Height above the player's root sampled by areas; avoids floor-contact skin offsets.")]
        [SerializeField, Min(0)] private float playerSampleHeight = 0.9f;
        [SerializeField] private string sourceName = "Dangerous environment";
        [SerializeField] private bool sourceActive = true;
        [SerializeField] private float ratePerSecond = -3;
        [SerializeField, Min(0)] private float recoveryCeiling = 75;
        [Tooltip("Negative means unlimited. Positive recovery budgets do not refill on re-entry or re-enable.")]
        [SerializeField] private float totalBudget = -1;
        private float spent;
        [Tooltip("Optional safe volumes that suspend this source only, even after recovery is exhausted.")]
        [SerializeField] private BoxCollider[] suppressInside = new BoxCollider[0];
        public bool SourceActive { get => sourceActive; set => sourceActive = value; }
        public float RemainingBudget => totalBudget < 0 ? float.PositiveInfinity : Mathf.Max(0, totalBudget - spent);
        public bool IsApplying { get; private set; }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if (player == null) return;
            bool inside = area == null || (area.enabled && area.gameObject.activeInHierarchy &&
                new Bounds(area.center, area.size).Contains(area.transform.InverseTransformPoint(player.transform.position + Vector3.up * playerSampleHeight)));
            foreach (var safe in suppressInside)
                if (safe != null && safe.enabled && safe.gameObject.activeInHierarchy &&
                    new Bounds(safe.center, safe.size).Contains(safe.transform.InverseTransformPoint(player.transform.position + Vector3.up * playerSampleHeight))) inside = false;
            IsApplying = isActiveAndEnabled && sourceActive && inside && RemainingBudget > 0 && deltaTime > 0;
            float amount = IsApplying ? ratePerSecond * deltaTime : 0;
            if (amount > 0) amount = Mathf.Min(amount, Mathf.Max(0, Mathf.Min(player.Capacity, recoveryCeiling) - player.Current));
            amount = Mathf.Sign(amount) * Mathf.Min(Mathf.Abs(amount), RemainingBudget);
            IsApplying = amount != 0;
            player.ReportSource(this, IsApplying ? sourceName + " " + ratePerSecond.ToString("+0.0;-0.0") + "/s" : null);
            if (IsApplying) spent += Mathf.Abs(player.Apply(amount, sourceName));
        }
        private void OnDisable() { IsApplying = false; if (player != null) player.ReportSource(this, null); }
    }
}
