#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone4PlayTests
    {
        private FirstPersonMotor player;
        private CryptidBrain brain;
        private CryptidNavigation navigation;
        private CryptidSenses senses;
        private NoiseChannel channel;

        private IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone4_Test.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            player = Object.FindAnyObjectByType<FirstPersonMotor>();
            brain = Object.FindAnyObjectByType<CryptidBrain>();
            Assert.That(brain, Is.Not.Null);
            navigation = brain.GetComponent<CryptidNavigation>();
            senses = brain.GetComponent<CryptidSenses>();
            channel = AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            player.enabled = false;
            player.GetComponent<PlayerInteractor>().enabled = false;
            player.GetComponent<PlayerBreath>().enabled = false; // deterministic breath ticks below
            Assert.That(navigation.Ready, Is.True, "Saved scene must contain a usable baked NavMesh.");
        }

        [UnityTest]
        public IEnumerator HeardSnapshotSearchesAndDisengagesWithoutFindingConcealedPlayer()
        {
            yield return Load();
            var hiding = player.GetComponent<PlayerHiding>();
            var spot = Object.FindAnyObjectByType<HidingSpot>();
            Assert.That(hiding.TryEnter(spot), Is.True);
            SetFloat(brain, "searchDuration", 3);
            var far = new Vector3(0, 0, -12);
            channel.Emit(NoiseKind.Walking, far, 0.2f, 4, player.gameObject, player.gameObject);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Idle));
            Vector3 sound = new Vector3(0, 0, -2);
            channel.Emit(NoiseKind.Environment, sound, 1, 18, player.gameObject, player.gameObject);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Investigate));
            Assert.That(brain.LastKnownPosition, Is.EqualTo(sound));
            bool searched = false, disengaged = false;
            float deadline = Time.time + 20;
            while (Time.time < deadline)
            {
                Assert.That(brain.State, Is.Not.EqualTo(CryptidState.Chase), "Cover should occlude the hidden player on this route.");
                searched |= brain.State == CryptidState.Search;
                disengaged |= brain.State == CryptidState.Disengage;
                if (disengaged && brain.State == CryptidState.Idle) break;
                Assert.That(brain.LastKnownPosition, Is.EqualTo(sound), "No live player position without a new observation.");
                yield return null;
            }
            Assert.That(searched && disengaged, Is.True);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Idle));
            Assert.That(brain.HasLastKnownPosition, Is.False);
        }

        [UnityTest]
        public IEnumerator VisualLossFreezesMemoryAndFreshSightReacquires()
        {
            yield return Load();
            Assert.That(navigation.Agent.Warp(new Vector3(8, 0, -4)), Is.True);
            brain.transform.rotation = Quaternion.identity;
            player.Teleport(new Vector3(8, 0, 0)); Physics.SyncTransforms();
            brain.Tick(0.02f);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Chase));
            Assert.That(navigation.Agent.speed, Is.EqualTo(4.5f));
            Vector3 seen = player.transform.position;
            // Move behind the agent without generating an event; no yield makes this deterministic.
            player.Teleport(new Vector3(8, 0, -10)); Physics.SyncTransforms();
            brain.Tick(0.02f);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Investigate));
            Assert.That(brain.LastKnownPosition, Is.EqualTo(seen));
            Assert.That(brain.InvestigationTarget, Is.EqualTo(seen));
            player.Teleport(new Vector3(9, 0, -11)); Physics.SyncTransforms(); brain.Tick(0.02f);
            Assert.That(brain.LastKnownPosition, Is.EqualTo(seen));
            player.Teleport(new Vector3(8, 0, 1)); Physics.SyncTransforms(); brain.Tick(0.02f);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Chase));
            Assert.That(brain.LastKnownPosition, Is.EqualTo(player.transform.position));
        }

        [UnityTest]
        public IEnumerator HiddenBreathSuppressionAndForcedBreathUseSharedChannel()
        {
            yield return Load();
            Assert.That(navigation.Agent.Warp(new Vector3(3, 0, -4)), Is.True);
            brain.transform.rotation = Quaternion.Euler(0, 180, 0);
            var hiding = player.GetComponent<PlayerHiding>();
            Assert.That(hiding.TryEnter(Object.FindAnyObjectByType<HidingSpot>()), Is.True);
            Physics.SyncTransforms();
            Assert.That(senses.TryObserve(out _), Is.False, "Rear cover must block sight.");
            var breath = player.GetComponent<PlayerBreath>();
            breath.Tick(true, 2);
            Assert.That(brain.HasNoise, Is.False);
            breath.Tick(true, 3);
            Assert.That(brain.LastNoise.Kind, Is.EqualTo(NoiseKind.ForcedBreath));
            Assert.That(brain.State, Is.EqualTo(CryptidState.Investigate));
            Assert.That(brain.LastKnownPosition, Is.EqualTo(player.transform.position));
            Assert.That(brain.VisuallyDetected, Is.False);
            var light = player.GetComponent<PlayerFlashlight>();
            light.Toggle(); Assert.That(senses.PlayerLightActive, Is.True);
            Assert.That(senses.TryObserve(out _), Is.False, "Light is accessible but does not bypass occlusion.");
            brain.enabled = false;
            Vector3 memory = brain.LastKnownPosition;
            channel.Emit(NoiseKind.Environment, Vector3.zero, 1, 20, player.gameObject);
            Assert.That(brain.LastKnownPosition, Is.EqualTo(memory), "Disabled brain must unsubscribe.");
        }

        [UnityTest]
        public IEnumerator SavedNavMeshRoutesAroundWallAndUnreachableTargetDoesNotStallBrain()
        {
            yield return Load();
            // Isolate navigation from perception for the path-around-wall assertion.
            senses.Configure(null, null, null);
            brain.enabled = false;
            Assert.That(navigation.Agent.Warp(new Vector3(4, 0, -1)), Is.True);
            Assert.That(navigation.MoveTo(new Vector3(4, 0, 5), 4), Is.True);
            Assert.That(navigation.Agent.path.corners.Length, Is.GreaterThan(2));
            float deadline = Time.time + 8;
            while (!navigation.Arrived && Time.time < deadline) yield return null;
            Assert.That(navigation.Arrived, Is.True, "Agent must actually traverse the route, not only calculate it.");
            brain.enabled = true;
            SetFloat(brain, "searchDuration", 0.2f);
            SetFloat(brain, "travelTimeout", 0.2f);
            Vector3 offMesh = brain.transform.position + Vector3.up * 5;
            channel.Emit(NoiseKind.Environment, offMesh, 1, 20, player.gameObject);
            Assert.That(brain.State, Is.EqualTo(CryptidState.Investigate));
            brain.Tick(0.1f); Assert.That(brain.State, Is.EqualTo(CryptidState.Search));
            brain.Tick(0.3f); Assert.That(brain.State, Is.EqualTo(CryptidState.Disengage));
            brain.Tick(0.3f); Assert.That(brain.State, Is.EqualTo(CryptidState.Idle));
        }

        private static void SetFloat(Object target, string property, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
