using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static class Milestone7CAudit
    {
        [MenuItem("Her Footsteps/Presentation/Audit Milestone 7C")]
        public static void Audit()
        {
            var report = new StringBuilder();
            var scene = EditorSceneManager.GetActiveScene();
            report.AppendLine($"Scene: {scene.path}; dirty: {scene.isDirty}; fog: {RenderSettings.fog} {RenderSettings.fogMode} {RenderSettings.fogDensity}");
            foreach (var e in Object.FindObjectsByType<HallucinationEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                report.AppendLine($"{e.name}: enabled={e.isActiveAndEnabled}, automatic={e.Automatic}, position={e.transform.position}\n{EditorJsonUtility.ToJson(e)}");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vegetation/Models/Vegetation.fbx");
            if (model != null) foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
                report.AppendLine($"MODEL {r.name} bounds={r.bounds.size} mesh={r.GetComponent<MeshFilter>()?.sharedMesh?.name} MATERIALS " + string.Join("; ",r.sharedMaterials.Select(m=>m==null?"NULL":$"{m.name}/{m.shader.name}/texture={m.mainTexture?.name}")));
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Milestone7C-audit.txt", report.ToString());
        }
    }
}
