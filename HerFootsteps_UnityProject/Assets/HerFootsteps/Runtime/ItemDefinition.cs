using UnityEngine;

namespace HerFootsteps
{
    [CreateAssetMenu(menuName = "Her Footsteps/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Test item";
        [SerializeField, TextArea] private string description = "Temporary inventory item.";
        [SerializeField, Min(1)] private int maximumStack = 1;
        [SerializeField] private bool consumable;
        [SerializeField] private bool removable = true;
        [SerializeField] private ItemUseEffect useEffect;
        public string DisplayName => displayName;
        public string Description => description;
        public int MaximumStack => Mathf.Max(1, maximumStack);
        public bool Consumable => consumable;
        public bool Removable => removable;
        public ItemUseEffect UseEffect => useEffect;
    }
}
