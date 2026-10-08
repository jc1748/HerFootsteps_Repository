using System;
using System.Collections.Generic;
using UnityEngine;

namespace HerFootsteps
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity;
        public ItemDefinition Item => item;
        public int Quantity => quantity;
        internal void Set(ItemDefinition definition, int count) { item = count > 0 ? definition : null; quantity = Mathf.Max(0, count); }
    }
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 5;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
        public event Action Changed;
        public event Action<ItemDefinition, int> ItemAdded;
        public string Status { get; private set; } = "Tab: inventory";
        public IReadOnlyList<InventorySlot> Slots { get { EnsureSlots(); return slots; } }
        public int Capacity => Mathf.Max(1, capacity);
        private void Awake() => EnsureSlots();
        private void EnsureSlots()
        {
            while (slots.Count < Capacity) slots.Add(new InventorySlot());
            // Lowering capacity at runtime never silently deletes items.
            while (slots.Count > Capacity && slots[slots.Count - 1].Item == null) slots.RemoveAt(slots.Count - 1);
        }
        public bool CanAccept(ItemDefinition item, int quantity)
        {
            EnsureSlots();
            if (item == null || quantity <= 0) return false;
            long available = 0;
            for (int i = 0; i < Capacity; i++)
            {
                var slot = slots[i];
                if (slot.Item == null) available += item.MaximumStack;
                else if (slot.Item == item) available += Mathf.Max(0, item.MaximumStack - slot.Quantity);
            }
            return available >= quantity;
        }
        public bool TryAdd(ItemDefinition item, int quantity = 1)
        {
            if (!CanAccept(item, quantity)) { Status = "Cannot accept pickup: inventory full or invalid item."; return false; }
            int remaining = quantity;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < Capacity && remaining > 0; i++)
                {
                    var slot = slots[i];
                    if (pass == 0 ? slot.Item != item : slot.Item != null) continue;
                    int added = Mathf.Min(remaining, Mathf.Max(0, item.MaximumStack - slot.Quantity));
                    slot.Set(item, slot.Quantity + added); remaining -= added;
                }
            Status = "Stored " + quantity + " x " + item.DisplayName;
            ItemAdded?.Invoke(item, quantity); Changed?.Invoke(); return true;
        }
        public bool TryUse(int index)
        {
            EnsureSlots();
            if (index < 0 || index >= slots.Count || slots[index].Item == null) return false;
            var slot = slots[index]; var item = slot.Item;
            if (item.UseEffect == null || !item.UseEffect.TryUse(gameObject))
            { Status = "Cannot use now (resource full or no use effect). Item kept."; return false; }
            if (item.Consumable) slot.Set(item, slot.Quantity - 1);
            Status = "Used " + item.DisplayName; Changed?.Invoke(); return true;
        }
        public bool TryRemove(int index, int quantity = 1)
        {
            EnsureSlots();
            if (index < 0 || index >= slots.Count || quantity <= 0 || slots[index].Item == null) return false;
            var slot = slots[index];
            if (!slot.Item.Removable) { Status = "Persistent narrative item cannot be discarded."; return false; }
            if (quantity > slot.Quantity) return false;
            string name = slot.Item.DisplayName; slot.Set(slot.Item, slot.Quantity - quantity);
            Status = "Removed " + name + " (no world drop)"; Changed?.Invoke(); return true;
        }
    }
}
