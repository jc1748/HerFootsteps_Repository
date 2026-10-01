using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    // Build only on an explicit request; keep the designer's original scene intact.
    [InitializeOnLoad]
    public static class Milestone2Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone2_Test.unity";
        public const string PlayerPath = "Assets/HerFootsteps/Prefabs/Milestone2Player.prefab";
        public const string BatteryPath = "Assets/HerFootsteps/Prefabs/TestBattery.prefab";
        public const string DebrisPath = "Assets/HerFootsteps/Prefabs/TestDebris.prefab";
        public const string ChannelPath = "Assets/HerFootsteps/Settings/PrototypeNoise.asset";
        private const string RequestPath = "Library/HerFootsteps/Milestone2.request";

        static Milestone2Setup() => EditorApplication.update += CheckRequest;

        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
            string command;
            try
            {
                command = File.ReadAllText(RequestPath).Trim();
                if (command.Length == 0) return;
                File.Delete(RequestPath);
            }
            catch (IOException) { return; }
            try
            {
                if (command == "build") CreateTestScene();
                else if (command == "open") OpenTestScene();
                else if (command == "tests") Milestone1Setup.RunTests();
                else if (command == "playtests") Milestone1Setup.RunPlayTests();
                else throw new InvalidOperationException("Unknown Milestone 2 request: " + command);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone2-setup-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Her Footsteps/Milestone 2/Open Test Scene")]
        public static void OpenTestScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Create the Milestone 2 test scene first: " + ScenePath);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Her Footsteps/Milestone 2/Run All Milestone Edit Mode Tests")]
        public static void RunTests() => Milestone1Setup.RunTests();

        [MenuItem("Her Footsteps/Milestone 2/Run All Milestone Play Mode Tests")]
        public static void RunPlayTests() => Milestone1Setup.RunPlayTests();

        [MenuItem("Her Footsteps/Milestone 2/Create Test Scene (once)")]
        public static void CreateTestScene()
        {
            foreach (string path in new[] { ScenePath, PlayerPath, BatteryPath, DebrisPath, ChannelPath })
                if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            // Copy the current saved blockout, including designer edits, rather than rebuilding it.
            if (!AssetDatabase.CopyAsset(Milestone1Setup.ScenePath, ScenePath))
                throw new InvalidOperationException("Could not copy the saved Milestone 1 scene.");
            Directory.CreateDirectory("Assets/HerFootsteps/Settings");
            AssetDatabase.Refresh();
            var channel = ScriptableObject.CreateInstance<NoiseChannel>();
            AssetDatabase.CreateAsset(channel, ChannelPath);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(root => root.GetComponentsInChildren<FirstPersonMotor>(true)).Single();
                var player = motor.gameObject;
                var camera = player.GetComponentInChildren<Camera>();
                var input = player.GetComponent<PlayerInputReader>();
                var beamObject = new GameObject("Flashlight Beam");
                beamObject.transform.SetParent(camera.transform, false);
                beamObject.transform.localPosition = new Vector3(0.15f, -0.1f, 0.2f);
                var beam = beamObject.AddComponent<Light>();
                beam.type = LightType.Spot;
                beam.range = 18;
                beam.spotAngle = 55;
                beam.innerSpotAngle = 30;
                beam.intensity = 8;
                beam.shadows = LightShadows.Soft;
                beam.enabled = false;
                beamObject.AddComponent<UniversalAdditionalLightData>();
                var flashlight = player.AddComponent<PlayerFlashlight>();
                Reference(flashlight, "input", input);
                Reference(flashlight, "beam", beam);
                var emitter = player.AddComponent<MovementNoiseEmitter>();
                Reference(emitter, "motor", motor);
                Reference(emitter, "channel", channel);
                var hud = player.AddComponent<Milestone2Hud>();
                Reference(hud, "flashlight", flashlight);
                Reference(hud, "channel", channel);
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPath, InteractionMode.AutomatedAction);
                player.name = "Milestone2Player";

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.08f, 0.1f, 0.13f);
                var directional = roots.SelectMany(root => root.GetComponentsInChildren<Light>())
                    .First(light => light.type == LightType.Directional);
                directional.intensity = 0.12f;
                camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f);

                var batteryMaterial = Material("M2_Battery", new Color(0.2f, 0.7f, 0.9f));
                var debrisMaterial = Material("M2_Debris", new Color(0.65f, 0.35f, 0.12f));
                var additions = new GameObject("Milestone 2 Test Props");
                var pedestalMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M1_Obstacle.mat");
                Box("Battery Pedestal A", new Vector3(-2, 0.45f, -7), new Vector3(1, 0.9f, 1), pedestalMaterial, additions.transform);
                Box("Battery Pedestal B", new Vector3(7, 0.45f, 4), new Vector3(1, 0.9f, 1), pedestalMaterial, additions.transform);
                var battery = Box("TestBattery", Vector3.zero, new Vector3(0.3f, 0.45f, 0.3f), batteryMaterial, null);
                battery.AddComponent<BatteryPickup>();
                PrefabUtility.SaveAsPrefabAssetAndConnect(battery, BatteryPath, InteractionMode.AutomatedAction);
                battery.name = "Battery Pickup A";
                battery.transform.SetParent(additions.transform, true);
                battery.transform.position = new Vector3(-2, 1.125f, -7);
                var second = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BatteryPath), scene);
                second.name = "Battery Pickup B";
                second.transform.SetParent(additions.transform, true);
                second.transform.position = new Vector3(7, 1.125f, 4);

                var debris = new GameObject("TestDebris");
                var trigger = debris.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0, 0.5f, 0);
                trigger.size = new Vector3(3, 1, 3);
                var body = debris.AddComponent<Rigidbody>();
                body.isKinematic = true; body.useGravity = false;
                var obstacle = debris.AddComponent<NoiseObstacle>();
                Reference(obstacle, "channel", channel);
                Box("Debris Surface", new Vector3(0, 0.025f, 0), new Vector3(3, 0.05f, 3), debrisMaterial, debris.transform);
                PrefabUtility.SaveAsPrefabAssetAndConnect(debris, DebrisPath, InteractionMode.AutomatedAction);
                debris.name = "Noise Debris Patch";
                debris.transform.SetParent(additions.transform, true);
                debris.transform.position = new Vector3(0, 0, -2);

                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone2-setup.txt", "Created " + ScenePath + "\nBase scene and prefab preserved. No imported audio used.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, "Assets/HerFootsteps/Materials/" + name + ".mat");
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }
    }
}
