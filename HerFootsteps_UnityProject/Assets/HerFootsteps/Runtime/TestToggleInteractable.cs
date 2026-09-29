using UnityEngine;

namespace HerFootsteps
{
    // Test feedback only; no inventory, clues, progression, or gameplay audio.
    public sealed class TestToggleInteractable : Interactable
    {
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color offColor = new Color(0.85f, 0.35f, 0.12f);
        [SerializeField] private Color onColor = new Color(0.15f, 0.8f, 0.4f);
        private MaterialPropertyBlock properties;
        public bool IsOn { get; private set; }
        public int InteractionCount { get; private set; }
        public override string Prompt => IsOn ? "Switch OFF" : "Switch ON";

        public void Configure(Renderer target) => indicator = target;

        private void Start() => RefreshColor();

        public override void Interact(PlayerInteractor interactor)
        {
            IsOn = !IsOn;
            InteractionCount++;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (indicator == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", IsOn ? onColor : offColor);
            indicator.SetPropertyBlock(properties);
        }
    }
}
