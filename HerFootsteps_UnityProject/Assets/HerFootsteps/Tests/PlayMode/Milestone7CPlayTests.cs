#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone7CPlayTests
    {
        private static void Set(Object o,string field,float value)
        { var s=new SerializedObject(o);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
        [UnityTest]
        public IEnumerator ForestFeedbackAndHallucinationReactions()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone7B_ForestRefinement.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();player.enabled=false;
            var comp=player.GetComponent<PlayerComposure>();var flash=player.GetComponent<PlayerFlashlight>();var camera=player.GetComponentInChildren<Camera>();
            var director=Object.FindAnyObjectByType<HallucinationDirector>();Assert.That(director,Is.Not.Null);director.enabled=false;
            var hunt=Object.FindAnyObjectByType<HuntController>();hunt.enabled=false;hunt.enabled=true;
            foreach(var rate in Object.FindObjectsByType<ComposureRateSource>(FindObjectsSortMode.None))rate.enabled=false;
            var noise=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");int emissions=0;void Heard(NoiseEvent e){emissions++;}noise.Emitted+=Heard;
            try
            {
                var obstacle=Object.FindObjectsByType<NoiseObstacle>(FindObjectsSortMode.None).First();
                int initial=emissions;obstacle.Interact(player.GetComponent<PlayerInteractor>());obstacle.Interact(player.GetComponent<PlayerInteractor>());
                Assert.That(emissions,Is.EqualTo(initial+1));Assert.That(obstacle.GetComponent<AudioSource>().isPlaying,Is.True);Assert.That(obstacle.GetComponent<AudioSource>().spatialBlend,Is.EqualTo(1));
                comp.Apply(60-comp.Current,"7C test medium");
                Assert.That(director.Request(0),Is.True,director.PlacementStatus);
                var whisper=Object.FindAnyObjectByType<WhisperHallucination>();Assert.That(whisper.IsActive,Is.True);whisper.Tick(6);
                Assert.That(director.Request(1),Is.True,director.PlacementStatus);var phantom=Object.FindAnyObjectByType<PhantomFootstepsHallucination>();Assert.That(phantom.IsActive,Is.True);phantom.Tick(9);
                Assert.That(director.Request(3),Is.False,"Beast must be gated at medium composure");
                comp.Apply(25-comp.Current,"7C test low");
                // Use existing route nodes, not an invented empty scene, until a clear approach is found.
                var route=Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None).SelectMany(r=>r.GetComponentsInChildren<Transform>().Skip(1)).ToArray();
                bool started=false;
                foreach(var point in route)
                {
                    player.Teleport(point.position+Vector3.up*.2f);camera.transform.rotation=Quaternion.LookRotation(Vector3.forward);
                    Physics.SyncTransforms();if(director.Request(3)){started=true;break;}
                }
                Assert.That(started,Is.True,director.PlacementStatus);var beast=Object.FindAnyObjectByType<SanityBeastHallucination>();
                if(flash.IsActive)flash.Toggle();float before=Vector3.Distance(beast.transform.position,player.transform.position);beast.Tick(.2f);
                Assert.That(Vector3.Distance(beast.transform.position,player.transform.position),Is.LessThan(before));
                flash.RestoreBattery(100);flash.Toggle();
                camera.transform.LookAt(camera.transform.position+(camera.transform.position-beast.transform.position));
                Assert.That(beast.Illuminated,Is.False,"Enabled flashlight pointing away must not count");
                camera.transform.LookAt(beast.transform.position+Vector3.up*1.1f);
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=Vector3.Lerp(camera.transform.position,beast.transform.position+Vector3.up*1.1f,.5f);blocker.transform.localScale=Vector3.one*2;Physics.SyncTransforms();
                Assert.That(beast.Illuminated,Is.False,"Solid occluder must block illumination");Object.DestroyImmediate(blocker);Physics.SyncTransforms();yield return null;
                Assert.That(beast.Illuminated,Is.True,"Actual beam should cover target");beast.Tick(.3f);Assert.That(beast.IsActive,Is.False);Assert.That(beast.Status,Is.EqualTo("Dismissed"));
                beast.Tick(66);if(flash.IsActive)flash.Toggle();
                Assert.That(director.Request(3),Is.True,director.PlacementStatus);int psychologicalStart=emissions;beast.Tick(4.1f);
                Assert.That(beast.Status,Is.EqualTo("FailedReaction"));Assert.That(emissions,Is.EqualTo(psychologicalStart),"Sanity scream must not emit NoiseChannel events");
                Assert.That(GameObject.Find("Sanity scream - psychological audio only").GetComponent<AudioSource>().isPlaying,Is.True);
                Assert.That(director.Request(2),Is.True,director.PlacementStatus);Assert.That(Object.FindAnyObjectByType<WildlifeHallucination>().IsActive,Is.True);
                comp.Apply(100-comp.Current,"7C recovery");Assert.That(Object.FindAnyObjectByType<WildlifeHallucination>().IsActive,Is.False);
                Assert.That(RenderSettings.fog,Is.True);Assert.That(RenderSettings.fogColor.b,Is.GreaterThan(.05f));
                Assert.That(Object.FindAnyObjectByType<NoiseExposureIndicator>().rectTransform.anchorMin,Is.EqualTo(new Vector2(.5f,1)));
                Assert.That(Object.FindAnyObjectByType<ComposureIndicator>().rectTransform.anchorMin,Is.EqualTo(Vector2.one));
                Assert.That(GameObject.Find("M7C Ground foliage - editable grass and shrubs").GetComponentsInChildren<Collider>().Length,Is.Zero);
                Assert.That(FlashlightIndicator.BatteryColor(0),Is.Not.EqualTo(FlashlightIndicator.BatteryColor(1)));
                Assert.That(FlashlightIndicator.BatteryColor(25),Is.Not.EqualTo(FlashlightIndicator.BatteryColor(26)));
            }
            finally{noise.Emitted-=Heard;}
        }
    }
}
#endif
