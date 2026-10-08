using UnityEngine;

namespace HerFootsteps
{
    public sealed class ComposureInteractable : Interactable
    {
        [SerializeField] private string sourceName = "Frightening event";
        [SerializeField] private float amount = -25;
        [SerializeField, Min(1)] private int uses = 3;
        [SerializeField, Min(0)] private float cooldown = 1;
        private int used;
        private float readyAt;
        public override bool CanInteract => base.CanInteract && used < uses && Time.time >= readyAt;
        public override string Prompt => sourceName + " (" + amount.ToString("+0;-0") + ", " + (uses - used) + " left)";
        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract || interactor == null) return;
            var composure = interactor.GetComponent<PlayerComposure>();
            if (composure == null || composure.Apply(amount, sourceName) == 0) return;
            used++; readyAt = Time.time + cooldown;
        }
    }
}
