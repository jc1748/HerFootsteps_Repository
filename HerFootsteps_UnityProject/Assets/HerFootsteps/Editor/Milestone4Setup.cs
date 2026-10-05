using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class Milestone4Setup
    {
        public const string ScenePath = "Assets/Scenes/Milestone4_Test.unity";
        public const string CryptidPath = "Assets/HerFootsteps/Prefabs/TestCryptid.prefab";
        public const string NavMeshPath = "Assets/HerFootsteps/Settings/Milestone4NavMesh.asset";
        private const string RequestPath = "Library/HerFootsteps/Milestone4.request";
        static Milestone4Setup() => EditorApplication.update += CheckRequest;

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
                else throw new InvalidOperationException("Unknown Milestone 4 request: " + command);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone4-setup-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }
        [MenuItem("Her Footsteps/Milestone 4/Open Test Scene")]
        public static void OpenTestScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Her Footsteps/Milestone 4/Run All Milestone Edit Mode Tests")]
        public static void RunTests() => Milestone1Setup.RunTests();
        [MenuItem("Her Footsteps/Milestone 4/Run All Milestone Play Mode Tests")]
        public static void RunPlayTests() => Milestone1Setup.RunPlayTests();

        [MenuItem("Her Footsteps/Milestone 4/Create Test Scene (once)")]
        public static void CreateTestScene()
        {
            foreach (string path in new[] { ScenePath, CryptidPath, NavMeshPath })
                if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            if (!AssetDatabase.CopyAsset(Milestone3Setup.ScenePath, ScenePath))
                throw new InvalidOperationException("Could not copy saved Milestone 3 scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var player = roots.SelectMany(root => root.GetComponentsInChildren<FirstPersonMotor>(true)).Single();
                // Collect only this scene's environment hierarchy, not another open scene or player.
                var geometry = new GameObject("Milestone 4 Navigation Geometry");
                foreach (var root in roots)
                    if (root != player.gameObject) root.transform.SetParent(geometry.transform, true);
                var surface = geometry.AddComponent<NavMeshSurface>();
                surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = Physics.DefaultRaycastLayers;
                surface.overrideVoxelSize = true;
                surface.voxelSize = 0.1f;
                surface.BuildNavMesh();
                if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh bake produced no data.");
                AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);

                var cryptid = new GameObject("TestCryptid");
                var agent = cryptid.AddComponent<NavMeshAgent>();
                agent.agentTypeID = surface.agentTypeID;
                agent.radius = 0.5f; agent.height = 2; agent.baseOffset = 0;
                agent.speed = 2.5f; agent.acceleration = 12; agent.angularSpeed = 240;
                agent.stoppingDistance = 0.8f;
                agent.autoBraking = true;
                agent.autoTraverseOffMeshLink = false;
                cryptid.AddComponent<CryptidNavigation>();
                var senses = cryptid.AddComponent<CryptidSenses>();
                var brain = cryptid.AddComponent<CryptidBrain>();
                brain.Configure(AssetDatabase.LoadAssetAtPath<NoiseChannel>(Milestone2Setup.ChannelPath));
                cryptid.AddComponent<CryptidDebugView>();
                var eyes = new GameObject("Eyes").transform;
                eyes.SetParent(cryptid.transform, false); eyes.localPosition = new Vector3(0, 1.7f, 0);
                senses.Configure(null, null, eyes);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/HerFootsteps/Materials/M1_Indicator.mat");
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Replaceable Capsule Visual";
                visual.transform.SetParent(cryptid.transform, false);
                visual.transform.localPosition = Vector3.up;
                visual.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                var facing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                facing.name = "Forward Indicator";
                facing.transform.SetParent(visual.transform, false);
                facing.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
                facing.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
                facing.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(facing.GetComponent<Collider>());
                PrefabUtility.SaveAsPrefabAssetAndConnect(cryptid, CryptidPath, InteractionMode.AutomatedAction);
                cryptid.transform.position = new Vector3(0, 0, 5);
                senses.Configure(player, player.GetComponentInChildren<Camera>().transform, eyes);
                PrefabUtility.RecordPrefabInstancePropertyModifications(senses);

                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(cryptid.transform.position, out var spawn, 1, filter))
                    throw new InvalidOperationException("Cryptid spawn is not on the baked NavMesh.");
                cryptid.transform.position = spawn.position;
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Milestone4-setup.txt", "Created M4 scene, cryptid prefab and baked NavMesh. Existing player/prefabs preserved.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
