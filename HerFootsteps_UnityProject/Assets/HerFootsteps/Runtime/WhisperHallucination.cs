using UnityEngine;

namespace HerFootsteps
{
    // Nonverbal, breath-like placeholder. No NoiseChannel or gameplay hearing emission.
    public sealed class WhisperHallucination : HallucinationEvent
    {
        [SerializeField] private AudioSource voice;
        protected override bool ActivationCondition() => voice != null && voice.clip != null;
        protected override void OnStarted()
        {
            voice.transform.position = Player.position - Player.forward * 1.5f + Player.right * 1.3f + Vector3.up * 1.5f;
            voice.Play();
        }
        protected override void OnFinished(HallucinationEnd reason) { if (voice != null) voice.Stop(); }
    }
}
