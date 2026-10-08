using UnityEngine;

namespace HerFootsteps
{
    [DefaultExecutionOrder(-90)]
    public sealed class InventoryView : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Milestone5Hud debug;
        private int selected;
        private Vector2 scroll;
        public bool IsOpen { get; private set; }
        private void Update()
        {
            if (input == null) return;
            if (input.InventoryPressed) SetOpen(!IsOpen);
            else if (IsOpen && input.CancelPressed) SetOpen(false);
        }
        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (input != null) input.SetModalOpen(open);
        }
        private void OnDisable() { if (IsOpen) SetOpen(false); }
        private void OnGUI()
        {
            if (!IsOpen || inventory == null) return;
            GUI.depth = -1000;
            float width = Mathf.Min(820, Screen.width - 32);
            float height = Mathf.Min(790, Screen.height - 32);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("INVENTORY — Tab / Esc to close (world continues)");
            GUILayout.Label("Select a slot to inspect. Use consumes only after a successful effect.");
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var slot = inventory.Slots[i];
                string label = slot.Item == null ? "Empty" : slot.Item.DisplayName + " x" + slot.Quantity;
                if (GUILayout.Button((selected == i ? "> " : "") + (i + 1) + ": " + label)) selected = i;
            }
            selected = Mathf.Clamp(selected, 0, inventory.Slots.Count - 1);
            var item = inventory.Slots[selected].Item;
            if (item != null)
            {
                GUILayout.Label(item.DisplayName + (item.Consumable ? " (consumable)" : " (persistent / inspectable)"));
                GUILayout.Label(item.Description);
                GUILayout.BeginHorizontal();
                GUI.enabled = item.UseEffect != null;
                if (GUILayout.Button("Use one")) inventory.TryUse(selected);
                GUI.enabled = item.Removable;
                if (GUILayout.Button("Remove one (discard, no world drop)")) inventory.TryRemove(selected);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(inventory.Status);
            if (debug != null) debug.DrawControls();
            if (GUILayout.Button("Close inventory")) SetOpen(false);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
