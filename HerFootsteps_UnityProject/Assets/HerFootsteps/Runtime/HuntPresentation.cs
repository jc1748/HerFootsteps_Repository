using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace HerFootsteps
{
    public sealed class HuntPresentation : MonoBehaviour
    {
        [SerializeField] private HuntController hunt;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PresentationHud hud;
        [SerializeField] private AudioSource[] ambience;
        [SerializeField] private AudioSource warning;
        [SerializeField, Range(0, 1)] private float dangerousAmbienceMultiplier = 0.05f;
        [SerializeField, Min(0.1f)] private float fadeSeconds = 1.5f;
        private float[] originalVolumes;
        private float previousTimeScale;
        private bool frozen, restarting, previousAudioPause;
        private InputAction restart;
        private void OnEnable()
        {
            originalVolumes = new float[ambience.Length];
            for (int i = 0; i < ambience.Length; i++) if (ambience[i] != null) originalVolumes[i] = ambience[i].volume;
            hunt.StateChanged += Changed;
            restart = new InputAction("Restart after capture", InputActionType.Button, "<Keyboard>/enter"); restart.Enable();
        }
        private void Changed(HuntState state)
        {
            if (state == HuntState.Warning) { if (warning != null) warning.Play(); hud?.Notify("The forest falls silent.", 4); }
            if (state == HuntState.Active) if (warning != null) warning.Stop();
            if (state == HuntState.Cooldown) { if (warning != null) warning.Stop(); hud?.Notify("The sounds of the forest return."); }
            if (state == HuntState.Failed)
            {
                if (warning != null) warning.Stop(); input.SetModalOpen(true);
                previousTimeScale = Time.timeScale; Time.timeScale = 0; frozen = true;
                previousAudioPause = AudioListener.pause; AudioListener.pause = true;
            }
        }
        private void Update()
        {
            bool dangerous = hunt.State == HuntState.Warning || hunt.State == HuntState.Active || hunt.State == HuntState.Failed;
            for (int i = 0; i < ambience.Length; i++) if (ambience[i] != null)
                ambience[i].volume = Mathf.MoveTowards(ambience[i].volume, originalVolumes[i] * (dangerous ? dangerousAmbienceMultiplier : 1), Time.unscaledDeltaTime * originalVolumes[i] / fadeSeconds);
            if (hunt.State == HuntState.Failed && Application.isFocused && restart.WasPressedThisFrame()) Restart();
        }
        public void Restart()
        {
            if (hunt.State != HuntState.Failed || restarting) return;
            restarting = true;
            if (frozen) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; frozen = false; }
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
        }
        private void OnGUI()
        {
            if (hunt.State != HuntState.Failed) return;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            var r = new Rect(Screen.width / 2f - 180, Screen.height / 2f - 70, 360, 140);
            GUI.Box(r, "YOU WERE CAUGHT");
            GUI.Label(new Rect(r.x + 40, r.y + 40, 300, 30), "The forest keeps its secrets.");
            if (GUI.Button(new Rect(r.x + 60, r.y + 85, 240, 36), "Restart — Enter")) Restart();
        }
        private void OnDisable()
        {
            if (hunt != null) hunt.StateChanged -= Changed;
            restart?.Dispose(); if (warning != null) warning.Stop();
            for (int i = 0; originalVolumes != null && i < ambience.Length; i++) if (ambience[i] != null) ambience[i].volume = originalVolumes[i];
            if (frozen) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; frozen = false; }
        }
    }
}

