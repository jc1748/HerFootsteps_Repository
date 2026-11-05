using UnityEngine;

namespace HerFootsteps
{
    [RequireComponent(typeof(Light))]
    public sealed class CryptidRedLight : MonoBehaviour
    {
        [SerializeField] private HuntController hunt;
        [SerializeField] private Color color = new Color(1, .035f, .015f);
        [SerializeField, Min(0)] private float intensity = 1.2f;
        [SerializeField, Min(0)] private float range = 4.5f;
        private Light source;
        private void LateUpdate()
        {
            if (source == null) source = GetComponent<Light>();
            source.type = LightType.Point; source.color = color; source.intensity = intensity; source.range = range;
            source.enabled = hunt != null && hunt.State == HuntState.Active;
        }
        private void OnDisable() { if (source != null) source.enabled = false; }
    }
}
