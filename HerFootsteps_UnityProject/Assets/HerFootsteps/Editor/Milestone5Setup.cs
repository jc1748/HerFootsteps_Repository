using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class Milestone5Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone5_Test.unity";
        public const string PlayerPath = "Assets/HerFootsteps/Prefabs/Milestone5Player.prefab";
        public const string BatteryPath = "Assets/HerFootsteps/Prefabs/InventoryBattery.prefab";
        public const string DataFolder = "Assets/HerFootsteps/Settings/Milestone5";
        private const string RequestPath = "Library/HerFootsteps/Milestone5.request";
        static Milestone5Setup() => EditorApplication.update += CheckRequest;
        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
            string command;
            try { command = File.ReadAllText(RequestPath).Trim(); if (command.Length == 0) return; File.Delete(RequestPath); }
            catch (IOException) { return; }
            try
            {
                if (command == "build") CreateTestScene();
                else if (command == "open") OpenTestScene();
                else if (command == "tests") Milestone1Setup.RunTests();
                else if (command == "playtests") Milestone1Setup.RunPlayTests();
                else throw new InvalidOperationException("Unknown Milestone 5 request: " + command);
            }
            catch (Exception error)
            {
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Milestone5-setup-error.txt", error.ToString()); Debug.LogException(error);
            }
        }
        [MenuItem("Her Footsteps/Milestone 5/Open Test Scene")]
        public static void OpenTestScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Her Footsteps/Milestone 5/Run All Milestone Edit Mode Tests")]
        public static void RunTests() => Milestone1Setup.RunTests();
        [MenuItem("Her Footsteps/Milestone 5/Run All Milestone Play Mode Tests")]
        public static void RunPlayTests() => Milestone1Setup.RunPlayTests();
        [MenuItem("Her Footsteps/Milestone 5/Create Test Scene (once)")]
        public static void CreateTestScene()
        {
            foreach (string path in new[] { ScenePath, PlayerPath, BatteryPath, DataFolder })
                if (File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            Directory.CreateDirectory(DataFolder); AssetDatabase.Refresh();
            var channel = Create<ComposureChannel>("ComposureChannel");
            var batteryEffect = Create<BatteryItemEffect>("BatteryEffect");
            var recoveryEffect = Create<ComposureItemEffect>("RecoveryEffect");
            var battery = Item("Battery", "Flashlight battery", "Restores 40 charge when used. A full flashlight keeps the battery.", 3, true, true, batteryEffect);
            var keepsake = Item("SisterKeepsake", "Sister's keepsake (test)", "A placeholder personal belonging. First discovery restores 20 composure. Kept for future narrative use; no progression implemented.", 1, false, false, null);
            var stone = Item("TestStone", "Inventory test stone", "Non-stackable placeholder to test capacity, inspection and removal. No gameplay effect.", 1, false, true, null);
            var token = Item("RecoveryToken", "Recovery token (test)", "Temporary consumable: restores 15 composure. Kept if composure is full.", 2, true, true, recoveryEffect);
            if (!AssetDatabase.CopyAsset(Milestone4Setup.ScenePath, ScenePath)) throw new InvalidOperationException("Could not copy M4 scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var player = roots.SelectMany(root => root.GetComponentsInChildren<FirstPersonMotor>(true)).Single();
                var cryptid = roots.SelectMany(root => root.GetComponentsInChildren<CryptidBrain>(true)).Single();
                var surface = roots.SelectMany(root => root.GetComponentsInChildren<NavMeshSurface>(true)).Single();
                surface.name = "Milestone 5 Navigation Geometry";
                var controls = player.GetComponent<PlayerInputReader>();
                var composure = player.gameObject.AddComponent<PlayerComposure>(); Set(composure, "channel", channel);
                var inventory = player.gameObject.AddComponent<PlayerInventory>();
                var passive = player.gameObject.AddComponent<ComposureRateSource>();
                Set(passive, "player", composure); Set(passive, "sourceName", "Passive danger"); Set(passive, "ratePerSecond", -0.5f); Set(passive, "sourceActive", false);
                var threat = player.gameObject.AddComponent<CryptidComposureSource>(); Set(threat, "player", composure);
                var discovery = player.gameObject.AddComponent<DiscoveryComposureRecovery>();
                Set(discovery, "inventory", inventory); Set(discovery, "composure", composure); Set(discovery, "meaningfulItem", keepsake);
                var hud = player.gameObject.AddComponent<Milestone5Hud>();
                Set(hud, "input", controls); Set(hud, "composure", composure); Set(hud, "inventory", inventory); Set(hud, "passive", passive); Set(hud, "cryptidSource", threat);
                var view = player.gameObject.AddComponent<InventoryView>();
                Set(view, "input", controls); Set(view, "inventory", inventory); Set(view, "debug", hud);
                PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, PlayerPath, InteractionMode.AutomatedAction);
                player.name = "Milestone5Player";
                Set(threat, "cryptid", cryptid);
                var camera = player.GetComponentInChildren<Camera>();
                cryptid.GetComponent<CryptidSenses>().Configure(player, camera.transform, cryptid.transform.Find("Eyes"));
                PrefabUtility.RecordPrefabInstancePropertyModifications(cryptid.GetComponent<CryptidSenses>());
                var noise = AssetDatabase.LoadAssetAtPath<NoiseChannel>(Milestone2Setup.ChannelPath);
                var blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M1_Obstacle.mat");
                var orange = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M1_Indicator.mat");
                var cyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M2_Battery.mat");

                // Scene-only conversion: M2-M4 TestBattery prefab remains a direct-refill prototype.
                foreach (var old in roots.SelectMany(root => root.GetComponentsInChildren<BatteryPickup>(true)))
                {
                    var obj = old.gameObject;
                    PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    UnityEngine.Object.DestroyImmediate(old);
                    obj.AddComponent<InventoryPickup>().Configure(battery);
                }
                var additions = new GameObject("Milestone 5 Test Props"); additions.transform.SetParent(surface.transform, false);
                Box("Inventory test bench", new Vector3(-6, 0.45f, -10), new Vector3(5, 0.9f, 1), blue, additions.transform);
                var batteryObject = Box("InventoryBattery", Vector3.zero, new Vector3(0.3f, 0.45f, 0.3f), cyan, null);
                batteryObject.AddComponent<InventoryPickup>().Configure(battery);
                PrefabUtility.SaveAsPrefabAssetAndConnect(batteryObject, BatteryPath, InteractionMode.AutomatedAction);
                batteryObject.transform.SetParent(additions.transform, true); batteryObject.transform.position = new Vector3(-8, 1.125f, -10);
                batteryObject.name = "Battery pack x4"; batteryObject.GetComponent<InventoryPickup>().Configure(battery, 4);
                PrefabUtility.RecordPrefabInstancePropertyModifications(batteryObject.GetComponent<InventoryPickup>());
                Pickup("Battery x1", battery, 1, new Vector3(-7, 1.125f, -10), cyan, additions.transform);
                Pickup("Sister keepsake", keepsake, 1, new Vector3(-6, 1.125f, -10), cyan, additions.transform);
                Pickup("Test stones x3 (three slots)", stone, 3, new Vector3(-5, 1.125f, -10), blue, additions.transform);
                Pickup("Recovery tokens x2", token, 2, new Vector3(-4, 1.125f, -10), cyan, additions.transform);
                Label("E: inventory items — Tab to inspect/use", new Vector3(-6, 1.8f, -10), additions.transform);
                var loss = Box("Test fright interaction (-25)", new Vector3(-4, 0.8f, -13), new Vector3(0.6f, 1.6f, 0.6f), orange, additions.transform).AddComponent<ComposureInteractable>();
                Set(loss, "uses", 4); Set(loss, "sourceName", "Test fright");
                Label("E: fright -25 (4 uses)", new Vector3(-4, 1.9f, -13), additions.transform);
                var recovery = Box("Test recovery interaction (+25)", new Vector3(-9, 0.8f, -10), new Vector3(0.6f, 1.6f, 0.6f), cyan, additions.transform).AddComponent<ComposureInteractable>();
                Set(recovery, "sourceName", "Reassuring object"); Set(recovery, "amount", 25f); Set(recovery, "uses", 2);
                Label("E: recovery +25 (2 uses)", new Vector3(-9, 1.9f, -10), additions.transform);
                Area("Danger area -3 per sec", new Vector3(-7, 0, -3), -3, -1, orange, composure, additions.transform);
                Area("Safe area +5 per sec (budget 30, cap 75)", new Vector3(-7, 0, -13), 5, 30, cyan, composure, additions.transform);

                // Hallucination geometry is separate from the NavMesh hierarchy, with no colliders.
                var hallucinations = new GameObject("Milestone 5 Hallucinations (not real geometry)");
                var trailObject = new GameObject("False Trail Event"); trailObject.transform.SetParent(hallucinations.transform, false); trailObject.transform.position = new Vector3(-7, 0, 0);
                var trail = trailObject.AddComponent<FalseTrailHallucination>();
                var trailVisual = new GameObject("Hallucinated trail markers"); trailVisual.transform.SetParent(trailObject.transform, false);
                for (int i = 0; i < 4; i++)
                {
                    var marker = Box("False arrow " + i, new Vector3(-i * 0.5f, 0.3f, i * 1.2f), new Vector3(0.5f, 0.6f, 0.15f), cyan, trailVisual.transform);
                    UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                }
                Label("THIS WAY? (false trail)", new Vector3(0, 1.3f, 0), trailVisual.transform);
                Set(trail, "composure", composure); Set(trail, "visualRoot", trailVisual); trailVisual.SetActive(false);
                var animalObject = new GameObject("Wildlife Event"); animalObject.transform.SetParent(hallucinations.transform, false); animalObject.transform.position = new Vector3(-7, 0, -6);
                var wildlife = animalObject.AddComponent<WildlifeHallucination>();
                var animalVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule); animalVisual.name = "Hallucinated threatening wildlife";
                animalVisual.transform.SetParent(animalObject.transform, false); animalVisual.transform.localPosition = new Vector3(0, 0.7f, 0); animalVisual.transform.localScale = new Vector3(1, 0.7f, 1);
                animalVisual.GetComponent<Renderer>().sharedMaterial = orange; UnityEngine.Object.DestroyImmediate(animalVisual.GetComponent<Collider>());
                Label("THREAT? Aim flashlight here", new Vector3(0, 1.5f, 0), animalObject.transform).transform.SetParent(animalVisual.transform, true);
                Set(wildlife, "composure", composure); Set(wildlife, "visualRoot", animalVisual); Set(wildlife, "reactionTarget", animalVisual.transform);
                Set(wildlife, "flashlight", player.GetComponent<PlayerFlashlight>()); Set(wildlife, "beam", player.GetComponentInChildren<Light>()); Set(wildlife, "view", camera); Set(wildlife, "noise", noise);
                Set(wildlife, "failureClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/618112__nachtmahrtv__woman-scream.wav"));
                animalVisual.SetActive(false);
                Set(hud, "trail", trail); Set(hud, "wildlife", wildlife);

                surface.BuildNavMesh();
                if (surface.navMeshData == null) throw new InvalidOperationException("M5 NavMesh bake failed.");
                AssetDatabase.CreateAsset(surface.navMeshData, DataFolder + "/Milestone5NavMesh.asset");
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Milestone5-setup.txt", "Created M5 scene, player/battery prefabs, four item definitions, two item effects, composure channel, separate NavMesh. M1-M4 preserved.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static T Create<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); asset.name = name; AssetDatabase.CreateAsset(asset, DataFolder + "/" + name + ".asset"); return asset;
        }
        private static ItemDefinition Item(string name, string label, string description, int stack, bool consumable, bool removable, ItemUseEffect effect)
        {
            var item = Create<ItemDefinition>(name);
            Set(item, "displayName", label); Set(item, "description", description); Set(item, "maximumStack", stack); Set(item, "consumable", consumable); Set(item, "removable", removable); Set(item, "useEffect", effect); return item;
        }
        private static void Pickup(string name, ItemDefinition item, int count, Vector3 position, Material material, Transform parent)
        { Box(name, position, new Vector3(0.3f, 0.45f, 0.3f), material, parent).AddComponent<InventoryPickup>().Configure(item, count); }
        private static void Area(string name, Vector3 position, float rate, float budget, Material material, PlayerComposure player, Transform parent)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            var volume = root.AddComponent<BoxCollider>(); volume.isTrigger = true; volume.center = Vector3.up; volume.size = new Vector3(3, 2, 3);
            var source = root.AddComponent<ComposureRateSource>(); Set(source, "player", player); Set(source, "area", volume); Set(source, "sourceName", name); Set(source, "ratePerSecond", rate); Set(source, "totalBudget", budget);
            var visual = Box("Area marker (no collision)", new Vector3(0, 0.015f, 0), new Vector3(3, 0.02f, 3), material, root.transform); UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            Label(name, new Vector3(0, 0.2f, 0), root.transform);
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static GameObject Label(string text, Vector3 position, Transform parent)
        {
            var obj = new GameObject(text); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.identity;
            var label = obj.AddComponent<TextMesh>(); label.text = text; label.characterSize = 0.07f; label.fontSize = 40; label.anchor = TextAnchor.MiddleCenter; label.color = Color.white; return obj;
        }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (value is float f) p.floatValue = f;
            else if (value is int i) p.intValue = i;
            else if (value is bool b) p.boolValue = b;
            else if (value is string s) p.stringValue = s;
            else p.objectReferenceValue = value as UnityEngine.Object;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
