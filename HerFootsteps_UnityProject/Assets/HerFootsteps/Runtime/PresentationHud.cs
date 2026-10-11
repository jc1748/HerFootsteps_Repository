using UnityEngine;
using UnityEngine.UI;

namespace HerFootsteps
{
    public sealed class PresentationHud : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private PlayerBreath breath;
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private HotbarSlotView[] slots;
        [SerializeField] private Text prompt, selectedName, resources, notice;
        [SerializeField] private Image holdProgress, composurePulse;
        [SerializeField] private CanvasGroup noticeGroup;
        private int selected;
        private float noticeUntil, selectionUntil, pulse;
        private string lastStatus;
        public int Selected => selected;
        private void OnEnable()
        {
            if (inventory != null) inventory.ItemAdded += Collected;
            if (composure != null) composure.StateChanged += StateChanged;
        }
        private void OnDisable()
        {
            if (inventory != null) inventory.ItemAdded -= Collected;
            if (composure != null) composure.StateChanged -= StateChanged;
        }
        private void Start() => Notify("Her footprints lead into the trees.\nF light   ·   E interact   ·   1–5 select   ·   R use", 9);
        public void Select(int index) { if (index >= 0 && index < inventory.Slots.Count) { selected = index; selectionUntil = Time.unscaledTime + 4; } }
        public bool UseSelected() { bool used = inventory.TryUse(selected); Notify(used ? "Used " + lastSelectedName : "Cannot use this now. Item kept."); return used; }
        private string lastSelectedName = "item";
        private void Collected(ItemDefinition item, int count) => Notify("Collected " + item.DisplayName + (count > 1 ? " ×" + count : ""));
        private void StateChanged(ComposureChange change)
        {
            pulse = change.State > change.PreviousState ? 0.13f : 0.06f;
            Notify(change.State > change.PreviousState ? "The forest feels less familiar." : "For a moment, you feel steadier.");
        }
        public void Notify(string message, float seconds = 3) { if (notice != null) notice.text = message; noticeUntil = Time.unscaledTime + seconds; }
        private void Update()
        {
            if (inventory == null || input == null) return;
            if (input.SelectedSlotPressed >= 0) Select(input.SelectedSlotPressed);
            selected = Mathf.Clamp(selected, 0, inventory.Slots.Count - 1);
            var item = inventory.Slots[selected].Item;
            lastSelectedName = item != null ? item.DisplayName : "Empty slot";
            if (input.UseItemPressed) UseSelected();
            for (int i = 0; i < slots.Length; i++)
            { slots[i].gameObject.SetActive(i < inventory.Slots.Count); if (i < inventory.Slots.Count) slots[i].Refresh(inventory.Slots[i], i, i == selected); }
            selectedName.text = item != null ? lastSelectedName + (item.UseEffect != null ? "   [R]" : "") : (Time.unscaledTime < selectionUntil ? "Empty slot" : "");
            var target = interactor.Target;
            prompt.text = target != null ? (target.HoldDuration > 0 ? "Hold E  ·  " : "E  ·  ") + target.Prompt : "";
            holdProgress.fillAmount = interactor.HoldProgress;
            holdProgress.enabled = interactor.HoldProgress > 0;
            holdProgress.rectTransform.sizeDelta = new Vector2(180 * interactor.HoldProgress, 2);
            resources.text = "";
            if (flashlight.IsActive || flashlight.Battery < 20) resources.text += "LIGHT  " + Mathf.RoundToInt(flashlight.Battery) + "%   ";
            if (motor.Stamina < motor.StaminaCapacity - 1) resources.text += "STAMINA  " + Mathf.RoundToInt(motor.Stamina) + "%   ";
            if (breath.IsHoldingBreath || breath.IsForcedRecovery) resources.text += "BREATH  " + Mathf.RoundToInt(breath.Breath) + "%";
            if (inventory.Status != lastStatus)
            {
                lastStatus = inventory.Status;
                if (lastStatus.StartsWith("Cannot accept")) Notify("No room for this pickup.");
            }
            noticeGroup.alpha = Mathf.MoveTowards(noticeGroup.alpha, Time.unscaledTime < noticeUntil ? 1 : 0, Time.unscaledDeltaTime * 2);
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 0.09f);
            composurePulse.color = new Color(0.12f, 0.18f, 0.17f, pulse);
        }
    }
}
