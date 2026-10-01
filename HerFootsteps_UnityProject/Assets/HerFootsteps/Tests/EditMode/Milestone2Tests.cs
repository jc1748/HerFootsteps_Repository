using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HerFootsteps.Tests
{
    public sealed class Milestone2Tests
    {
        private readonly List<Object> owned = new List<Object>();
        private readonly Vector3 origin = new Vector3(6000, 6000, 6000);

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
            owned.Clear();
        }

        [Test]
        public void FlashlightDrainsOnlyWhileActiveAndStopsAtEmpty()
        {
            var flashlight = Flashlight();
            flashlight.Tick(10);
            Assert.That(flashlight.Battery, Is.EqualTo(100));
            flashlight.Toggle();
            Assert.That(flashlight.IsActive, Is.True);
            Assert.That(flashlight.GetComponent<Light>().enabled, Is.True);
            flashlight.Tick(10);
            Assert.That(flashlight.Battery, Is.EqualTo(80));
            flashlight.Toggle();
            flashlight.Tick(100);
            Assert.That(flashlight.Battery, Is.EqualTo(80));
            flashlight.Toggle();
            flashlight.Tick(100);
            Assert.That(flashlight.Battery, Is.Zero);
            Assert.That(flashlight.IsActive, Is.False);
            Assert.That(flashlight.GetComponent<Light>().enabled, Is.False);
            flashlight.Toggle();
            Assert.That(flashlight.IsActive, Is.False);
        }

        [Test]
        public void BatteryRefillClampsAndDoesNotAutomaticallySwitchOn()
        {
            var flashlight = Flashlight();
            flashlight.Toggle(); flashlight.Tick(100);
            Assert.That(flashlight.RestoreBattery(40), Is.EqualTo(40));
            Assert.That(flashlight.IsActive, Is.False);
            Assert.That(flashlight.RestoreBattery(500), Is.EqualTo(60));
            Assert.That(flashlight.Battery, Is.EqualTo(100));
            Assert.That(flashlight.RestoreBattery(-10), Is.Zero);
            Assert.That(flashlight.RestoreBattery(float.NaN), Is.Zero);
            flashlight.Toggle();
            Assert.That(flashlight.IsActive, Is.True);
        }

        [Test]
        public void PickupUsesExistingInteractionAndCannotBeConsumedTwice()
        {
            var flashlight = Flashlight();
            var interactor = flashlight.gameObject.AddComponent<PlayerInteractor>();
            var camera = flashlight.gameObject.AddComponent<Camera>();
            interactor.Configure(null, camera);
            var target = Cube("Battery", origin + Vector3.forward * 2, Vector3.one).AddComponent<BatteryPickup>();
            Physics.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.Target, Is.EqualTo(target));
            interactor.ProcessInteraction(true, true, 0.02f);
            Assert.That(target.Consumed, Is.False, "Full battery should leave pickup available.");
            flashlight.Toggle(); flashlight.Tick(30);
            interactor.ProcessInteraction(true, true, 0.02f);
            Assert.That(flashlight.Battery, Is.EqualTo(80));
            Assert.That(target.Consumed, Is.True);
            Assert.That(target.gameObject.activeSelf, Is.False);
            target.Interact(interactor);
            Assert.That(flashlight.Battery, Is.EqualTo(80));
        }

        [Test]
        public void NoiseChannelDeliversSpatialSourceDataAndAllowsUnsubscribe()
        {
            var channel = Channel();
            var source = Empty("Source", origin);
            NoiseEvent last = default;
            int first = 0, second = 0;
            System.Action<NoiseEvent> subscriber = noise => { last = noise; first++; };
            channel.Emitted += subscriber;
            channel.Emitted += _ => second++;
            channel.Emit(NoiseKind.Environment, origin, 1, 18, source, source);
            Assert.That(first, Is.EqualTo(1)); Assert.That(second, Is.EqualTo(1));
            Assert.That(last.Position, Is.EqualTo(origin));
            Assert.That(last.Source, Is.EqualTo(source)); Assert.That(last.Instigator, Is.EqualTo(source));
            Assert.That(last.Intensity, Is.EqualTo(1)); Assert.That(last.Range, Is.EqualTo(18));
            channel.Emitted -= subscriber;
            channel.Emit(NoiseKind.Walking, origin, 0.2f, 4, source);
            Assert.That(first, Is.EqualTo(1)); Assert.That(second, Is.EqualTo(2));
            channel.Emit(NoiseKind.Walking, origin, float.NaN, 4, source);
            channel.Emit(NoiseKind.Walking, origin, 1, -1, source);
            Assert.That(second, Is.EqualTo(2));
        }

        [Test]
        public void RealMovementEmitsDistinctWalkingAndSprintNoise()
        {
            var channel = Channel();
            var events = new List<NoiseEvent>();
            channel.Emitted += events.Add;
            var motor = Motor(channel);
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, false, 0.02f);
            Assert.That(events.Count, Is.InRange(2, 3));
            Assert.That(events[0].Kind, Is.EqualTo(NoiseKind.Walking));
            Assert.That(events[0].Range, Is.EqualTo(4));
            events.Clear();
            for (int i = 0; i < 60; i++) motor.Simulate(Vector2.up, true, 0.02f);
            Assert.That(events.Count, Is.InRange(3, 4));
            Assert.That(events[0].Kind, Is.EqualTo(NoiseKind.Sprinting));
            Assert.That(events[0].Intensity, Is.GreaterThan(0.2f));
            Assert.That(events[0].Range, Is.GreaterThan(4));
        }

        [Test]
        public void IdleAndPushingAgainstWallDoNotGenerateFootsteps()
        {
            var channel = Channel();
            int count = 0;
            channel.Emitted += _ => count++;
            var motor = Motor(channel);
            for (int i = 0; i < 100; i++) motor.Simulate(Vector2.zero, true, 0.02f);
            Assert.That(count, Is.Zero);
            Cube("Wall", origin + new Vector3(0, 1, 1), new Vector3(5, 3, 0.5f));
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) motor.Simulate(Vector2.up, true, 0.02f);
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void EnvironmentalInteractionEmitsSignificantNoiseWithCooldown()
        {
            var channel = Channel();
            int count = 0;
            NoiseEvent last = default;
            channel.Emitted += noise => { count++; last = noise; };
            var obstacle = Empty("Debris", origin).AddComponent<NoiseObstacle>();
            obstacle.Configure(channel);
            var interactor = Empty("Player", origin).AddComponent<PlayerInteractor>();
            obstacle.Interact(interactor);
            obstacle.Interact(interactor);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(last.Kind, Is.EqualTo(NoiseKind.Environment));
            Assert.That(last.Intensity, Is.EqualTo(1));
            Assert.That(last.Range, Is.EqualTo(18));
            Assert.That(last.Instigator, Is.EqualTo(interactor.gameObject));
        }

        [Test]
        public void Milestone2PrefabIsVariantWithAssignedLightAndSharedNoiseChannel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HerFootsteps/Prefabs/Milestone2Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Variant));
            var flashlight = new SerializedObject(prefab.GetComponent<PlayerFlashlight>());
            Assert.That(flashlight.FindProperty("beam").objectReferenceValue, Is.Not.Null);
            var emitter = new SerializedObject(prefab.GetComponent<MovementNoiseEmitter>());
            var hud = new SerializedObject(prefab.GetComponent<Milestone2Hud>());
            Assert.That(emitter.FindProperty("channel").objectReferenceValue, Is.Not.Null);
            Assert.That(hud.FindProperty("channel").objectReferenceValue,
                Is.EqualTo(emitter.FindProperty("channel").objectReferenceValue));
        }

        private GameObject Empty(string name, Vector3 position)
        {
            var item = new GameObject(name); item.transform.position = position; owned.Add(item); return item;
        }
        private GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            var item = Empty(name, position); item.transform.localScale = scale; item.AddComponent<BoxCollider>(); return item;
        }
        private PlayerFlashlight Flashlight()
        {
            var item = Empty("Flashlight", origin);
            var flashlight = item.AddComponent<PlayerFlashlight>();
            flashlight.Configure(null, item.AddComponent<Light>());
            return flashlight;
        }
        private NoiseChannel Channel()
        {
            var channel = ScriptableObject.CreateInstance<NoiseChannel>(); owned.Add(channel); return channel;
        }
        private FirstPersonMotor Motor(NoiseChannel channel)
        {
            Cube("Floor", origin - Vector3.up * 0.25f, new Vector3(100, 0.5f, 100));
            var item = Empty("Motor", origin + Vector3.up * 0.05f);
            var controller = item.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.3f; controller.center = Vector3.up * 0.9f;
            controller.skinWidth = 0.03f; controller.minMoveDistance = 0;
            var motor = item.AddComponent<FirstPersonMotor>();
            Physics.SyncTransforms();
            for (int i = 0; i < 10; i++) motor.Simulate(Vector2.zero, false, 0.02f);
            item.AddComponent<MovementNoiseEmitter>().Configure(motor, channel);
            return motor;
        }
    }
}
