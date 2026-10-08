using UnityEngine;

namespace HerFootsteps
{
    // Adapter observes the existing AI; it never writes AI state or requests a hunt.
    public sealed class CryptidComposureSource : MonoBehaviour
    {
        [SerializeField] private PlayerComposure player;
        [SerializeField] private CryptidBrain cryptid;
        [SerializeField] private bool proximityEnabled;
        [SerializeField] private bool pursuitEnabled;
        [SerializeField] private bool detectionEnabled;
        [SerializeField, Min(0)] private float proximityRange = 6;
        [SerializeField, Min(0)] private float proximityLossPerSecond = 2;
        [SerializeField, Min(0)] private float pursuitLossPerSecond = 4;
        [SerializeField, Min(0)] private float detectionLoss = 8;
        private bool previouslyDetected;
        public bool ProximityEnabled { get => proximityEnabled; set => proximityEnabled = value; }
        public bool PursuitEnabled { get => pursuitEnabled; set => pursuitEnabled = value; }
        public bool DetectionEnabled { get => detectionEnabled; set => detectionEnabled = value; }
        private void LateUpdate() => Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if (player == null) return;
            bool active = cryptid != null && cryptid.isActiveAndEnabled;
            bool detected = active && cryptid.VisuallyDetected;
            if (detectionEnabled && detected && !previouslyDetected) player.Apply(-detectionLoss, "Cryptid detection");
            previouslyDetected = detectionEnabled && detected;
            float rate = 0;
            string label = "";
            if (active && proximityEnabled && Vector3.Distance(player.transform.position, cryptid.transform.position) <= proximityRange)
            { rate += proximityLossPerSecond; label = "Cryptid proximity"; }
            if (active && pursuitEnabled && cryptid.State == CryptidState.Chase)
            { rate += pursuitLossPerSecond; label += " Pursuit"; }
            player.ReportSource(this, rate > 0 ? label + " -" + rate.ToString("0.0") + "/s" : null);
            if (rate > 0) player.Apply(-rate * Mathf.Max(0, deltaTime), label);
        }
        private void OnDisable() { previouslyDetected = false; if (player != null) player.ReportSource(this, null); }
    }
}
