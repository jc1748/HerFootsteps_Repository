using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class Milestone3Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone3_Test.unity";
        public const string PlayerPath = "Assets/HerFootsteps/Prefabs/Milestone3Player.prefab";
        public const string SpotPath = "Assets/HerFootsteps/Prefabs/TestHidingSpot.prefab";
        private const string RequestPath = "Library/HerFootsteps/Milestone3.request";
        static Milestone3Setup() => EditorApplication.update += CheckRequest;

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
                else throw new InvalidOperationException("Unknown Milestone 3 request: " + command);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone3-setup-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Her Footsteps/Milestone 3/Open Test Scene")]
        public static void OpenTestScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Her Footsteps/Milestone 3/Run All Milestone Edit Mode Tests")]
        public static void RunTests() => Milestone1Setup.RunTests();
        [MenuItem("Her Footsteps/Milestone 3/Run All Milestone Play Mode Tests")]
        public static void RunPlayTests() => Milestone1Setup.RunPlayTests();

        [MenuItem("Her Footsteps/Milestone 3/Create Test Scene (once)")]
        public static void CreateTestScene()
        {
            foreach (string path in new[] { ScenePath, PlayerPath, SpotPath })
                if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            if (!AssetDatabase.CopyAsset(Milestone2Setup.ScenePath, ScenePath))
                throw new InvalidOperationException("Could not copy the saved Milestone 2 scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var motor = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FirstPersonMotor>(true)).Single();
                var player = motor.gameObject;
                var hiding = player.AddComponent<PlayerHiding>();
                var breath = player.AddComponent<PlayerBreath>();
                Reference(breath, "input", player.GetComponent<PlayerInputReader>());
                Reference(breath, "hiding", hiding);
                Reference(breath, "channel", AssetDatabase.LoadAssetAtPath<NoiseChannel>(Milestone2Setup.ChannelPath));
                Reference(player.GetComponent<PlayerInteractor>(), "hiding", hiding);
                var hud = player.AddComponent<Milestone3Hud>();
                Reference(hud, "hiding", hiding);
                Reference(hud, "breath", breath);
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPath, InteractionMode.AutomatedAction);
                player.name = "Milestone3Player";

                var spot = new GameObject("TestHidingSpot");
                var hidingSpot = spot.AddComponent<HidingSpot>();
                var hidden = new GameObject("Hiding Position (player feet)").transform;
                hidden.SetParent(spot.transform, false);
                hidden.localPosition = new Vector3(0, 0.05f, 0);
                var exit = new GameObject("Exit Position (player feet)").transform;
                exit.SetParent(spot.transform, false);
                exit.localPosition = new Vector3(0, 0.05f, -2);
                hidingSpot.Configure(hidden, exit);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M1_Obstacle.mat");
                Box("Left Cover", new Vector3(-0.9f, 1.1f, 0), new Vector3(0.2f, 2.2f, 2), material, spot.transform);
                Box("Right Cover", new Vector3(0.9f, 1.1f, 0), new Vector3(0.2f, 2.2f, 2), material, spot.transform);
                Box("Rear Cover", new Vector3(0, 1.1f, 0.9f), new Vector3(1.8f, 2.2f, 0.2f), material, spot.transform);
                Box("Roof", new Vector3(0, 2.2f, 0), new Vector3(2, 0.2f, 2), material, spot.transform);
                Box("E - Hiding Spot Marker", new Vector3(0, 1.2f, -0.9f), new Vector3(0.7f, 0.5f, 0.12f), material, spot.transform);
                // Save at origin; position only this instance in the copied blockout.
                PrefabUtility.SaveAsPrefabAssetAndConnect(spot, SpotPath, InteractionMode.AutomatedAction);
                spot.transform.position = new Vector3(3, 0, -8);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone3-setup.txt", "Created " + ScenePath + "\nMilestone 1/2 assets preserved. No imported audio used.");
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
        private static void Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
