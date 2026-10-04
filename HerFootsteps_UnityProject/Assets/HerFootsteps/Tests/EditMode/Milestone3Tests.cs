using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HerFootsteps.Tests
{
    public sealed class Milestone3Tests
    {
        private readonly List<Object> owned = new List<Object>();
        private PlayerHiding hiding;
        private PlayerBreath breath;
        private HidingSpot spot;
        private List<NoiseEvent> noises;
        private readonly Vector3 origin = new Vector3(8000, 8000, 8000);

        [SetUp]
        public void Setup()
        {
            var player = Make("Player", origin);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.3f; controller.center = Vector3.up * 0.9f;
            player.AddComponent<FirstPersonMotor>();
            hiding = player.AddComponent<PlayerHiding>();
            breath = player.AddComponent<PlayerBreath>();
            var channel = ScriptableObject.CreateInstance<NoiseChannel>(); owned.Add(channel);
            noises = new List<NoiseEvent>(); channel.Emitted += noises.Add;
            breath.Configure(null, hiding, channel);
            player.AddComponent<MovementNoiseEmitter>().Configure(player.GetComponent<FirstPersonMotor>(), channel);
            spot = Make("Spot", origin + Vector3.forward * 3).AddComponent<HidingSpot>();
            spot.Configure(spot.transform, null);
            Physics.SyncTransforms();
        }
        [TearDown]
        public void Cleanup()
        {
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
            owned.Clear();
        }
        private GameObject Make(string name, Vector3 position)
        {
            var item = new GameObject(name); item.transform.position = position; owned.Add(item); return item;
        }

        [Test]
        public void HidingLocksMovementPreservesLookAndFlashlightAndReturnsToEntry()
        {
            var motor = hiding.GetComponent<FirstPersonMotor>();
            var view = Make("View", origin).transform; view.SetParent(motor.transform, false);
            motor.Configure(null, view);
            var light = hiding.gameObject.AddComponent<PlayerFlashlight>();
            light.Configure(null, hiding.gameObject.AddComponent<Light>()); light.Toggle();
            Assert.That(hiding.TryEnter(spot), Is.True);
            Assert.That(hiding.transform.position, Is.EqualTo(spot.transform.position));
            motor.Simulate(Vector2.one, true, 1);
            Assert.That(hiding.transform.position, Is.EqualTo(spot.transform.position));
            Assert.That(motor.IsSprinting, Is.False); Assert.That(noises, Is.Empty);
            motor.ApplyLook(new Vector2(100, 100));
            Assert.That(view.localRotation, Is.Not.EqualTo(Quaternion.identity));
            Assert.That(light.IsActive, Is.True); light.Toggle(); Assert.That(light.IsActive, Is.False);
            light.Toggle(); light.Tick(1); Assert.That(light.Battery, Is.EqualTo(98));
            Assert.That(hiding.TryLeave(), Is.True);
            Assert.That(hiding.transform.position, Is.EqualTo(origin));
            Assert.That(motor.MovementLocked, Is.False); Assert.That(spot.Occupant, Is.Null);
        }
        [Test]
        public void HoldingSuppressesBreathingAndReleaseRecovers()
        {
            breath.Tick(true, 1); Assert.That(breath.Breath, Is.EqualTo(100));
            Assert.That(breath.IsHoldingBreath, Is.False);
            hiding.TryEnter(spot);
            breath.Tick(false, 2); Assert.That(noises.Count, Is.EqualTo(1));
            Assert.That(noises[0].Kind, Is.EqualTo(NoiseKind.Breathing));
            noises.Clear(); breath.Tick(true, 2);
            Assert.That(breath.Breath, Is.EqualTo(60)); Assert.That(noises, Is.Empty);
            breath.Tick(false, 1); Assert.That(breath.Breath, Is.EqualTo(85));
            Assert.That(breath.IsHoldingBreath, Is.False);
        }
        [Test]
        public void ExhaustionEmitsOnceAndRequiresCooldownAndReleaseEvenAcrossExit()
        {
            hiding.TryEnter(spot); breath.Tick(true, 5);
            Assert.That(breath.Breath, Is.Zero); Assert.That(breath.IsForcedRecovery, Is.True);
            Assert.That(breath.IsHoldingBreath, Is.False); Assert.That(noises.Count, Is.EqualTo(1));
            Assert.That(noises[0].Kind, Is.EqualTo(NoiseKind.ForcedBreath));
            Assert.That(noises[0].Range, Is.EqualTo(12));
            Assert.That(noises[0].Instigator, Is.EqualTo(hiding.gameObject));
            hiding.TryLeave(); hiding.TryEnter(spot);
            breath.Tick(true, 3); breath.Tick(true, 1);
            Assert.That(breath.IsHoldingBreath, Is.False); Assert.That(breath.ReleaseRequired, Is.True);
            breath.Tick(false, 0.1f); breath.Tick(true, 0.1f);
            Assert.That(breath.IsHoldingBreath, Is.True);
            hiding.TryLeave(); Assert.That(breath.IsHoldingBreath, Is.False);
        }
        [Test]
        public void EarlyReleaseCannotBypassForcedRecovery()
        {
            hiding.TryEnter(spot); breath.Tick(true, 5); breath.Tick(false, 0.1f); breath.Tick(true, 1);
            Assert.That(breath.IsForcedRecovery, Is.True); Assert.That(breath.IsHoldingBreath, Is.False);
        }
        [Test]
        public void CleanupRestoresPlayerAndBlockedExitDoesNotTeleportIntoWall()
        {
            hiding.TryEnter(spot);
            var blocker = Make("Blocked return", origin + Vector3.up).AddComponent<BoxCollider>();
            blocker.size = Vector3.one * 2; Physics.SyncTransforms();
            Assert.That(hiding.TryLeave(), Is.False); Assert.That(hiding.ExitBlocked, Is.True);
            blocker.enabled = false; Physics.SyncTransforms();
            hiding.ForceLeave(); // Actual OnDisable callback is checked in Play Mode.
            Assert.That(hiding.IsHidden, Is.False);
            Assert.That(hiding.GetComponent<FirstPersonMotor>().MovementLocked, Is.False);
        }
        [Test]
        public void PrefabReferencesAndTemporaryBindingAreAssigned()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HerFootsteps/Prefabs/Milestone3Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Variant));
            foreach (string field in new[] { "input", "hiding", "channel" })
                Assert.That(new SerializedObject(prefab.GetComponent<PlayerBreath>()).FindProperty(field).objectReferenceValue, Is.Not.Null);
            Assert.That(new SerializedObject(prefab.GetComponent<PlayerInteractor>()).FindProperty("hiding").objectReferenceValue, Is.Not.Null);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.That(actions.FindAction("Player/HoldBreath", true).bindings[0].path, Is.EqualTo("<Keyboard>/leftCtrl"));
        }
    }
}
