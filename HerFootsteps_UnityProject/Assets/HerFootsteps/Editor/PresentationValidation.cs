using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class PresentationSetup
    {
        [MenuItem("Her Footsteps/Presentation/Refine Colliders")]
        public static void RefineColliders()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var surface=Object.FindAnyObjectByType<NavMeshSurface>();
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();Set(player.GetComponent<Milestone5Hud>(),"controlHint","F3 debug | F4 -25 | F5 +25 | F7 trail | F8 wildlife");
            foreach(var sphere in surface.GetComponentsInChildren<SphereCollider>())
            {var go=sphere.gameObject;Object.DestroyImmediate(sphere);go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;}
            var saved=surface.navMeshData;surface.BuildNavMesh();var rebuilt=surface.navMeshData;
            surface.RemoveData();EditorUtility.CopySerialized(rebuilt,saved);EditorUtility.SetDirty(saved);surface.navMeshData=saved;surface.AddData();Object.DestroyImmediate(rebuilt);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Validate();
        }
        [MenuItem("Her Footsteps/Presentation/Validate")]
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Open presentation scene first.");
            var roots=scene.GetRootGameObjects();var renderers=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToArray();
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
            var report=new System.Text.StringBuilder();report.AppendLine("Scene: "+scene.path);report.AppendLine("Renderers: "+renderers.Length);
            foreach(var material in materials){bool error=material.shader==null||ShaderUtil.ShaderHasError(material.shader);report.AppendLine(material.name+" | "+(material.shader!=null?material.shader.name:"missing")+" | shader errors: "+error);if(error)throw new Exception("Invalid material "+material.name);}
            int missing=roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));report.AppendLine("Missing scripts: "+missing);if(missing>0)throw new Exception("Missing scripts");
            var player=roots.SelectMany(r=>r.GetComponentsInChildren<FirstPersonMotor>(true)).Single();var globals=player.GetComponents<ComposureRateSource>();report.AppendLine("Player passive sources: "+globals.Length);if(globals.Length!=1)throw new Exception("Duplicate player passive source");
            File.WriteAllText("Logs/Presentation-validation.txt",report.ToString());
        }
    }
}
