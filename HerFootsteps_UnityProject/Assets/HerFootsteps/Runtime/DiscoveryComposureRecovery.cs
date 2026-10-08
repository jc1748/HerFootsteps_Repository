using UnityEngine;

namespace HerFootsteps
{
    // Optional bridge. Neither the inventory nor the composure resource depends on the other.
    public sealed class DiscoveryComposureRecovery : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private ItemDefinition meaningfulItem;
        [SerializeField, Min(0)] private float recovery = 20;
        private bool discovered;
        private void OnEnable() { if (inventory != null) inventory.ItemAdded += OnItemAdded; }
        private void OnDisable() { if (inventory != null) inventory.ItemAdded -= OnItemAdded; }
        private void OnItemAdded(ItemDefinition item, int quantity)
        {
            if (discovered || item != meaningfulItem || composure == null) return;
            discovered = true;
            composure.Apply(recovery, "First discovery: " + item.DisplayName);
        }
    }
}
