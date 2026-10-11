using System;
using UnityEngine;
using UnityEngine.Events;

namespace HerFootsteps
{
    // Discovery is independent of inventory capacity and future objective tracking.
    public sealed class ClueDiscovery : Interactable
    {
        [SerializeField] private string clueId = "sister-scarf";
        [SerializeField] private string title = "Her torn scarf";
        [SerializeField, Min(0.1f)] private float holdSeconds = 1.5f;
        [SerializeField, Min(0)] private float recovery = 20;
        [SerializeField] private UnityEvent onDiscovered = new UnityEvent();
        public event Action<ClueDiscovery, PlayerComposure> Discovered;
        public string ClueId => clueId;
        public bool IsDiscovered { get; private set; }
        public override bool CanInteract => base.CanInteract && !IsDiscovered;
        public override string Prompt => "Discover " + title;
        public override float HoldDuration => holdSeconds;
        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract || interactor == null) return;
            var player = interactor.GetComponent<PlayerComposure>();
            if (player == null) return;
            IsDiscovered = true;
            player.Apply(recovery, title);
            interactor.GetComponent<PresentationHud>()?.Notify("Her scarf. She came this way.", 5);
            onDiscovered.Invoke(); Discovered?.Invoke(this, player);
        }
    }
}
