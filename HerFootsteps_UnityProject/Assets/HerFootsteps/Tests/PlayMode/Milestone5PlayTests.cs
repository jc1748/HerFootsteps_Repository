#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone5PlayTests
    {
        private FirstPersonMotor player;
        private PlayerComposure composure;
        private PlayerInventory inventory;
        private IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone5_Test.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            player = Object.FindAnyObjectByType<FirstPersonMotor>();
            composure = player.GetComponent<PlayerComposure>(); inventory = player.GetComponent<PlayerInventory>();
            player.enabled = false;
            Assert.That(Object.FindAnyObjectByType<CryptidNavigation>().Ready, Is.True);
            Assert.That(Object.FindObjectsByType<BatteryPickup>(FindObjectsSortMode.None), Is.Empty);
        }
        private static void Set(Object target, string name, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(name); Assert.That(p, Is.Not.Null, name);
            if (value is bool b) p.boolValue = b; else if (value is float f) p.floatValue = f; else p.objectReferenceValue = (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnityTest] public IEnumerator PickupStoresBatteryAndModalPreservesHidingLock()
        {
            yield return Load();
            var flashlight = player.GetComponent<PlayerFlashlight>(); var interactor = player.GetComponent<PlayerInteractor>();
            var battery = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/HerFootsteps/Settings/Milestone5/Battery.asset");
            var pickupObject = new GameObject("Test inventory pickup"); var pickup = pickupObject.AddComponent<InventoryPickup>(); pickup.Configure(battery, 2);
            pickup.Interact(interactor); Assert.That(pickup.Collected, Is.True); Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(2));
            Assert.That(flashlight.Battery, Is.EqualTo(100)); Assert.That(inventory.TryUse(0), Is.False);
            flashlight.Toggle(); flashlight.Tick(10); Assert.That(inventory.TryUse(0), Is.True); Assert.That(flashlight.Battery, Is.EqualTo(100));
            var hiding = player.GetComponent<PlayerHiding>(); Assert.That(hiding.TryEnter(Object.FindAnyObjectByType<HidingSpot>()), Is.True);
            var view = player.GetComponent<InventoryView>(); var input = player.GetComponent<PlayerInputReader>();
            view.SetOpen(true); Assert.That(input.HasControl, Is.False); Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Vector3 position = player.transform.position; player.Simulate(Vector2.up, true, 1);
            interactor.RefreshTarget(); interactor.ProcessInteraction(true, true, 2);
            Assert.That(player.transform.position, Is.EqualTo(position)); Assert.That(hiding.IsHidden, Is.True);
            view.SetOpen(false); Assert.That(player.MovementLocked, Is.True); Assert.That(hiding.IsHidden, Is.True);
            Object.Destroy(pickupObject);
        }
        [UnityTest] public IEnumerator WildlifeFailureEmitsOneScreamAndCryptidInvestigatesSnapshot()
        {
            yield return Load();
            var wildlife = Object.FindAnyObjectByType<WildlifeHallucination>(); wildlife.Automatic = false;
            Object.FindAnyObjectByType<FalseTrailHallucination>().Automatic = false;
            Set(wildlife, "requireViewToStart", false); Set(wildlife, "activationRange", 100f);
            var brain = Object.FindAnyObjectByType<CryptidBrain>(); brain.GetComponent<CryptidSenses>().Configure(null, null, null);
            player.Teleport(new Vector3(0, 0, -2)); Physics.SyncTransforms(); composure.Apply(-80, "Test fright");
            var noise = AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            int screams = 0; System.Action<NoiseEvent> count = e => { if (e.Kind == NoiseKind.Scream) screams++; }; noise.Emitted += count;
            try
            {
                Assert.That(wildlife.TryActivate(), Is.True); wildlife.Tick(4.1f);
                Assert.That(screams, Is.EqualTo(1)); Assert.That(brain.State, Is.EqualTo(CryptidState.Investigate));
                Assert.That(brain.LastKnownPosition, Is.EqualTo(player.transform.position));
                wildlife.Tick(1); Assert.That(screams, Is.EqualTo(1)); Assert.That(wildlife.TryActivate(), Is.False);
            }
            finally { noise.Emitted -= count; }
        }
        [UnityTest] public IEnumerator FlashlightDismissesWildlifeAndRecoveryCancelsWithoutNoise()
        {
            yield return Load();
            var wildlife = Object.FindAnyObjectByType<WildlifeHallucination>(); wildlife.Automatic = false;
            Object.FindAnyObjectByType<FalseTrailHallucination>().Automatic = false;
            Set(wildlife, "requireViewToStart", false); Set(wildlife, "activationRange", 100f);
            player.Teleport(new Vector3(0, 0, -2));
            wildlife.transform.position = player.transform.position + Vector3.up * 2 + Vector3.forward * 4;
            var beam = player.GetComponentInChildren<Light>(); beam.transform.LookAt(wildlife.transform.position + Vector3.up * 0.7f);
            Physics.SyncTransforms(); composure.Apply(-80, "Test fright");
            var flashlight = player.GetComponent<PlayerFlashlight>(); flashlight.Toggle();
            var noise = AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            int screams = 0; System.Action<NoiseEvent> count = e => { if (e.Kind == NoiseKind.Scream) screams++; }; noise.Emitted += count;
            try
            {
                Assert.That(wildlife.TryActivate(), Is.True); wildlife.Tick(0.5f);
                Assert.That(wildlife.IsActive, Is.False); Assert.That(wildlife.Status, Is.EqualTo("Dismissed"));
                wildlife.Tick(21); Assert.That(wildlife.TryActivate(), Is.True);
                composure.Apply(80, "Recovery"); Assert.That(wildlife.IsActive, Is.False); Assert.That(wildlife.Status, Is.EqualTo("Recovered")); Assert.That(screams, Is.Zero);
            }
            finally { noise.Emitted -= count; }
        }
        [UnityTest] public IEnumerator AreasDiscoveryAndCryptidPressureAreIndependent()
        {
            yield return Load();
            var rates = Object.FindObjectsByType<ComposureRateSource>(FindObjectsSortMode.None);
            var danger = System.Array.Find(rates, r => r.name.StartsWith("Danger area"));
            var safe = System.Array.Find(rates, r => r.name.StartsWith("Safe area"));
            Assert.That(danger, Is.Not.Null); Assert.That(safe, Is.Not.Null);
            // Character-controller skin can leave the root slightly below the floor.
            player.Teleport(new Vector3(-7, -0.02f, -3)); danger.Tick(1);
            Assert.That(composure.Current, Is.EqualTo(97));
            player.Teleport(new Vector3(0, 0, -12)); danger.Tick(1);
            Assert.That(composure.Current, Is.EqualTo(97));
            composure.Apply(-57, "Setup"); player.Teleport(new Vector3(-7, -0.02f, -13)); safe.Tick(10);
            Assert.That(composure.Current, Is.EqualTo(70)); Assert.That(safe.RemainingBudget, Is.Zero);
            var keepsake = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/HerFootsteps/Settings/Milestone5/SisterKeepsake.asset");
            Assert.That(inventory.TryAdd(keepsake), Is.True); Assert.That(composure.Current, Is.EqualTo(90));
            Assert.That(inventory.TryAdd(keepsake), Is.True); Assert.That(composure.Current, Is.EqualTo(90));
            Assert.That(inventory.TryRemove(0), Is.False);
            var brain = Object.FindAnyObjectByType<CryptidBrain>();
            var pressure = player.GetComponent<CryptidComposureSource>();
            brain.GetComponent<CryptidNavigation>().Agent.Warp(new Vector3(8, 0, -4));
            brain.transform.rotation = Quaternion.identity; player.Teleport(new Vector3(8, 0, 0)); Physics.SyncTransforms(); brain.Tick(0.02f);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Chase));
            pressure.Tick(1); Assert.That(composure.Current, Is.EqualTo(90), "All pressure sources start off.");
            pressure.DetectionEnabled = true; pressure.Tick(0); Assert.That(composure.Current, Is.EqualTo(82));
            pressure.Tick(0); Assert.That(composure.Current, Is.EqualTo(82), "Sustained detection is not a repeated impulse.");
            pressure.DetectionEnabled = false; pressure.ProximityEnabled = true; pressure.Tick(1); Assert.That(composure.Current, Is.EqualTo(80));
            pressure.ProximityEnabled = false; pressure.PursuitEnabled = true; pressure.Tick(1); Assert.That(composure.Current, Is.EqualTo(76));
            Assert.That(brain.State, Is.EqualTo(CryptidState.Chase), "Composure adapter must not change AI state.");
        }
    }
}
#endif
