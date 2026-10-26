using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class Milestone7Setup
    {
        [MenuItem("Her Footsteps/Presentation/Repair Milestone 7 Ramp Landings")]
        public static void RepairRampLandings()
        {
            if(EditorSceneManager.GetActiveScene().path!=ScenePath||File.Exists(Folder+"/RampClearance.txt"))throw new Exception("Initial ramp repair already completed or M7 not open.");
            string path=Folder+"/Prefabs/Watchtower.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            foreach(Transform t in prefab.transform){var p=t.localPosition;if(t.name=="Turn landing")p.z=Mathf.Sign(p.z)*8.5f;if(t.name=="Lookout deck")p.z=11;t.localPosition=p;}
            PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);Physics.SyncTransforms();
            var surface=Object.FindAnyObjectByType<NavMeshSurface>();var saved=surface.navMeshData;surface.BuildNavMesh();var rebuilt=surface.navMeshData;EditorUtility.CopySerialized(rebuilt,saved);EditorUtility.SetDirty(saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();Object.DestroyImmediate(rebuilt);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/RampClearance.txt","Initial ramp landing head clearance repaired.\n");AssetDatabase.Refresh();
        }
        [MenuItem("Her Footsteps/Presentation/Finish Milestone 7 Initial Terrain")]
        public static void FinishInitialTerrain()
        {
            if(EditorSceneManager.GetActiveScene().path!=ScenePath||File.Exists(Folder+"/InitialRefinement.txt"))throw new Exception("Initial refinement already completed or M7 not open; refusing to overwrite.");
            var t=Object.FindAnyObjectByType<Terrain>();var data=t.terrainData;int n=data.alphamapWidth;var alpha=new float[n,n,3];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {var p=new Vector2(280+x/(float)(n-1)*1400,1040-z/(float)(n-1)*1100);float d=PathDistance(p);float clearing=Clearings.Min(c=>Vector2.Distance(c,p))*.14f;float path=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Min(d,clearing*.45f)-1.5f)/1.2f));float corruption=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1170,1260,p.x));alpha[z,x,1]=path;alpha[z,x,2]=(1-path)*corruption;alpha[z,x,0]=(1-path)*(1-corruption);}
            data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();EditorUtility.SetDirty(data);foreach(var texture in data.alphamapTextures)EditorUtility.SetDirty(texture);t.Flush();
            var tower=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(o=>o.name=="Watchtower - accessible ramps");tower.position+=Vector3.forward*5;PrefabUtility.RecordPrefabInstancePropertyModifications(tower);
            var surface=Object.FindAnyObjectByType<NavMeshSurface>();var saved=surface.navMeshData;surface.BuildNavMesh();var rebuilt=surface.navMeshData;EditorUtility.CopySerialized(rebuilt,saved);EditorUtility.SetDirty(saved);surface.RemoveData();surface.navMeshData=saved;surface.AddData();Object.DestroyImmediate(rebuilt);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/InitialRefinement.txt","Initial terrain paint serialization and tower entry clearance completed. Do not rerun layout generation.\n");AssetDatabase.Refresh();
        }
        [MenuItem("Her Footsteps/Presentation/Inspect Milestone 7 Terrain")]
        public static void InspectTerrain()
        {
            var t=Object.FindAnyObjectByType<Terrain>();var data=t.terrainData;var a=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var text=new StringBuilder();
            text.AppendLine($"layers={data.terrainLayers.Length}; alphaTextures={data.alphamapTextures.Length}; terrain={t.transform.position}; trees={Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Length}");
            foreach(var p in new[]{new Vector2(790,900),new Vector2(600,800),new Vector2(1400,700)}){int x=Mathf.RoundToInt((p.x-280)/1400*(data.alphamapWidth-1)),z=Mathf.RoundToInt((1040-p.y)/1100*(data.alphamapHeight-1));text.AppendLine(p+": "+string.Join(",",Enumerable.Range(0,data.alphamapLayers).Select(i=>a[z,x,i])));}
            File.WriteAllText("Logs/Milestone7-terrain.txt",text.ToString());
        }
        [MenuItem("Her Footsteps/Presentation/Open Milestone 7")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
        [MenuItem("Her Footsteps/Presentation/Validate Milestone 7")]
        public static void Validate()
        {
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open M7 before validating.");
            Physics.SyncTransforms();var report=new StringBuilder();int failed=0;
            var motor=Object.FindAnyObjectByType<FirstPersonMotor>();var surface=Object.FindAnyObjectByType<NavMeshSurface>();surface.AddData();
            var targets=Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None).SelectMany(r=>r.GetComponentsInChildren<Transform>().Skip(1)).ToList();
            targets.AddRange(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Access marker"||t.name.StartsWith("Overlook marker")));
            NavMesh.SamplePosition(motor.transform.position,out var start,2,NavMesh.AllAreas);
            foreach(var target in targets)
            {
                var path=new NavMeshPath();bool sampled=NavMesh.SamplePosition(target.position,out var hit,2,NavMesh.AllAreas);
                bool complete=sampled&&NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                if(!complete)failed++;
                report.AppendLine((complete?"PASS ":"FAIL ")+target.parent.name+"/"+target.name+" "+target.position);
            }
            foreach(var spot in Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
            {
                bool clear=true;
                foreach(var point in new[]{spot.HidingPosition.position,spot.ExitPosition.position})
                    clear &= !Physics.OverlapCapsule(point+Vector3.up*.35f,point+Vector3.up*1.45f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider));
                report.AppendLine((clear?"PASS ":"FAIL ")+spot.name+" capsule clearance");if(!clear)failed++;
            }
            int missing=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));failed+=missing;
            report.AppendLine($"SUMMARY targets={targets.Count}, shelters={Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None).Length}, missing scripts={missing}, failures={failed}");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Milestone7-navigation.txt",report.ToString());Debug.Log("M7 navigation validation failures="+failed);
        }
        [MenuItem("Her Footsteps/Presentation/Export Milestone 7 Overview")]
        public static void ExportOverview()
        {
            if(EditorSceneManager.GetActiveScene().path!=ScenePath||EditorApplication.isPlaying)throw new Exception("Open M7 in Edit Mode before export.");
            var go=new GameObject("Temporary review camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=79;camera.aspect=1.4f;camera.transform.position=new Vector3(-1,230,67);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.nearClipPlane=.1f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.12f,.13f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rt=new RenderTexture(2240,1600,24,RenderTextureFormat.ARGB32);rt.Create();
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).ToArray();var intensities=lights.Select(l=>l.intensity).ToArray();
            bool fog=RenderSettings.fog;Color ambient=RenderSettings.ambientLight;var previous=RenderTexture.active;var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var canvasStates=canvases.Select(c=>c.enabled).ToArray();
            var labels=new GameObject("Temporary overview labels");
            var lods=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            var lodEnabled=lods.Select(l=>l.enabled).ToArray();
            var renderers=lods.SelectMany(l=>l.GetLODs().SelectMany(d=>d.renderers)).Where(r=>r!=null).Distinct().ToArray();var rendererEnabled=renderers.Select(r=>r.enabled).ToArray();
            var originalMaterials=renderers.Select(r=>r.sharedMaterials).ToArray();var captureMaterials=originalMaterials.SelectMany(m=>m).Where(m=>m!=null).Distinct().ToDictionary(m=>m,m=>new Material(m){enableInstancing=false});
            try
            {
                RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.65f,.68f,.7f);foreach(var light in lights)light.intensity=1.15f;foreach(var canvas in canvases)canvas.enabled=false;
                foreach(var lod in lods){lod.enabled=false;foreach(var level in lod.GetLODs())foreach(var r in level.renderers)if(r!=null)r.enabled=false;foreach(var r in lod.GetLODs()[0].renderers)if(r!=null)r.enabled=true;}
                for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originalMaterials[i].Select(m=>m==null?null:captureMaterials[m]).ToArray();
                Directory.CreateDirectory("Assets/Docs/LevelReviews");
                void Capture(string filename)
                {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("Assets/Docs/LevelReviews/"+filename,image.EncodeToPNG());Object.DestroyImmediate(image);}
                Capture("Milestone7_TopDown.png");
                void Label(string text,float u,float v)
                {var t=new GameObject(text).AddComponent<TextMesh>();t.transform.SetParent(labels.transform);t.transform.position=World(new Vector2(u,v),90);t.transform.rotation=Quaternion.Euler(90,0,0);t.text=text;t.fontSize=64;t.characterSize=.45f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.96f,.88f,.62f);}
                Label("SOUTH / START",790,980);Label("HUNTER'S CABIN",510,345);Label("ABANDONED TENT",700,715);Label("CENTRAL CAMP",880,750);Label("WATCHTOWER",1065,995);Label("SKELETON\nRESERVATION",810,130);Label("CRYPTID / SISTER\nRESERVATION",1340,390);Label("A: CORRUPTED LOOP",1460,740);Label("B: NOISE ROUTE",950,470);Label("NORTH  +Z",1060,55);
                Capture("Milestone7_TopDown_Labeled.png");
                labels.SetActive(false);camera.orthographic=false;camera.fieldOfView=38;Capture("Milestone7_PerspectiveReview.png");
                RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;for(int i=0;i<lights.Length;i++)lights[i].intensity=intensities[i];camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                var overlook=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name.StartsWith("Overlook marker"));camera.fieldOfView=65;camera.transform.position=overlook.position+Vector3.up*1.6f;var target=World(new Vector2(1340,390),16);camera.transform.LookAt(target);Capture("Milestone7_Watchtower.png");
            }
            finally
            {
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=rendererEnabled[i];for(int i=0;i<lods.Length;i++)lods[i].enabled=lodEnabled[i];
                for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originalMaterials[i];foreach(var mat in captureMaterials.Values)Object.DestroyImmediate(mat);
                RenderTexture.active=previous;RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;for(int i=0;i<lights.Length;i++)lights[i].intensity=intensities[i];for(int i=0;i<canvases.Length;i++)canvases[i].enabled=canvasStates[i];Object.DestroyImmediate(labels);Object.DestroyImmediate(go);rt.Release();Object.DestroyImmediate(rt);
            }
            AssetDatabase.Refresh();Debug.Log("M7 overview exported. Fog/lighting/canvas restored; no layout changed.");
        }
    }
}
