using UnityEngine;

namespace HerFootsteps
{
    // Future clues can override HoldDuration without changing input bindings.
    public abstract class Interactable : MonoBehaviour
    {
        public virtual string Prompt => "Interact";
        public virtual float HoldDuration => 0f;
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract void Interact(PlayerInteractor interactor);
    }
}
