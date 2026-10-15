using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HerFootsteps.Tests
{
    public sealed class Milestone6Tests
    {
        [Test] public void GameplayBindingsResolveToRealKeyboardAndMouseControls()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            var asset=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/HerFootsteps/Milestone6/GameplayInput.inputactions"));
            try
            {
                asset.bindingMask=InputBinding.MaskByGroup("Keyboard&Mouse");asset.devices=new InputDevice[]{keyboard,mouse};asset.Enable();
                for(int i=1;i<=5;i++) Assert.That(asset.FindAction("Player/Slot"+i).controls,Does.Contain(keyboard[(Key)((int)Key.Digit1+i-1)]));
                Assert.That(asset.FindAction("Player/UseItem").controls,Does.Contain(keyboard.rKey));
                Assert.That(asset.FindAction("Player/CycleSlot").controls,Does.Contain(mouse.scroll.y));
                Assert.That(asset.FindAction("Player/Map").controls,Does.Contain(keyboard.tabKey));
                Assert.That(asset.FindAction("Player/Inventory",false),Is.Null);
            }
            finally {asset.Disable();Object.DestroyImmediate(asset);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);}
        }
        [Test] public void PhantomLifecycleRecoversAndNeverPublishesGameplayNoise()
        {
            var player=new GameObject("Player");var eventObject=new GameObject("Phantom");eventObject.SetActive(false);var soundObject=new GameObject("Sound");
            var channel=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");int noiseCount=0;
            void Heard(NoiseEvent e)=>noiseCount++;
            channel.Emitted+=Heard;
            var clip=AudioClip.Create("Test steps",4410,1,44100,false);
            try
            {
                var composure=player.AddComponent<PlayerComposure>();var phantom=eventObject.AddComponent<PhantomFootstepsHallucination>();
                var audio=soundObject.AddComponent<AudioSource>();audio.clip=clip;
                var so=new SerializedObject(phantom);so.FindProperty("composure").objectReferenceValue=composure;so.FindProperty("footsteps").objectReferenceValue=audio;so.FindProperty("automatic").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
                eventObject.SetActive(true);Assert.That(phantom.TryActivate(),Is.False);
                composure.Apply(-80,"Test");Assert.That(phantom.TryActivate(),Is.True);phantom.Tick(1);
                composure.Apply(80,"Recovery");phantom.Tick(.01f);Assert.That(phantom.IsActive,Is.False);Assert.That(phantom.CooldownRemaining,Is.GreaterThan(0));
                composure.Apply(-80,"Test");Assert.That(phantom.TryActivate(),Is.False);Assert.That(noiseCount,Is.Zero);
            }
            finally {channel.Emitted-=Heard;Object.DestroyImmediate(eventObject);Object.DestroyImmediate(player);Object.DestroyImmediate(soundObject);Object.DestroyImmediate(clip);}
        }
    }
}
