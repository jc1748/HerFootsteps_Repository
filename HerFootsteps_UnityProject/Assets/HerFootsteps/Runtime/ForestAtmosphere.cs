using UnityEngine;
using UnityEngine.Rendering;

namespace HerFootsteps
{
    [ExecuteAlways]
    public sealed class ForestAtmosphere : MonoBehaviour
    {
        [SerializeField] private Color fogColor = new Color(0.085f, 0.125f, 0.14f);
        [SerializeField, Range(0, 0.2f)] private float fogDensity = 0.045f;
        [SerializeField] private Color ambientColor = new Color(0.16f, 0.21f, 0.23f);
        [SerializeField] private Light moon;
        [SerializeField, Range(0, 2)] private float moonIntensity = 0.38f;
        private void OnEnable() => Apply();
        private void OnValidate() => Apply();
        public void Apply()
        {
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = ambientColor;
            RenderSettings.skybox = null;
            if (moon != null) moon.intensity = moonIntensity;
        }
    }
}
