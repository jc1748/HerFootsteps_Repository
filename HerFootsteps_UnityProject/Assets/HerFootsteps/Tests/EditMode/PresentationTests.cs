using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HerFootsteps.Tests
{
    public sealed class PresentationTests
    {
        private static void Set(Object target,string field,Object value)
        {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        [Test] public void SafeVolumeSuspendsOnlyItsAssignedSourceAndResumesAfterExit()
        {
            var player=new GameObject("Composure");var area=new GameObject("Safe area");var source=new GameObject("Passive source");
            try
            {
                var composure=player.AddComponent<PlayerComposure>();var box=area.AddComponent<BoxCollider>();box.size=Vector3.one*5;
                var rate=source.AddComponent<ComposureRateSource>();Set(rate,"player",composure);
                var so=new SerializedObject(rate);var list=so.FindProperty("suppressInside");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=box;so.ApplyModifiedPropertiesWithoutUndo();
                rate.Tick(10);Assert.That(composure.Current,Is.EqualTo(100));
                composure.Apply(-10,"Fright still applies in calm area");Assert.That(composure.Current,Is.EqualTo(90));
                player.transform.position=Vector3.right*10;rate.Tick(1);Assert.That(composure.Current,Is.EqualTo(87));
                player.transform.position=Vector3.zero;box.enabled=false;rate.Tick(1);Assert.That(composure.Current,Is.EqualTo(84));
            }
            finally {Object.DestroyImmediate(source);Object.DestroyImmediate(area);Object.DestroyImmediate(player);}
        }
        [Test] public void ClueDiscoveryRecoversOnceWithoutAnInventory()
        {
            var player=new GameObject("Player");var clueObject=new GameObject("Clue");
            try
            {
                var composure=player.AddComponent<PlayerComposure>();composure.Apply(-50,"Setup");var interactor=player.AddComponent<PlayerInteractor>();var clue=clueObject.AddComponent<ClueDiscovery>();int discoveries=0;clue.Discovered+=(_,__)=>discoveries++;
                Assert.That(clue.HoldDuration,Is.EqualTo(1.5f));clue.Interact(interactor);clue.Interact(interactor);
                Assert.That(composure.Current,Is.EqualTo(70));Assert.That(discoveries,Is.EqualTo(1));Assert.That(clue.CanInteract,Is.False);Assert.That(player.GetComponent<PlayerInventory>(),Is.Null);
            }
            finally {Object.DestroyImmediate(clueObject);Object.DestroyImmediate(player);}
        }
    }
}
