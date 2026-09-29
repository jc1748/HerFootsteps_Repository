using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HerFootsteps.Tests
{
    public sealed class Milestone1Tests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        // Keep physics tests far from any open designer scene.
        private readonly Vector3 origin = new Vector3(5000, 5000, 5000);

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void SprintLastsFiveSecondsThenRequiresShiftRelease()
        {
            var stamina = new SprintStamina(100);
            Assert.That(stamina.Step(true, true, 5, 100, 20, 15), Is.EqualTo(1));
            Assert.That(stamina.Current, Is.Zero);
            Assert.That(stamina.Exhausted, Is.True);
            Assert.That(stamina.Step(true, true, 1, 100, 20, 15), Is.Zero);
            Assert.That(stamina.Current, Is.EqualTo(15));
            Assert.That(stamina.Step(true, true, 1, 100, 20, 15), Is.Zero);
            stamina.Step(false, true, 1, 100, 20, 15);
            Assert.That(stamina.Exhausted, Is.False);
            Assert.That(stamina.Step(true, true, 1, 100, 20, 15), Is.EqualTo(1));
        }

        [Test]
        public void IdleDoesNotDrainAndRecoveryIsCapped()
        {
            var stamina = new SprintStamina(100);
            stamina.Step(true, false, 10, 100, 20, 15);
            Assert.That(stamina.Current, Is.EqualTo(100));
            stamina.Step(true, true, 2, 100, 20, 15);
            stamina.Step(false, true, 20, 100, 20, 15);
            Assert.That(stamina.Current, Is.EqualTo(100));
        }

        [Test]
        public void PartialFinalSprintFrameCannotSpendMoreThanRemainingStamina()
        {
            var stamina = new SprintStamina(5);
            Assert.That(stamina.Step(true, true, 1, 100, 20, 15), Is.EqualTo(0.25f));
            Assert.That(stamina.Current, Is.Zero);
        }

        [Test]
        public void DiagonalMovementHasSameSpeedAsForwardMovement()
        {
            Floor();
            var motor = Motor();
            Vector3 start = motor.transform.position;
            for (int i = 0; i < 50; i++) motor.Simulate(Vector2.up, false, 0.02f);
            float forward = HorizontalDistance(start, motor.transform.position);
            Object.DestroyImmediate(motor.gameObject);
            motor = Motor();
            start = motor.transform.position;
            for (int i = 0; i < 50; i++) motor.Simulate(Vector2.one, false, 0.02f);
            Assert.That(HorizontalDistance(start, motor.transform.position), Is.EqualTo(forward).Within(0.03f));
            Assert.That(forward, Is.EqualTo(3).Within(0.03f));
        }

        [Test]
        public void SprintMovesFasterAndDrainsStamina()
        {
            Floor();
            var motor = Motor();
            Vector3 start = motor.transform.position;
            for (int i = 0; i < 50; i++) motor.Simulate(Vector2.up, true, 0.02f);
            Assert.That(HorizontalDistance(start, motor.transform.position), Is.EqualTo(6).Within(0.05f));
            Assert.That(motor.Stamina, Is.EqualTo(80).Within(0.05f));
        }

        [Test]
        public void CharacterControllerStopsAtSolidWall()
        {
            Floor();
            Cube("Wall", origin + new Vector3(0, 1, 2), new Vector3(5, 3, 0.5f));
            var motor = Motor();
            Physics.SyncTransforms();
            for (int i = 0; i < 100; i++) motor.Simulate(Vector2.up, false, 0.02f);
            Assert.That(motor.transform.position.z - origin.z, Is.InRange(1.35f, 1.55f));
        }

        [Test]
        public void MouseLookClampsPitchAndAccumulatesDisplacement()
        {
            var motor = Motor();
            var view = motor.transform.GetChild(0);
            motor.ApplyLook(new Vector2(100, 10000));
            Assert.That(Mathf.DeltaAngle(0, view.localEulerAngles.x), Is.EqualTo(-85).Within(0.01f));
            Assert.That(motor.transform.eulerAngles.y, Is.EqualTo(10).Within(0.01f));
            motor.ApplyLook(new Vector2(100, 0));
            Assert.That(motor.transform.eulerAngles.y, Is.EqualTo(20).Within(0.01f));
        }

        [Test]
        public void TapTogglesImmediatelyAndHoldingDoesNotRepeat()
        {
            var interactor = Interactor();
            var target = Cube("Target", origin + Vector3.forward * 2, Vector3.one).AddComponent<TestToggleInteractable>();
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.EqualTo(target));
            interactor.ProcessInteraction(true, true, 0.01f);
            Assert.That(target.IsOn, Is.True);
            for (int i = 0; i < 100; i++) interactor.ProcessInteraction(false, true, 0.02f);
            Assert.That(target.InteractionCount, Is.EqualTo(1));
            interactor.ProcessInteraction(false, false, 0.02f);
            interactor.ProcessInteraction(true, true, 0.02f);
            Assert.That(target.IsOn, Is.False);
        }

        [Test]
        public void InteractionRespectsRangeAndSolidOcclusion()
        {
            var interactor = Interactor();
            var target = Cube("Target", origin + Vector3.forward * 4, Vector3.one).AddComponent<TestToggleInteractable>();
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.Null);
            target.transform.position = origin + Vector3.forward * 2;
            var wall = Cube("Wall", origin + Vector3.forward, new Vector3(2, 2, 0.1f));
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.Null);
            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.EqualTo(target));
            target.enabled = false;
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.Null);
        }

        [Test]
        public void FutureHoldInteractionCancelsOnReleaseAndLostTarget()
        {
            var interactor = Interactor();
            var target = Cube("Hold stub", origin + Vector3.forward * 2, Vector3.one).AddComponent<HoldTestInteractable>();
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            interactor.ProcessInteraction(true, true, 0.3f);
            interactor.ProcessInteraction(false, false, 0.1f);
            interactor.ProcessInteraction(false, true, 2);
            Assert.That(target.Count, Is.Zero);
            interactor.ProcessInteraction(true, true, 0.3f);
            target.transform.position += Vector3.right * 10;
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            target.transform.position -= Vector3.right * 10;
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            interactor.ProcessInteraction(false, true, 2);
            Assert.That(target.Count, Is.Zero);
            interactor.ProcessInteraction(true, true, 0.5f);
            interactor.ProcessInteraction(false, true, 0.5f);
            interactor.ProcessInteraction(false, true, 2);
            Assert.That(target.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExistingActionsHaveImmediateInteractAndRequiredKeyboardBindings()
        {
            var original = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var actions = Object.Instantiate(original);
            var keyboard = InputSystem.AddDevice<Keyboard>("M1TestKeyboard");
            try
            {
                actions.devices = new InputDevice[] { keyboard };
                actions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
                var move = actions.FindAction("Player/Move", true);
                var sprint = actions.FindAction("Player/Sprint", true);
                var interact = actions.FindAction("Player/Interact", true);
                move.Enable(); sprint.Enable(); interact.Enable();
                Assert.That(interact.interactions, Is.Null.Or.Empty);
                Assert.That(move.controls, Does.Contain(keyboard.wKey));
                Assert.That(move.controls, Does.Contain(keyboard.aKey));
                Assert.That(move.controls, Does.Contain(keyboard.sKey));
                Assert.That(move.controls, Does.Contain(keyboard.dKey));
                Assert.That(sprint.controls, Does.Contain(keyboard.leftShiftKey));
                Assert.That(interact.controls, Does.Contain(keyboard.eKey));
                Assert.That(actions.FindAction("Player/Jump").enabled, Is.False);
                Assert.That(actions.FindAction("Player/Crouch").enabled, Is.False);
            }
            finally
            {
                actions.Disable();
                Object.DestroyImmediate(actions);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void SavedPlayerPrefabHasWiredReferencesAndNoMissingScripts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HerFootsteps/Prefabs/PrototypePlayer.prefab");
            Assert.That(prefab, Is.Not.Null);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            foreach (var type in new[] { typeof(PlayerInputReader), typeof(FirstPersonMotor), typeof(PlayerInteractor), typeof(PrototypeHud) })
            {
                var serialized = new SerializedObject(prefab.GetComponent(type));
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.That(property.objectReferenceValue, Is.Not.Null, type.Name + "." + property.name);
            }
        }

        private GameObject Empty(string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.position = position;
            objects.Add(item);
            return item;
        }

        private GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            var item = Empty(name, position);
            item.transform.localScale = scale;
            item.AddComponent<BoxCollider>();
            return item;
        }

        private void Floor() => Cube("Floor", origin - Vector3.up * 0.25f, new Vector3(100, 0.5f, 100));

        private FirstPersonMotor Motor()
        {
            var player = Empty("Motor", origin + Vector3.up * 0.05f);
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = 0.3f;
            capsule.center = new Vector3(0, 0.9f, 0); capsule.skinWidth = 0.03f;
            capsule.minMoveDistance = 0;
            var view = Empty("View", player.transform.position);
            view.transform.SetParent(player.transform);
            var motor = player.AddComponent<FirstPersonMotor>();
            motor.Configure(null, view.transform);
            Physics.SyncTransforms();
            for (int i = 0; i < 10; i++) motor.Simulate(Vector2.zero, false, 0.02f);
            return motor;
        }

        private PlayerInteractor Interactor()
        {
            var item = Empty("Interactor", origin);
            var interactor = item.AddComponent<PlayerInteractor>();
            interactor.Configure(null, item.AddComponent<Camera>());
            return interactor;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }
    }

    public sealed class HoldTestInteractable : Interactable
    {
        public int Count { get; private set; }
        public override float HoldDuration => 1;
        public override void Interact(PlayerInteractor interactor) => Count++;
    }
}
