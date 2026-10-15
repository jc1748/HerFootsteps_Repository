using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static class Milestone6Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone6_Gameplay.unity";
        public const string Folder = "Assets/HerFootsteps/Milestone6";
        [MenuItem("Her Footsteps/Presentation/Build Milestone 6")]
        public static void Build()
        {
            if (File.Exists(ScenePath) || Directory.Exists(Folder)) throw new InvalidOperationException("M6 exists; refusing to overwrite.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            AssetDatabase.CopyAsset(PresentationSetup.ScenePath, ScenePath);
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            T Find<T>() where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
            var player=Find<FirstPersonMotor>(); var input=player.GetComponent<PlayerInputReader>();
            var brain=Find<CryptidBrain>(); var composure=player.GetComponent<PlayerComposure>();
            var actions=InputActionAsset.FromJson(AssetDatabase.LoadAssetAtPath<InputActionAsset>(PresentationSetup.Folder+"/PresentationInput.inputactions").ToJson());
            var map=actions.FindActionMap("Player");
            for(int i=1;i<=5;i++) map.FindAction("Slot"+i).ChangeBinding(0).WithPath("<Keyboard>/"+i);
            map.AddAction("CycleSlot",InputActionType.Value,expectedControlLayout:"Axis").AddBinding("<Mouse>/scroll/y",groups:"Keyboard&Mouse");
            File.WriteAllText(Folder+"/GameplayInput.inputactions",actions.ToJson());Object.DestroyImmediate(actions);AssetDatabase.Refresh();
            // Explicitly replace the inherited scene override; saving a prefab alone did not do this in M5.1.
            Set(input,"actions",AssetDatabase.LoadAssetAtPath<InputActionAsset>(Folder+"/GameplayInput.inputactions"));
            var oldTrail=Find<FalseTrailHallucination>();oldTrail.gameObject.SetActive(false);
            var phantomObject=new GameObject("Phantom footsteps — perception only");
            phantomObject.transform.SetParent(player.transform,false);
            var phantom=phantomObject.AddComponent<PhantomFootstepsHallucination>();
            Set(phantom,"composure",composure);Set(phantom,"duration",8f);Set(phantom,"cooldown",25f);
            // Separate scene-root sound origin: once started it stays in world space.
            var steps=new GameObject("Phantom audio origin — no noise emitter").AddComponent<AudioSource>();
            steps.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/75266__garybran__heavyfootstompsinforest4.wav");
            steps.playOnAwake=false;steps.loop=true;steps.spatialBlend=1;steps.volume=.45f;steps.minDistance=2;steps.maxDistance=16;steps.rolloffMode=AudioRolloffMode.Linear;
            Set(phantom,"footsteps",steps);
            var canvas=player.GetComponentInChildren<Canvas>();
            var meter=new GameObject("Composure dial",typeof(RectTransform),typeof(CanvasRenderer),typeof(ComposureIndicator));
            var rect=meter.GetComponent<RectTransform>();rect.SetParent(canvas.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(0,0);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(82,82);rect.sizeDelta=new Vector2(88,88);
            Set(meter.GetComponent<ComposureIndicator>(),"composure",composure);
            var huntObject=new GameObject("Hunt — encounter orchestration");
            var hunt=huntObject.AddComponent<HuntController>();
            Set(hunt,"input",input);Set(hunt,"composure",composure);Set(hunt,"cryptid",brain);Set(hunt,"noise",AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset"));
            brain.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
            var ambience=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioSource>(true)).Where(a=>a.loop&&a!=steps).ToArray();
            var warning=new GameObject("Distant warning — nonlocal cue").AddComponent<AudioSource>();
            warning.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/869866__fusagawi__beast-growling.wav");
            warning.playOnAwake=false;warning.spatialBlend=0;warning.volume=.25f;warning.pitch=.8f;
            var presentation=huntObject.AddComponent<HuntPresentation>();
            Set(presentation,"hunt",hunt);Set(presentation,"input",input);Set(presentation,"hud",player.GetComponent<PresentationHud>());Set(presentation,"warning",warning);SetArray(presentation,"ambience",ambience);
            var debug=huntObject.AddComponent<HuntDebugView>();
            Set(debug,"hunt",hunt);Set(debug,"hiding",player.GetComponent<PlayerHiding>());Set(debug,"input",input);Set(debug,"phantom",phantom);
            var debugMode=player.GetComponent<PresentationDebugMode>();
            var serialized=new SerializedObject(debugMode);var panels=serialized.FindProperty("panels");var existing=Enumerable.Range(0,panels.arraySize).Select(i=>panels.GetArrayElementAtIndex(i).objectReferenceValue).ToList();existing.Add(debug);SetArray(debugMode,"panels",existing.ToArray());
            Set(debugMode,"eventHint","F7 phantom footsteps | F8 wildlife");
            var oldHud=player.GetComponent<Milestone5Hud>();Set(oldHud,"trail",null);Set(oldHud,"brokenEventHint"," (Hunt Controller gated)");Set(oldHud,"controlHint","F3 debug | F4/F5 composure | F7 phantom | F8 wildlife");
            debug.enabled=false;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            // Scene-path restart works in Editor and builds; retain all existing build entries.
            var buildScenes=EditorBuildSettings.scenes.ToList();if(!buildScenes.Any(s=>s.path==ScenePath)){buildScenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();}
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Milestone6-build.txt","Saved M6 from presentation scene. Existing geometry, NavMesh, fog and prior scenes preserved.\n");
        }
        internal static void Set(Object target,string field,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new Exception(target.name+" missing "+field);
            if(value is float f)p.floatValue=f;else if(value is bool b)p.boolValue=b;else if(value is string s)p.stringValue=s;else p.objectReferenceValue=value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        private static void SetArray(Object target,string field,Object[] values)
        {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
        [MenuItem("Her Footsteps/Presentation/Open Milestone 6")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
    }
}
