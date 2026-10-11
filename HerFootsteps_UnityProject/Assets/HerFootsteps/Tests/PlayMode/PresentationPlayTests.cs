#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HerFootsteps.Tests
{
    public sealed class PresentationPlayTests
    {
        [UnityTest] public IEnumerator ForestStartsInGameplayModeWithClueHotbarSafeAreaAndNavigableRoutes()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Milestone5_Presentation.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();player.enabled=false;
            var brain=Object.FindAnyObjectByType<CryptidBrain>();Assert.That(brain.GetComponent<CryptidNavigation>().Ready,Is.True);
            var input=player.GetComponent<PlayerInputReader>();var inventory=player.GetComponent<PlayerInventory>();var composure=player.GetComponent<PlayerComposure>();
            Assert.That(input.ModalOpen,Is.False);Assert.That(player.GetComponent<InventoryView>().enabled,Is.False);
            Assert.That(player.GetComponent<PresentationDebugMode>().Visible,Is.False);Assert.That(player.GetComponent<Milestone5Hud>().enabled,Is.False);
            Assert.That(Object.FindObjectsByType<HotbarSlotView>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            var actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/HerFootsteps/Presentation/PresentationInput.inputactions");
            Assert.That(actions.FindAction("Player/Inventory",false),Is.Null);Assert.That(actions.FindAction("Player/Map").bindings[0].path,Is.EqualTo("<Keyboard>/tab"));
            var passive=player.GetComponent<ComposureRateSource>();float start=composure.Current;passive.Tick(2);Assert.That(composure.Current,Is.EqualTo(start));
            player.Teleport(new Vector3(0,0,-20));passive.Tick(2);Assert.That(composure.Current,Is.EqualTo(start-1).Within(.01f));
            var pickup=Object.FindObjectsByType<InventoryPickup>(FindObjectsSortMode.None)[0];pickup.Interact(player.GetComponent<PlayerInteractor>());
            var hud=player.GetComponent<PresentationHud>();hud.Select(0);var light=player.GetComponent<PlayerFlashlight>();light.Tick(10);
            Assert.That(hud.UseSelected(),Is.True);Assert.That(input.ModalOpen,Is.False);
            var clue=Object.FindAnyObjectByType<ClueDiscovery>();Assert.That(clue,Is.Not.Null);Assert.That(inventory.Slots.Count(s=>s.Item!=null),Is.EqualTo(1));
            composure.Apply(-40,"Test");float before=composure.Current;var interactor=player.GetComponent<PlayerInteractor>();
            player.Teleport(clue.transform.position+Vector3.back*1.7f-Vector3.up*.9f);player.GetComponentInChildren<Camera>().transform.LookAt(clue.transform.position);Physics.SyncTransforms();interactor.RefreshTarget();
            Assert.That(interactor.Target,Is.EqualTo(clue));interactor.ProcessInteraction(true,true,.5f);Assert.That(clue.IsDiscovered,Is.False);interactor.ProcessInteraction(false,false,.1f);
            interactor.ProcessInteraction(true,true,1.6f);Assert.That(clue.IsDiscovered,Is.True);Assert.That(composure.Current,Is.EqualTo(before+20).Within(.01f));
            Assert.That(inventory.Slots.Count(s=>s.Item!=null),Is.EqualTo(1));
            var hiding=player.GetComponent<PlayerHiding>();Assert.That(hiding.TryEnter(Object.FindAnyObjectByType<HidingSpot>()),Is.True,"Root shelter must have a clear capsule position.");Assert.That(hiding.TryLeave(),Is.True);
            foreach(var point in new[]{new Vector3(0,0,-29),new Vector3(-15,0,-8),new Vector3(-16,0,9),new Vector3(5,0,10),new Vector3(0,0,26)})
            {Assert.That(NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas),Is.True);var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(brain.transform.position,hit.position,NavMesh.AllAreas,path),Is.True);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),point.ToString());}
            player.GetComponent<PresentationDebugMode>().SetVisible(true);Assert.That(player.GetComponent<Milestone5Hud>().enabled,Is.True);
            player.GetComponent<PresentationDebugMode>().SetVisible(false);Assert.That(player.GetComponent<Milestone5Hud>().enabled,Is.False);
        }
    }
}
#endif
