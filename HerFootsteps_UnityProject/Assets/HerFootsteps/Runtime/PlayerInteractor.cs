using UnityEngine;

namespace HerFootsteps
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Camera view;
        [SerializeField] private PlayerHiding hiding;
        [SerializeField, Min(0.1f)] private float interactionRange = 2.5f;
        [Tooltip("Include blockers as well as interactables to prevent interaction through walls.")]
        [SerializeField] private LayerMask raycastLayers = Physics.DefaultRaycastLayers;
        private Interactable pending;
        private float holdTime;
        public Interactable Target { get; private set; }
        public float HoldProgress => pending != null && pending.HoldDuration > 0
            ? Mathf.Clamp01(holdTime / pending.HoldDuration) : 0;

        public void Configure(PlayerInputReader source, Camera camera)
        {
            input = source;
            view = camera;
        }

        private void LateUpdate()
        {
            if (input == null || !input.HasControl)
            {
                CancelInteraction();
                Target = null;
                return;
            }
            RefreshTarget();
            ProcessInteraction(input.InteractPressed, input.InteractHeld, Time.deltaTime);
        }

        public void RefreshTarget()
        {
            if (input != null && input.ModalOpen) { CancelInteraction(); Target = null; return; }
            Interactable next = null;
            if (hiding != null && hiding.IsHidden) next = hiding.ActiveSpot;
            else if (view != null && Physics.Raycast(view.transform.position, view.transform.forward,
                out var hit, interactionRange, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                next = hit.collider.GetComponentInParent<Interactable>();
                if (next != null && !next.CanInteract) next = null;
            }
            if (next != Target) CancelInteraction();
            Target = next;
        }

        public void ProcessInteraction(bool pressed, bool held, float deltaTime)
        {
            if (input != null && input.ModalOpen) { CancelInteraction(); Target = null; return; }
            if (Target == null || !Target.CanInteract)
            {
                CancelInteraction();
                return;
            }
            if (pressed)
            {
                if (Target.HoldDuration <= 0)
                {
                    Target.Interact(this);
                    return;
                }
                pending = Target;
                holdTime = 0;
            }
            if (!held) CancelInteraction();
            if (pending == null) return;
            holdTime += Mathf.Max(0, deltaTime);
            if (holdTime < pending.HoldDuration) return;
            var completed = pending;
            CancelInteraction();
            completed.Interact(this);
        }

        private void CancelInteraction()
        {
            pending = null;
            holdTime = 0;
        }

        private void OnDisable()
        {
            CancelInteraction();
            Target = null;
        }
    }
}
