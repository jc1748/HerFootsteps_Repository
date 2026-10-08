using UnityEngine;

namespace HerFootsteps
{
    [CreateAssetMenu(menuName = "Her Footsteps/Item Effects/Composure Recovery")]
    public sealed class ComposureItemEffect : ItemUseEffect
    {
        [SerializeField, Min(0)] private float restoreAmount = 15;
        public override bool TryUse(GameObject owner)
        {
            var composure = owner.GetComponent<PlayerComposure>();
            return composure != null && composure.Apply(restoreAmount, "Inventory recovery resource") > 0;
        }
    }
}
