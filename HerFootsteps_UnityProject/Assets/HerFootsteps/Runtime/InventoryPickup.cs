using UnityEngine;

namespace HerFootsteps
{
    public sealed class InventoryPickup : Interactable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        public bool Collected { get; private set; }
        public override bool CanInteract => base.CanInteract && !Collected && item != null;
        public override string Prompt => item != null ? "Store " + quantity + " x " + item.DisplayName : "Unassigned item";
        public void Configure(ItemDefinition definition, int count = 1) { item = definition; quantity = Mathf.Max(1, count); }
        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract || interactor == null) return;
            var inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null || !inventory.TryAdd(item, quantity)) return;
            Collected = true; gameObject.SetActive(false);
        }
    }
}
