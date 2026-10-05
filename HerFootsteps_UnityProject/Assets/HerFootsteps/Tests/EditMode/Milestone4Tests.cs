using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace HerFootsteps.Tests
{
    public sealed class Milestone4Tests
    {
        private readonly List<Object> owned = new List<Object>();
        private readonly Vector3 origin = new Vector3(9000, 9000, 9000);
        private GameObject Make(string name, Vector3 position)
        {
            var item = new GameObject(name); item.transform.position = position; owned.Add(item); return item;
        }
        [TearDown]
        public void Cleanup()
        {
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
            owned.Clear();
        }

        [TestCase(NoiseKind.Walking, 4f, 0.2f)]
        [TestCase(NoiseKind.Sprinting, 10f, 0.6f)]
        [TestCase(NoiseKind.Environment, 18f, 1f)]
        [TestCase(NoiseKind.ForcedBreath, 12f, 0.8f)]
        [TestCase(NoiseKind.Breathing, 2f, 0.1f)]
        public void HearingUsesEventRangeAndPosition(NoiseKind kind, float range, float intensity)
        {
            var senses = Make("Senses", origin).AddComponent<CryptidSenses>();
            Assert.That(senses.CanHear(new NoiseEvent(kind, origin + Vector3.forward * (range - 0.1f), intensity, range, 0, null, null), out _), Is.True);
            Assert.That(senses.CanHear(new NoiseEvent(kind, origin + Vector3.forward * (range + 0.1f), intensity, range, 0, null, null), out _), Is.False);
        }
        [Test]
        public void HearingCapsRadiusAndRejectsQuietEvents()
        {
            var senses = Make("Senses", origin).AddComponent<CryptidSenses>();
            Assert.That(senses.CanHear(new NoiseEvent(NoiseKind.Environment, origin + Vector3.forward * 21, 1, 100, 0, null, null), out float effective), Is.False);
            Assert.That(effective, Is.EqualTo(20));
            Assert.That(senses.CanHear(new NoiseEvent(NoiseKind.Environment, origin, 0.01f, 10, 0, null, null), out _), Is.False);
        }
        [Test]
        public void SightRequiresRangeFovAndClearGeometryEvenWhenHidden()
        {
            var senses = Make("Senses", origin).AddComponent<CryptidSenses>();
            var player = Make("Player", origin + Vector3.forward * 5).AddComponent<FirstPersonMotor>();
            player.gameObject.layer = 2;
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.3f; controller.center = Vector3.up * 0.9f;
            var hiding = player.gameObject.AddComponent<PlayerHiding>();
            var head = Make("Head", player.transform.position + Vector3.up * 1.65f).transform;
            head.SetParent(player.transform, true);
            senses.Configure(player, head, null);
            Physics.SyncTransforms();
            Assert.That(senses.TryObserve(out Vector3 observed), Is.True);
            Assert.That(observed, Is.EqualTo(player.transform.position));
            senses.transform.rotation = Quaternion.Euler(0, 180, 0);
            Assert.That(senses.TryObserve(out _), Is.False);
            senses.transform.rotation = Quaternion.identity;
            player.Teleport(origin + Vector3.forward * 15);
            Assert.That(senses.TryObserve(out _), Is.False);
            player.Teleport(origin + Vector3.forward * 5);
            var spot = Make("Cover marker", player.transform.position).AddComponent<HidingSpot>();
            spot.Configure(spot.transform, null); Physics.SyncTransforms();
            Assert.That(hiding.TryEnter(spot), Is.True);
            Assert.That(senses.PlayerHidden, Is.True);
            Assert.That(senses.TryObserve(out _), Is.True, "A hiding flag without cover cannot grant invisibility.");
            var wall = Make("Wall", origin + new Vector3(0, 1.1f, 2.5f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(4, 2.2f, 0.2f); Physics.SyncTransforms();
            Assert.That(senses.TryObserve(out _), Is.False);
            wall.enabled = false; Physics.SyncTransforms();
            Assert.That(senses.TryObserve(out _), Is.True);
        }
        [Test]
        public void SavedCryptidAndNavMeshExistWithoutModifyingPlayerPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HerFootsteps/Prefabs/TestCryptid.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<NavMeshAgent>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CryptidBrain>(), Is.Not.Null);
            Assert.That(new SerializedObject(prefab.GetComponent<CryptidBrain>()).FindProperty("channel").objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/HerFootsteps/Settings/Milestone4NavMesh.asset"), Is.Not.Null);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HerFootsteps/Prefabs/Milestone3Player.prefab");
            Assert.That(player.GetComponent<PlayerHiding>(), Is.Not.Null);
            Assert.That(player.GetComponent<CryptidBrain>(), Is.Null);
        }
    }
}

