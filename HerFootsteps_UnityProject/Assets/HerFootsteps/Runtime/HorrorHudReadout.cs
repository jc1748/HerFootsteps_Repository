using UnityEngine;
using UnityEngine.UI;
namespace HerFootsteps
{
    public sealed class HorrorHudReadout : MonoBehaviour
    {
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private Text batteryText, composureText;
        private void Update()
        {
            if (flashlight != null && batteryText != null) batteryText.text = $"LIGHT  {Mathf.CeilToInt(flashlight.Battery / flashlight.Capacity * 100)}%";
            if (composure != null && composureText != null) composureText.text = $"COMPOSURE  {Mathf.RoundToInt(composure.Current / composure.Capacity * 100)}%";
        }
    }
}
