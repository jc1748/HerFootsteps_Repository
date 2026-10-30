#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;

namespace HerFootsteps.Tests
{
    public class Milestone7BPlayTests
    {
        [UnityTest]
        public IEnumerator ExposureGatesConcealedEntryAndPhysicalReturn()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone7B_ForestRefinement.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var motor=Object.FindAnyObjectByType<FirstPersonMotor>();motor.enabled=false;
            var hunt=Object.FindAnyObjectByType<HuntController>();var exposure=motor.GetComponent<NoiseExposure>();var placement=hunt.GetComponent<HuntPlacement>();var brain=hunt.Cryptid;
            var noise=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            Assert.That(hunt.IndividualNoiseTriggers,Is.False);Assert.That(Object.FindAnyObjectByType<NoiseExposureIndicator>(),Is.Not.Null);
            hunt.enabled=false;hunt.enabled=true;
            void Emit(NoiseKind kind,Vector3 position)=>noise.Emit(kind,position,1.2f,20,motor.gameObject,motor.gameObject);
            Emit(NoiseKind.Scream,motor.transform.position);Assert.That(exposure.Current,Is.Zero);Assert.That(hunt.State,Is.EqualTo(HuntState.SafePeriod));
            hunt.Tick(61,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));
            var outsider=new GameObject("Unrelated source");noise.Emit(NoiseKind.Scream,motor.transform.position,1,20,outsider,outsider);Object.Destroy(outsider);Assert.That(exposure.Current,Is.Zero);
            Emit(NoiseKind.Environment,brain.transform.position);Assert.That(exposure.Current,Is.EqualTo(20));Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration),"Individual audible noise must not duplicate the exposure trigger");
            Emit(NoiseKind.Walking,motor.transform.position);Assert.That(exposure.Current,Is.EqualTo(20.4f).Within(.01f));
            float retained=exposure.Current;yield return null;Assert.That(exposure.Current,Is.EqualTo(retained),"Exposure must not decay");
            int warnings=0;void Changed(HuntState state){if(state==HuntState.Warning)warnings++;}hunt.StateChanged+=Changed;
            var evidence=motor.transform.position;Emit(NoiseKind.Scream,evidence);Assert.That(exposure.Locked,Is.True);Assert.That(exposure.Current,Is.EqualTo(exposure.Threshold));
            Emit(NoiseKind.Scream,evidence);Assert.That(warnings,Is.EqualTo(1));Assert.That(brain.enabled,Is.False);
            var home=brain.transform.position;motor.Teleport(motor.transform.position+Vector3.left*2);Physics.SyncTransforms();
            hunt.Tick(5,true);
            Assert.That(hunt.State,Is.EqualTo(HuntState.Active),placement.Status);
            Assert.That(brain.InvestigationTarget,Is.EqualTo(evidence));Assert.That(brain.InvestigationTarget,Is.Not.EqualTo(motor.transform.position));
            Assert.That(placement.IsInCameraView(placement.LastEntry),Is.False);Assert.That(Vector3.Distance(placement.LastEntry,motor.transform.position),Is.InRange(16f,32f));
            Assert.That(brain.GetComponent<CryptidNavigation>().Ready,Is.True);Assert.That(Vector3.Distance(home,brain.transform.position),Is.GreaterThan(3));
            // Keep the observer outside sight range while the bounded search expires.
            brain.Tick(91);brain.Tick(9);hunt.Tick(.01f,true);
            Assert.That(hunt.State,Is.EqualTo(HuntState.Cooldown));Assert.That(exposure.Current,Is.Zero);Assert.That(exposure.Locked,Is.False);Assert.That(placement.Returning,Is.True);
            var nav=brain.GetComponent<CryptidNavigation>();Assert.That(Vector3.Distance(nav.Destination,placement.HomePosition),Is.LessThan(2));
            Emit(NoiseKind.Scream,motor.transform.position);Assert.That(exposure.Current,Is.Zero);
            // Exercise actual NavMeshAgent locomotion, without a test warp to the destination.
            Time.timeScale=10;
            for(int i=0;i<1200&&placement.Returning;i++)yield return null;
            Time.timeScale=1;
            Assert.That(placement.Returning,Is.False,placement.Status);Assert.That(Vector3.Distance(brain.transform.position,placement.HomePosition),Is.LessThan(1.3f));
            Assert.That(placement.Status,Does.Not.Contain("Hidden return"),"Normal return should walk home");
            hunt.Tick(31,true);Assert.That(hunt.State,Is.EqualTo(HuntState.Exploration));Assert.That(exposure.Current,Is.Zero);Assert.That(warnings,Is.EqualTo(1));hunt.StateChanged-=Changed;
        }
        [TearDown] public void RestoreTime()=>Time.timeScale=1;
        [UnityTest]
        public IEnumerator ReleasedMovementOnDesignerTerrain()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone7_ForestBlockout.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var motor=Object.FindAnyObjectByType<FirstPersonMotor>();motor.enabled=false;
            motor.GetComponent<PlayerInputReader>().SetModalOpen(false);
            var terrain=Object.FindAnyObjectByType<Terrain>();float worst=0;Vector3 where=default;
            foreach(var route in Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None))foreach(Transform node in route.transform)
            {
                var p=node.position;p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.12f;motor.Teleport(p);Physics.SyncTransforms();
                for(int i=0;i<30;i++)motor.Simulate(Vector2.up,false,1f/60);
                for(int i=0;i<30;i++)motor.Simulate(Vector2.zero,false,1f/60);
                var start=motor.transform.position;
                for(int i=0;i<300;i++)motor.Simulate(Vector2.zero,false,1f/60);
                var delta=motor.transform.position-start;delta.y=0;if(delta.magnitude>worst){worst=delta.magnitude;where=start;}
            }
            Debug.Log($"M7B saved terrain maximum no-input drift: {worst:F5}m at {where}");
            Assert.That(worst,Is.LessThan(.02f),"Saved terrain no-input slide");
        }
        [UnityTest]
        public IEnumerator ReleasedMovementStaysStillOnWalkableSlopes()
        {
            foreach(float angle in new[]{0f,12f,28f,44f,48f,55f})
            {
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.position=new Vector3(1000,0,1000);
                ground.transform.localScale=new Vector3(30,1,30);
                ground.transform.rotation=Quaternion.Euler(angle,0,0);
                var player=new GameObject("Slope regression player");
                var controller=player.AddComponent<CharacterController>();
                controller.height=1.8f;controller.center=Vector3.up*.9f;controller.radius=.3f;controller.skinWidth=.08f;controller.slopeLimit=45;
                var motor=player.AddComponent<FirstPersonMotor>();motor.enabled=false;motor.Teleport(new Vector3(1000,3,1000));
                Physics.SyncTransforms();
                for(int i=0;i<120;i++)motor.Simulate(Vector2.zero,false,1f/60);
                var start=player.transform.position;
                for(int i=0;i<300;i++)motor.Simulate(Vector2.zero,false,1f/60);
                var delta=player.transform.position-start;delta.y=0;
                Debug.Log($"M7B idle slope {angle}: horizontal drift {delta.magnitude:F5}m over 5s; grounded={controller.isGrounded}");
                Object.Destroy(ground);Object.Destroy(player);yield return null;
                Assert.That(delta.magnitude,Is.LessThan(.015f),"No-input drift on slope "+angle);
            }
        }
    }
}
#endif
