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
    public sealed class Milestone6PlayTests
    {
        private const string Scene="Assets/Scenes/Milestone6_Gameplay.unity";
        private static void Set(Object o,string field,float value){var so=new SerializedObject(o);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        [UnityTest] public IEnumerator HuntDiscardsSafeTriggersUsesSnapshotsAndResolvesThroughExistingAI()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var hunt=Object.FindAnyObjectByType<HuntController>();var player=Object.FindAnyObjectByType<FirstPersonMotor>();player.enabled=false;
            var composure=player.GetComponent<PlayerComposure>();var brain=hunt.Cryptid;var channel=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            // Reset encounter lifecycle to test the waiting-for-control gate independently of Editor focus.
            hunt.enabled=false;hunt.enabled=true;
            hunt.Tick(10,false);Assert.That(hunt.SafeRemaining,Is.EqualTo(60));
            composure.Apply(-100,"Safe-period Broken");Assert.That(hunt.State,Is.EqualTo(HuntState.SafePeriod));Assert.That(brain.enabled,Is.False);
            channel.Emit(NoiseKind.Scream,brain.transform.position,1.2f,18,player.gameObject,player.gameObject);
            hunt.Tick(61,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));hunt.Tick(2,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));
            var evidence=brain.transform.position;channel.Emit(NoiseKind.Walking,evidence,.2f,4,player.gameObject,player.gameObject);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));
            channel.Emit(NoiseKind.Environment,evidence,1,18,player.gameObject,player.gameObject);Assert.That(hunt.State,Is.EqualTo(HuntState.Warning));Assert.That(brain.enabled,Is.False);
            player.Teleport(new Vector3(0,0,-29));hunt.Tick(4.1f,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Active));Assert.That(brain.InvestigationTarget,Is.EqualTo(evidence));
            brain.Tick(16);Assert.That(brain.State,Is.EqualTo(CryptidState.Search));brain.Tick(9);Assert.That(brain.State,Is.EqualTo(CryptidState.Disengage));brain.Tick(16);hunt.Tick(.1f,true);
            Assert.That(hunt.State,Is.EqualTo(HuntState.Cooldown));Assert.That(brain.enabled,Is.False);
            composure.Apply(100,"Recover");composure.Apply(-100,"Cooldown Broken");channel.Emit(NoiseKind.Scream,evidence,1.2f,18,player.gameObject,player.gameObject);
            hunt.Tick(31,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));hunt.Tick(1,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));
            composure.Apply(100,"Recover");composure.Apply(-100,"Fresh Broken");Assert.That(hunt.State,Is.EqualTo(HuntState.Warning));Assert.That(hunt.InitialEvidence,Is.EqualTo(brain.transform.position));Assert.That(hunt.InitialEvidence,Is.Not.EqualTo(player.transform.position));
        }
        [UnityTest] public IEnumerator SavedSceneHotbarCaptureOcclusionFailureAndRestart()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();player.enabled=false;var input=player.GetComponent<PlayerInputReader>();
            var inputSo=new SerializedObject(input);Assert.That(AssetDatabase.GetAssetPath(inputSo.FindProperty("actions").objectReferenceValue),Is.EqualTo("Assets/HerFootsteps/Milestone6/GameplayInput.inputactions"));
            Assert.That(Object.FindAnyObjectByType<ComposureIndicator>(),Is.Not.Null);Assert.That(Object.FindAnyObjectByType<FalseTrailHallucination>(),Is.Null);
            var hud=player.GetComponent<PresentationHud>();var inventory=player.GetComponent<PlayerInventory>();var light=player.GetComponent<PlayerFlashlight>();
            inventory.TryAdd(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/HerFootsteps/Presentation/Battery.asset"),2);light.RestoreBattery(100);if(light.IsActive)light.Toggle();
            hud.Select(0);Assert.That(hud.UseSelected(),Is.False);Assert.That(inventory.Slots[0].Quantity,Is.EqualTo(2));
            light.Toggle();light.Tick(15);light.Toggle();Assert.That(hud.UseSelected(),Is.True);Assert.That(light.Battery,Is.EqualTo(100));Assert.That(inventory.Slots[0].Quantity,Is.EqualTo(1));
            hud.Cycle(-1);Assert.That(hud.Selected,Is.EqualTo(4));Assert.That(hud.UseSelected(),Is.False);hud.Cycle(1);Assert.That(hud.Selected,Is.Zero);Assert.That(input.ModalOpen,Is.False);
            var hunt=Object.FindAnyObjectByType<HuntController>();hunt.Tick(61,true);player.GetComponent<PlayerComposure>().Apply(-100,"Capture test");hunt.Tick(5,true);
            var brain=hunt.Cryptid;var senses=brain.GetComponent<CryptidSenses>();player.Teleport(brain.transform.position+brain.transform.forward*.9f);Physics.SyncTransforms();brain.Tick(.01f);
            Assert.That(brain.State,Is.EqualTo(CryptidState.Chase));Assert.That(hunt.CanCapture(),Is.True);
            var wall=new GameObject("Temporary occlusion test").AddComponent<BoxCollider>();wall.transform.position=brain.transform.position+brain.transform.forward*.45f+Vector3.up;wall.transform.rotation=brain.transform.rotation;wall.size=new Vector3(3,3,.1f);Physics.SyncTransforms();
            Assert.That(hunt.CanCapture(),Is.False,"Capture cannot pass through cover");Object.Destroy(wall.gameObject);yield return null;Physics.SyncTransforms();
            hunt.Tick(.01f,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Failed));Assert.That(input.ModalOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);
            Object.FindAnyObjectByType<HuntPresentation>().Restart();yield return null;yield return null;
            // Wait for scene load without a gameplay-time-dependent delay (failure was paused).
            for(int i=0;i<120 && Object.FindAnyObjectByType<HuntController>()==hunt;i++)yield return null;
            Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(Object.FindAnyObjectByType<HuntController>().State,Is.EqualTo(HuntState.SafePeriod));
            Assert.That(Object.FindAnyObjectByType<PlayerInventory>().Slots[0].Item,Is.Null);
        }
        [TearDown] public void RestoreTime()=>Time.timeScale=1;
    }
}
#endif
