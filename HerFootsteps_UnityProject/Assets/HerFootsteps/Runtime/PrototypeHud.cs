using UnityEngine;

namespace HerFootsteps
{
    // Deliberately temporary IMGUI feedback for this testing milestone.
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerInputReader input;

        public void Configure(FirstPersonMotor player, PlayerInteractor interaction, PlayerInputReader controls)
        {
            motor = player;
            interactor = interaction;
            input = controls;
        }

        private void OnGUI()
        {
            if (motor == null || interactor == null || input == null) return;
            GUI.Box(new Rect(16, 16, 460, 114), "TEMPORARY PROTOTYPE TEST SPACE");
            GUI.Label(new Rect(28, 44, 440, 24), "WASD: move   |   Left Shift: sprint   |   E: interact");
            GUI.Label(new Rect(28, 68, 440, 24), "Esc: release cursor   |   Click: resume   |   No jump/crouch");
            GUI.Label(new Rect(28, 94, 440, 24),
                $"Stamina: {motor.Stamina:0} / {motor.StaminaCapacity:0}" +
                (motor.Exhausted ? "   Release Shift to sprint again" : ""));
            if (!input.HasControl)
            {
                GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 25, 300, 50),
                    "Click the Game view to resume");
                return;
            }
            GUI.Label(new Rect(Screen.width / 2f - 4, Screen.height / 2f - 12, 20, 24), "+");
            var target = interactor.Target;
            if (target == null) return;
            string prompt = target.HoldDuration > 0 ? "Hold E" : "E";
            GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 24, 300, 48),
                $"{prompt}: {target.Prompt}");
        }
    }
}
