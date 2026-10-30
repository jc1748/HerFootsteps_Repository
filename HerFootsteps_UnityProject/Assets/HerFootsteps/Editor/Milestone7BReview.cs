using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static class Milestone7BReview
    {
        [MenuItem("Her Footsteps/Presentation/Validate Milestone 7B")]
        public static void Validate()
        {
            if(EditorSceneManager.GetActiveScene().path!=Milestone7BSetup.ScenePath)throw new Exception("Open M7B first");
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();Physics.SyncTransforms();
            var targets=Object.FindObjectsByType<BlockoutRoute>(FindObjectsSortMode.None).SelectMany(r=>r.GetComponentsInChildren<Transform>().Skip(1)).Concat(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Access marker"||t.name.StartsWith("Overlook marker"))).ToArray();
            var text=new StringBuilder();int failures=0;
            NavMesh.SamplePosition(player.transform.position,out var start,2,NavMesh.AllAreas);
            foreach(var t in targets)
            {
                var path=new NavMeshPath();bool okay=NavMesh.SamplePosition(t.position,out var hit,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                if(!okay)failures++;text.AppendLine((okay?"PASS ":"FAIL ")+t.parent.name+"/"+t.name);
            }
            var terrain=Object.FindAnyObjectByType<Terrain>();var data=terrain.terrainData;var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/HerFootsteps/Milestone7/ForestTerrain.asset");
            var a=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var b=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);float paintError=0;
            for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++)for(int l=0;l<data.alphamapLayers;l++)paintError=Mathf.Max(paintError,Mathf.Abs(a[z,x,l]-b[z,x,l]));
            var tower=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Watchtower - accessible ramps");
            int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var original=source.GetHeights(0,0,n,n);float outsideError=0;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {var p=terrain.transform.position+new Vector3(x/(float)(n-1)*data.size.x,0,z/(float)(n-1)*data.size.z);if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(tower.position.x,tower.position.z))>=23)outsideError=Mathf.Max(outsideError,Mathf.Abs(h[z,x]-original[z,x]));}
            if(paintError>.00001f||outsideError>.00001f)failures++;
            text.AppendLine($"Path paint max difference={paintError}; terrain difference outside tower radius={outsideError}");
            text.AppendLine($"Destinations={targets.Length}; failures={failures}; far clip={player.GetComponentInChildren<Camera>().farClipPlane}");
            File.WriteAllText("Logs/Milestone7B-navigation.txt",text.ToString());Debug.Log(text.ToString());
        }

        [MenuItem("Her Footsteps/Presentation/Export Milestone 7B Review")]
        public static void Export()
        {
            if(EditorSceneManager.GetActiveScene().path!=Milestone7BSetup.ScenePath||EditorApplication.isPlaying)throw new Exception("Open M7B in Edit Mode");
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();var source=player.GetComponentInChildren<Camera>();
            var go=new GameObject("Temporary review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);camera.aspect=16f/9;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var rt=new RenderTexture(1920,1080,24);rt.Create();var previous=RenderTexture.active;
            var beam=player.GetComponentsInChildren<Light>(true).First(l=>l.type==LightType.Spot);bool wasOn=beam.enabled;
            var lods=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);var lodOn=lods.Select(l=>l.enabled).ToArray();
            var renderers=lods.SelectMany(l=>l.GetLODs().SelectMany(d=>d.renderers)).Where(r=>r!=null).Distinct().ToArray();var enabled=renderers.Select(r=>r.enabled).ToArray();var materials=renderers.Select(r=>r.sharedMaterials).ToArray();
            var copies=materials.SelectMany(m=>m).Where(m=>m!=null).Distinct().ToDictionary(m=>m,m=>new Material(m){enableInstancing=false});
            bool fog=RenderSettings.fog;Color ambient=RenderSettings.ambientLight;var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).ToArray();var powers=lights.Select(l=>l.intensity).ToArray();
            try
            {
                foreach(var lod in lods){lod.enabled=false;foreach(var d in lod.GetLODs())foreach(var r in d.renderers)if(r!=null)r.enabled=false;foreach(var r in lod.GetLODs()[0].renderers)if(r!=null)r.enabled=true;}
                for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=materials[i].Select(m=>m==null?null:copies[m]).ToArray();
                Directory.CreateDirectory("Assets/Docs/LevelReviews");
                void Capture(string name)
                {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes("Assets/Docs/LevelReviews/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);}
                beam.enabled=false;Capture("Milestone7B_FlashlightOff");beam.enabled=true;Capture("Milestone7B_FlashlightOn");beam.enabled=false;
                var deck=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name.StartsWith("Overlook marker"));camera.transform.position=deck.position+Vector3.up*1.6f;camera.transform.LookAt(Object.FindAnyObjectByType<CryptidBrain>().transform.position+Vector3.up*10);Capture("Milestone7B_Watchtower");
                RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.65f,.68f,.7f);foreach(var l in lights)l.intensity=1.15f;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                camera.orthographic=true;camera.orthographicSize=80;camera.transform.position=new Vector3(-1,230,67);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.farClipPlane=300;Capture("Milestone7B_TopDown");
            }
            finally
            {
                beam.enabled=wasOn;RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;for(int i=0;i<lights.Length;i++)lights[i].intensity=powers[i];
                for(int i=0;i<renderers.Length;i++){renderers[i].sharedMaterials=materials[i];renderers[i].enabled=enabled[i];}for(int i=0;i<lods.Length;i++)lods[i].enabled=lodOn[i];foreach(var m in copies.Values)Object.DestroyImmediate(m);
                RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
            }
            AssetDatabase.Refresh();
        }
    }
}
