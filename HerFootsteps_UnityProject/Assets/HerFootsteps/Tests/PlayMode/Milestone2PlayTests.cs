#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone2PlayTests
    {
        [UnityTest]
        public IEnumerator FBindingTogglesOncePerPress()
        {
            var asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            var keyboard = InputSystem.AddDevice<Keyboard>("M2Keyboard");
            var previousBackground = InputSystem.settings.backgroundBehavior;
            var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            var item = new GameObject("Flashlight input test");
            var light = item.AddComponent<Light>();
            var flashlight = item.AddComponent<PlayerFlashlight>();
            flashlight.Configure(null, light);
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                asset.devices = new InputDevice[] { keyboard };
                asset.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
                var action = asset.FindAction("Player/Flashlight", true);
                action.performed += _ => flashlight.Toggle();
                action.Enable();
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update();
                Assert.That(flashlight.IsActive, Is.True); Assert.That(light.enabled, Is.True);
                InputSystem.Update();
                Assert.That(flashlight.IsActive, Is.True, "Held F should not toggle repeatedly.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update();
                Assert.That(flashlight.IsActive, Is.False);
            }
            finally
            {
                asset.Disable(); Object.Destroy(asset); Object.Destroy(item);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
            }
        }

        [UnityTest]
        public IEnumerator SceneIntegratesLightPickupAndWalkThroughDebris()
        {
            const string scenePath = "Assets/Scenes/Milestone2_Test.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), Is.Not.Null,
                "Generate the scene with Her Footsteps > Milestone 2 > Create Test Scene (once) before running this test.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            motor.enabled = false; // deterministic manual movement through the real physics scene
            var controller = motor.GetComponent<CharacterController>();
            var flashlight = motor.GetComponent<PlayerFlashlight>();
            Assert.That(flashlight, Is.Not.Null);
            Assert.That(flashlight.IsActive, Is.False);
            flashlight.Toggle();
            float batteryBefore = flashlight.Battery;
            yield return new WaitForSeconds(0.15f);
            Assert.That(flashlight.Battery, Is.LessThan(batteryBefore));
            flashlight.Tick(20);
            float beforePickup = flashlight.Battery;
            var pickup = Object.FindAnyObjectByType<BatteryPickup>();
            pickup.Interact(motor.GetComponent<PlayerInteractor>());
            Assert.That(pickup.Consumed, Is.True);
            Assert.That(flashlight.Battery, Is.GreaterThan(beforePickup));

            var channel = AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            int environment = 0, walking = 0;
            System.Action<NoiseEvent> listener = noise =>
            {
                if (noise.Kind == NoiseKind.Environment) environment++;
                if (noise.Kind == NoiseKind.Walking) walking++;
            };
            channel.Emitted += listener;
            try
            {
                controller.enabled = false;
                motor.transform.position = new Vector3(1, 0.05f, -5);
                controller.enabled = true;
                Physics.SyncTransforms();
                for (int i = 0; i < 10; i++) { motor.Simulate(Vector2.zero, false, 0.02f); yield return new WaitForFixedUpdate(); }
                for (int i = 0; i < 100; i++) { motor.Simulate(Vector2.up, false, 0.02f); yield return new WaitForFixedUpdate(); }
                Assert.That(walking, Is.GreaterThan(0));
                Assert.That(environment, Is.GreaterThan(0), "Walking over the trigger must emit environmental noise.");
                int beforeIdle = environment;
                for (int i = 0; i < 110; i++) { motor.Simulate(Vector2.zero, false, 0.02f); yield return new WaitForFixedUpdate(); }
                Assert.That(environment, Is.EqualTo(beforeIdle), "Stationary player must not repeatedly trigger debris.");
            }
            finally { channel.Emitted -= listener; }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
