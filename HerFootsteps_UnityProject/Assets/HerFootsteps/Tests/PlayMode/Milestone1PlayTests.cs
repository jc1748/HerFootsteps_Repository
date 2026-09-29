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
    public sealed class Milestone1PlayTests
    {
        [UnityTest]
        public IEnumerator KeyboardEventsDriveExistingActionsInPlayMode()
        {
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var actions = Object.Instantiate(source);
            var keyboard = InputSystem.AddDevice<Keyboard>("M1PlayKeyboard");
            var mouse = InputSystem.AddDevice<Mouse>("M1PlayMouse");
            var previousBackground = InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                actions.devices = new InputDevice[] { keyboard, mouse };
                actions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
                var move = actions.FindAction("Player/Move", true);
                var look = actions.FindAction("Player/Look", true);
                var sprint = actions.FindAction("Player/Sprint", true);
                var interact = actions.FindAction("Player/Interact", true);
                int performed = 0;
                interact.performed += _ => performed++;
                move.Enable(); look.Enable(); sprint.Enable(); interact.Enable();
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.E));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(10, -5) });
                InputSystem.Update();
                Assert.That(move.ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
                Assert.That(look.ReadValue<Vector2>(), Is.EqualTo(new Vector2(10, -5)));
                Assert.That(sprint.IsPressed(), Is.True);
                Assert.That(performed, Is.EqualTo(1), "E should perform immediately, without a hold timer.");
                InputSystem.Update();
                Assert.That(performed, Is.EqualTo(1), "Holding E must not repeat.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(move.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
                Assert.That(sprint.IsPressed(), Is.False);
            }
            finally
            {
                actions.Disable();
                Object.Destroy(actions);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }

        [UnityTest]
        public IEnumerator SavedSceneStartsWithWorkingPlayerAndTargets()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone1_Test.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var player = Object.FindFirstObjectByType<FirstPersonMotor>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<PlayerInputReader>().enabled, Is.True);
            Assert.That(player.Stamina, Is.EqualTo(100));
            var controller = player.GetComponent<CharacterController>();
            float settleDeadline = Time.realtimeSinceStartup + 1f;
            while (!controller.isGrounded && Time.realtimeSinceStartup < settleDeadline)
                yield return null;
            Assert.That(controller.isGrounded, Is.True, "Player should settle onto the blockout floor.");
            var targets = Object.FindObjectsByType<TestToggleInteractable>(FindObjectsSortMode.None);
            Assert.That(targets.Length, Is.EqualTo(2));
            targets[0].Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(targets[0].IsOn, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
