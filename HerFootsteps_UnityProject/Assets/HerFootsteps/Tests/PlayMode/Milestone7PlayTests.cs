#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class Milestone7PlayTests
    {
        [UnityTest] public IEnumerator ForestMapNavigationAndIntegratedMechanics()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone7_ForestBlockout.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var motor=Object.FindAnyObjectByType<FirstPersonMotor>();motor.enabled=false;var player=motor.gameObject;
            var composure=player.GetComponent<PlayerComposure>();var hiding=player.GetComponent<PlayerHiding>();var inventory=player.GetComponent<PlayerInventory>();
            Assert.That(Object.FindAnyObjectByType<Terrain>(),Is.Not.Null);Assert.That(player.GetComponent<PresentationHud>(),Is.Not.Null);Assert.That(player.GetComponent<InventoryView>().enabled,Is.False);
            var brain=Object.FindAnyObjectByType<CryptidBrain>();Assert.That(brain.GetComponent<CryptidNavigation>().Ready,Is.True);Assert.That(brain.enabled,Is.False);
            var hunt=Object.FindAnyObjectByType<HuntController>();Assert.That(hunt.State,Is.EqualTo(HuntState.SafePeriod));
            var passive=player.GetComponent<ComposureRateSource>();passive.Tick(2);Assert.That(composure.Current,Is.EqualTo(100));
            var channel=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");int events=0;void Heard(NoiseEvent n){if(n.Kind==NoiseKind.Environment)events++;}
            channel.Emitted+=Heard;Object.FindAnyObjectByType<NoiseObstacle>().Interact(player.GetComponent<PlayerInteractor>());channel.Emitted-=Heard;Assert.That(events,Is.EqualTo(1));Assert.That(hunt.State,Is.EqualTo(HuntState.SafePeriod));
            var corrupt=Object.FindObjectsByType<ComposureRateSource>(FindObjectsSortMode.None).First(s=>s.name.StartsWith("Route A"));motor.Teleport(corrupt.transform.position);float before=composure.Current;corrupt.Tick(2);Assert.That(composure.Current,Is.EqualTo(before-1).Within(.01f));
            var clue=Object.FindAnyObjectByType<ClueDiscovery>();composure.Apply(-30,"Test");before=composure.Current;clue.Interact(player.GetComponent<PlayerInteractor>());Assert.That(composure.Current,Is.EqualTo(before+20).Within(.01f));Assert.That(inventory.Slots.All(s=>s.Item==null),Is.True);
            var spots=Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);Assert.That(spots.Length,Is.EqualTo(7));
            foreach(var spot in spots){motor.Teleport(spot.ExitPosition.position);Physics.SyncTransforms();Assert.That(hiding.TryEnter(spot),Is.True,spot.name);Assert.That(hiding.TryLeave(),Is.True,spot.name+" exit");}
            foreach(var route in Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None))foreach(Transform node in route.transform)
            {Assert.That(NavMesh.SamplePosition(node.position,out var hit,2,NavMesh.AllAreas),Is.True,node.name);var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(brain.transform.position,hit.position,NavMesh.AllAreas,path),Is.True,route.name);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),route.name);}
            var tower=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Watchtower - accessible ramps");var foot=tower.Find("Access marker");var deck=tower.Find("Overlook marker - face northeast");
            NavMesh.SamplePosition(foot.position,out var start,2,NavMesh.AllAreas);NavMesh.SamplePosition(deck.position,out var end,1,NavMesh.AllAreas);var towerPath=new NavMeshPath();NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,towerPath);Assert.That(towerPath.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            motor.Teleport(start.position+Vector3.up*.1f);player.GetComponent<PlayerInputReader>().SetModalOpen(false);
            int corner=1;
            for(int step=0;step<1200&&corner<towerPath.corners.Length;step++)
            {
                var delta=towerPath.corners[corner]-motor.transform.position;delta.y=0;
                if(delta.magnitude<.35f){corner++;continue;}
                motor.transform.rotation=Quaternion.LookRotation(delta);motor.Simulate(Vector2.up,false,.05f);
                if(step%60==0)yield return null;
            }
            Assert.That(motor.transform.position.y,Is.GreaterThan(tower.position.y+9),"Player must physically ascend the ramps without jump/climb.");
            // Broken activation on the larger map still starts evidence investigation, never a teleport.
            var original=brain.transform.position;hunt.Tick(61,true);composure.Apply(-100,"M7 hunt check");Assert.That(hunt.State,Is.EqualTo(HuntState.Warning));hunt.Tick(5,true);Assert.That(brain.State,Is.EqualTo(CryptidState.Investigate));Assert.That(Vector3.Distance(brain.transform.position,original),Is.LessThan(.5f));
        }
    }
}
#endif
