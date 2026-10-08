using UnityEngine;

namespace HerFootsteps
{
    [CreateAssetMenu(menuName = "Her Footsteps/Item Effects/Battery")]
    public sealed class BatteryItemEffect : ItemUseEffect
    {
        [SerializeField, Min(0)] private float restoreAmount = 40;
        public override bool TryUse(GameObject owner)
        {
            var light = owner.GetComponent<PlayerFlashlight>();
            return light != null && light.RestoreBattery(restoreAmount) > 0;
        }
    }
}
