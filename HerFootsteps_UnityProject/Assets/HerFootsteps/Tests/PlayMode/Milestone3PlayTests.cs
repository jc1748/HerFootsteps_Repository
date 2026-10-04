#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone3PlayTests
    {
        [UnityTest]
        public IEnumerator SavedSceneEntersThroughRaycastAndLeavesWhileLookingAway()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone3_Test.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var hiding = Object.FindAnyObjectByType<PlayerHiding>();
            var spot = Object.FindAnyObjectByType<HidingSpot>();
            Assert.That(hiding, Is.Not.Null); Assert.That(spot, Is.Not.Null);
            var motor = hiding.GetComponent<FirstPersonMotor>(); motor.enabled = false;
            var interactor = hiding.GetComponent<PlayerInteractor>(); interactor.enabled = false;
            var breath = hiding.GetComponent<PlayerBreath>(); breath.enabled = false;
            var flashlight = hiding.GetComponent<PlayerFlashlight>(); flashlight.Toggle();
            motor.Teleport(spot.ExitPosition.position);
            motor.transform.rotation = Quaternion.identity;
            var camera = motor.GetComponentInChildren<Camera>();
            camera.transform.LookAt(spot.transform.Find("E - Hiding Spot Marker"));
            Physics.SyncTransforms();
            interactor.RefreshTarget(); Assert.That(interactor.Target, Is.EqualTo(spot));
            interactor.ProcessInteraction(true, true, 0.02f);
            Assert.That(hiding.IsHidden, Is.True); Assert.That(flashlight.IsActive, Is.True);
            Vector3 position = hiding.transform.position;
            for (int i = 0; i < 10; i++) motor.Simulate(Vector2.up, true, 0.02f);
            Assert.That(hiding.transform.position, Is.EqualTo(position));
            breath.Tick(true, 1); Assert.That(breath.IsHoldingBreath, Is.True);
            breath.Tick(true, 4); Assert.That(breath.IsForcedRecovery, Is.True);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            interactor.RefreshTarget(); Assert.That(interactor.Target, Is.EqualTo(spot));
            interactor.ProcessInteraction(true, true, 0.02f);
            Assert.That(hiding.IsHidden, Is.False); Assert.That(motor.MovementLocked, Is.False);
            Assert.That(hiding.transform.position, Is.EqualTo(spot.ExitPosition.position));
            Assert.That(hiding.TryEnter(spot), Is.True);
            spot.enabled = false;
            Assert.That(hiding.IsHidden, Is.False);
            Assert.That(motor.MovementLocked, Is.False);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
