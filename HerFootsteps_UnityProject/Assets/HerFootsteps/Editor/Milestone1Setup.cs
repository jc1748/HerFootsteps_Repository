using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    // Only runs from the menu, batch executeMethod, or an explicit local request file.
    // Never silently regenerates or overwrites a scene/prefab on assembly reload.
    [InitializeOnLoad]
    public static class Milestone1Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone1_Test.unity";
        public const string PlayerPath = "Assets/HerFootsteps/Prefabs/PrototypePlayer.prefab";
        public const string TogglePath = "Assets/HerFootsteps/Prefabs/TestToggle.prefab";
        private const string RequestPath = "Library/HerFootsteps/Milestone1.request";
        private static TestRunnerApi runner;

        static Milestone1Setup()
        {
            EditorApplication.update += CheckRequest;
            if (SessionState.GetBool("HF.M1.TestsActive", false)) RegisterRunner();
        }

        private static void RegisterRunner()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
        }

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
            catch (IOException) { return; } // The request writer may still own the file.
            try
            {
                if (command == "build") CreateTestScene();
                else if (command == "tests") RunTests();
                else if (command == "playtests") RunPlayTests();
                else throw new InvalidOperationException("Unknown Milestone 1 request: " + command);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone1-setup-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Her Footsteps/Milestone 1/Create Test Scene (once)")]
        public static void CreateTestScene()
        {
            if (File.Exists(ScenePath) || File.Exists(PlayerPath) || File.Exists(TogglePath))
                throw new InvalidOperationException("Milestone 1 assets already exist; refusing to overwrite them.");
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (actions == null || shader == null) throw new InvalidOperationException("Input Actions or URP Lit missing.");
            Directory.CreateDirectory("Assets/HerFootsteps/Prefabs");
            Directory.CreateDirectory("Assets/HerFootsteps/Materials");
            AssetDatabase.Refresh();

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.6f);
                RenderSettings.fog = false;
                var floor = Material("Floor", shader, new Color(0.28f, 0.32f, 0.32f));
                var wall = Material("Boundary", shader, new Color(0.18f, 0.22f, 0.28f));
                var obstacle = Material("Obstacle", shader, new Color(0.3f, 0.48f, 0.62f));
                var indicator = Material("Indicator", shader, new Color(0.85f, 0.35f, 0.12f));

                var environment = new GameObject("Temporary Blockout");
                Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(24, 0.5f, 32), floor, environment.transform);
                Box("North Boundary", new Vector3(0, 1.5f, 16), new Vector3(24, 3, 0.5f), wall, environment.transform);
                Box("South Boundary", new Vector3(0, 1.5f, -16), new Vector3(24, 3, 0.5f), wall, environment.transform);
                Box("East Boundary", new Vector3(12, 1.5f, 0), new Vector3(0.5f, 3, 32), wall, environment.transform);
                Box("West Boundary", new Vector3(-12, 1.5f, 0), new Vector3(0.5f, 3, 32), wall, environment.transform);
                Box("Occlusion Wall", new Vector3(4, 1.25f, 2), new Vector3(3, 2.5f, 0.5f), wall, environment.transform);
                Box("Slalom Block A", new Vector3(-2, 0.75f, 2), new Vector3(2, 1.5f, 2), obstacle, environment.transform);
                Box("Slalom Block B", new Vector3(1, 0.75f, 7), new Vector3(2, 1.5f, 2), obstacle, environment.transform);
                var ramp = Box("15 Degree Ramp", new Vector3(-7, 0.7f, 5), new Vector3(3, 0.3f, 6), obstacle, environment.transform);
                ramp.transform.rotation = Quaternion.Euler(-15, 0, 0);
                for (int i = 0; i < 3; i++)
                    Box("Step " + (i + 1), new Vector3(7, (i + 1) * 0.1f, 8 + i),
                        new Vector3(3, (i + 1) * 0.2f, 1), obstacle, environment.transform);

                var lightObject = new GameObject("Test Directional Light");
                lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                lightObject.AddComponent<UniversalAdditionalLightData>();

                var player = new GameObject("PrototypePlayer");
                player.layer = 2; // Ignore Raycast: the player's own capsule cannot block interaction.
                var controller = player.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.center = new Vector3(0, 0.9f, 0);
                controller.stepOffset = 0.3f;
                controller.slopeLimit = 45;
                controller.skinWidth = 0.03f;
                controller.minMoveDistance = 0;
                var cameraObject = new GameObject("Player Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
                var camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 150;
                camera.fieldOfView = 75;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
                var input = player.AddComponent<PlayerInputReader>();
                input.Configure(actions);
                var motor = player.AddComponent<FirstPersonMotor>();
                motor.Configure(input, cameraObject.transform);
                var interactor = player.AddComponent<PlayerInteractor>();
                interactor.Configure(input, camera);
                player.AddComponent<PrototypeHud>().Configure(motor, interactor, input);
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPath, InteractionMode.AutomatedAction);
                player.transform.position = new Vector3(0, 0.05f, -10);

                var toggle = Box("TestToggle", Vector3.zero, new Vector3(0.8f, 1.2f, 0.8f), indicator, null);
                toggle.AddComponent<TestToggleInteractable>().Configure(toggle.GetComponent<Renderer>());
                PrefabUtility.SaveAsPrefabAssetAndConnect(toggle, TogglePath, InteractionMode.AutomatedAction);
                toggle.name = "Tap Target A";
                toggle.transform.position = new Vector3(0, 1.2f, -6);
                var second = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TogglePath), scene);
                second.name = "Tap Target B (behind wall)";
                second.transform.position = new Vector3(4, 1.2f, 4);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone1-setup.txt", "Created " + ScenePath + "\n" + PlayerPath + "\n" + TogglePath);
                Debug.Log("Milestone 1 scene and prefabs created. Open " + ScenePath + " to playtest.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Material Material(string name, Shader shader, Color color)
        {
            var material = new Material(shader) { name = "M1_" + name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, "Assets/HerFootsteps/Materials/M1_" + name + ".mat");
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        [MenuItem("Her Footsteps/Milestone 1/Run Edit Mode Tests")]
        // Both milestones share these test assemblies; this also runs regression checks.
        public static void RunTests()
        {
            StartTests(TestMode.EditMode, "HerFootsteps.Tests.EditMode", "tests");
        }

        [MenuItem("Her Footsteps/Milestone 1/Run Play Mode Tests")]
        public static void RunPlayTests()
        {
            StartTests(TestMode.PlayMode, "HerFootsteps.Tests.PlayMode", "playtests");
        }

        private static void StartTests(TestMode mode, string assembly, string report)
        {
            SessionState.SetBool("HF.M1.TestsActive", true);
            SessionState.SetString("HF.M1.Report", report);
            RegisterRunner();
            runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = new[] { assembly }
            }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                var report = "Logs/Milestone1-" + SessionState.GetString("HF.M1.Report", "tests");
                TestRunnerApi.SaveResultToFile(result, report + ".xml");
                File.WriteAllText(report + ".txt",
                    $"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}\n{result.Message}");
                Debug.Log("Milestone 1 tests: " + result.ResultState);
                SessionState.SetBool("HF.M1.TestsActive", false);
                runner.UnregisterCallbacks(this);
                UnityEngine.Object.DestroyImmediate(runner);
            }
        }
    }
}
