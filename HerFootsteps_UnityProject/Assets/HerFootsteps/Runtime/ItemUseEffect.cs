using UnityEngine;

namespace HerFootsteps
{
    public abstract class ItemUseEffect : ScriptableObject
    {
        // Return true only if the effect was applied. Inventory consumes after success.
        public abstract bool TryUse(GameObject owner);
    }
}
