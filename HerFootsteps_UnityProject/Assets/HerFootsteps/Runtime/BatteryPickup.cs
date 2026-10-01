using UnityEngine;

namespace HerFootsteps
{
    public sealed class BatteryPickup : Interactable
    {
        [SerializeField, Min(0.01f)] private float restoreAmount = 40;
        public bool Consumed { get; private set; }
        public override bool CanInteract => base.CanInteract && !Consumed;
        public override string Prompt => $"Take battery (+{restoreAmount:0})";

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract || interactor == null) return;
            var flashlight = interactor.GetComponent<PlayerFlashlight>();
            if (flashlight == null || flashlight.RestoreBattery(restoreAmount) <= 0) return;
            // A full battery does not waste the pickup. Partial refills consume one pickup.
            Consumed = true;
            gameObject.SetActive(false);
        }
    }
}
