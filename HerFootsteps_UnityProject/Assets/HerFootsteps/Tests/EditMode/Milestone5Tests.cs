using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HerFootsteps.Tests
{
    public sealed class Milestone5Tests
    {
        private readonly List<Object> owned = new List<Object>();
        private T Component<T>() where T : Component
        { var go = new GameObject(typeof(T).Name); owned.Add(go); return go.AddComponent<T>(); }
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset); return asset; }
        private static void Set(Object target, string name, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(name);
            Assert.That(p, Is.Not.Null, name);
            if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else p.objectReferenceValue = (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void Cleanup()
        { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }

        [Test] public void ComposureClampsAndPublishesOnlyBrokenEntries()
        {
            var player = Component<PlayerComposure>(); var channel = Asset<ComposureChannel>(); Set(player, "channel", channel);
            int local = 0, shared = 0; player.BrokenEntered += _ => local++; channel.BrokenEntered += _ => shared++;
            Assert.That(player.Classify(70), Is.EqualTo(ComposureState.High));
            Assert.That(player.Classify(35), Is.EqualTo(ComposureState.Medium));
            Assert.That(player.Classify(1), Is.EqualTo(ComposureState.Low));
            Assert.That(player.Apply(-200, "Fright"), Is.EqualTo(-100));
            player.Apply(-10, "Still frightened"); Assert.That(local, Is.EqualTo(1));
            player.Apply(10, "Recovery"); player.Apply(-10, "New fright");
            Assert.That(local, Is.EqualTo(2)); Assert.That(shared, Is.EqualTo(2));
            Assert.That(player.Apply(200, "Recovery"), Is.EqualTo(100));
            Assert.That(player.Apply(float.NaN, "Invalid"), Is.Zero);
        }
        [Test] public void InventoryStacksAndRejectsWholePickupWithoutLosingItems()
        {
            var inventory = Component<PlayerInventory>(); Set(inventory, "capacity", 2);
            var item = Asset<ItemDefinition>(); Set(item, "maximumStack", 3);
            Assert.That(inventory.TryAdd(item, 4), Is.True);
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(3)); Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(1));
            Assert.That(inventory.TryAdd(item, 3), Is.False);
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(1));
            Assert.That(inventory.TryAdd(item, 2), Is.True);
            Set(item, "removable", false); Assert.That(inventory.TryRemove(0), Is.False);
        }
        [Test] public void BatteryUseConsumesOnlyAfterSuccessfulClampedRecharge()
        {
            var inventory = Component<PlayerInventory>(); var flashlight = inventory.gameObject.AddComponent<PlayerFlashlight>();
            var item = Asset<ItemDefinition>(); Set(item, "consumable", true); Set(item, "maximumStack", 3); Set(item, "useEffect", Asset<BatteryItemEffect>());
            inventory.TryAdd(item, 2); Assert.That(inventory.TryUse(0), Is.False); Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(2));
            flashlight.Toggle(); flashlight.Tick(5); Assert.That(flashlight.Battery, Is.EqualTo(90));
            Assert.That(inventory.TryUse(0), Is.True); Assert.That(flashlight.Battery, Is.EqualTo(100)); Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1));
        }
        [Test] public void RecoveryBudgetAndCeilingSurviveLeavingAndReentering()
        {
            var player = Component<PlayerComposure>(); player.Apply(-60, "Setup");
            var source = Component<ComposureRateSource>(); Set(source, "player", player); Set(source, "ratePerSecond", 5f); Set(source, "totalBudget", 30f);
            source.Tick(10); Assert.That(player.Current, Is.EqualTo(70)); Assert.That(source.RemainingBudget, Is.Zero);
            source.enabled = false; source.enabled = true; player.Apply(-20, "Fright"); source.Tick(10);
            Assert.That(player.Current, Is.EqualTo(50));
            var other = Component<ComposureRateSource>(); Set(other, "player", player); Set(other, "ratePerSecond", 5f); other.Tick(100);
            Assert.That(player.Current, Is.EqualTo(75));
        }
        [Test] public void FalseTrailRequiresLowComposureCancelsOnRecoveryAndRespectsCooldown()
        {
            var player = Component<PlayerComposure>(); var trail = Component<FalseTrailHallucination>(); trail.Automatic = false;
            Set(trail, "composure", player); Assert.That(trail.TryActivate(), Is.False);
            player.Apply(-80, "Setup"); Assert.That(trail.TryActivate(), Is.True);
            player.Apply(50, "Recovery"); trail.Tick(0.1f); Assert.That(trail.IsActive, Is.False);
            player.Apply(-50, "Fright"); Assert.That(trail.TryActivate(), Is.False);
            trail.Tick(21); Assert.That(trail.TryActivate(), Is.True);
        }
    }
}
