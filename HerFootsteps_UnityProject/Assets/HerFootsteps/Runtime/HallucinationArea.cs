using UnityEngine;
namespace HerFootsteps
{
    // Scene-level eligibility adapter; the existing event still owns composure and cooldown gates.
    public sealed class HallucinationArea : MonoBehaviour
    {
        [SerializeField] private PlayerComposure player;
        [SerializeField] private BoxCollider area;
        [SerializeField] private HallucinationEvent hallucination;
        private void Update()
        {
            if(player==null||area==null||hallucination==null||!area.enabled)return;
            if(new Bounds(area.center,area.size).Contains(area.transform.InverseTransformPoint(player.transform.position+Vector3.up*.9f)))hallucination.TryActivate();
        }
    }
}
