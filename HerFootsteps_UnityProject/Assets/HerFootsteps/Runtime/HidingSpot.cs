using UnityEngine;

namespace HerFootsteps
{
    // Cover is only a location/state. It does not grant invisibility or decide detection.
    public sealed class HidingSpot : Interactable
    {
        [Tooltip("Player root/feet position, not camera position. Keep the capsule clear of solid cover.")]
        [SerializeField] private Transform hidingPosition;
        [Tooltip("Optional exit position; otherwise return to the position used to enter.")]
        [SerializeField] private Transform exitPosition;
        public PlayerHiding Occupant { get; private set; }
        public Transform HidingPosition => hidingPosition;
        public Transform ExitPosition => exitPosition;
        public override bool CanInteract => base.CanInteract && hidingPosition != null;
        public override string Prompt => Occupant != null ? "Leave hiding spot" : "Enter hiding spot";

        public void Configure(Transform hidden, Transform exit)
        {
            hidingPosition = hidden;
            exitPosition = exit;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract || interactor == null) return;
            var player = interactor.GetComponent<PlayerHiding>();
            if (player == null) return;
            if (Occupant == player) player.TryLeave();
            else player.TryEnter(this);
        }

        internal bool Claim(PlayerHiding player)
        {
            if (!CanInteract || Occupant != null) return false;
            Occupant = player;
            return true;
        }

        internal void Release(PlayerHiding player)
        {
            if (Occupant == player) Occupant = null;
        }

        private void OnDisable()
        {
            if (Occupant != null) Occupant.ForceLeave();
        }
    }
}
