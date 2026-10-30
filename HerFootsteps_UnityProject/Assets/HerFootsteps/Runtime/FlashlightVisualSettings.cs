using UnityEngine;

namespace HerFootsteps
{
    // Visual beam tuning only; PlayerFlashlight remains the sole owner of charge and on/off state.
    [ExecuteAlways]
    public sealed class FlashlightVisualSettings : MonoBehaviour
    {
        [SerializeField] private Light beam;
        [SerializeField, Min(0)] private float intensity = 18;
        [SerializeField, Min(1)] private float range = 24;
        [SerializeField, Range(1,179)] private float outerAngle = 48;
        [SerializeField, Range(0,179)] private float innerAngle = 24;
        [SerializeField] private Color tint = new Color(1,.95f,.85f);
        [SerializeField] private LightShadows shadows = LightShadows.Soft;
        [SerializeField, Range(0,2)] private float shadowBias = .05f;
        [SerializeField, Range(0,2)] private float shadowNormalBias = .12f;
        private void OnEnable()=>Apply();
        private void OnValidate()=>Apply();
        public void Apply()
        {
            if(beam==null)return;
            beam.type=LightType.Spot;beam.intensity=intensity;beam.range=range;
            beam.spotAngle=outerAngle;beam.innerSpotAngle=Mathf.Min(innerAngle,outerAngle);
            beam.color=tint;beam.shadows=shadows;beam.shadowBias=shadowBias;beam.shadowNormalBias=shadowNormalBias;
            beam.shadowResolution=UnityEngine.Rendering.LightShadowResolution.High;
        }
    }
}
