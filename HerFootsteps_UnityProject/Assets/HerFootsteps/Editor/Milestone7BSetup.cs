using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static class Milestone7BSetup
    {
        public const string ScenePath="Assets/Scenes/Milestone7B_ForestRefinement.unity";
        public const string Folder="Assets/HerFootsteps/Milestone7B";
        private static void Set(Object o,string field,object value)=>Milestone6Setup.Set(o,field,value);
        private static void ColorField(Object o,string field,Color value){var s=new SerializedObject(o);s.FindProperty(field).colorValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        [MenuItem("Her Footsteps/Presentation/Build Milestone 7B")]
        public static void Build()
        {
            if(File.Exists(ScenePath)||Directory.Exists(Folder))throw new InvalidOperationException("M7B exists; refusing to overwrite designer edits.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current designer edits first.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            AssetDatabase.CopyAsset(Milestone7Setup.ScenePath,ScenePath);
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var terrain=Object.FindAnyObjectByType<Terrain>();var original=terrain.terrainData;
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),Folder+"/ForestTerrain.asset"))throw new Exception("Terrain copy failed");
            terrain.terrainData=AssetDatabase.LoadAssetAtPath<TerrainData>(Folder+"/ForestTerrain.asset");terrain.GetComponent<TerrainCollider>().terrainData=terrain.terrainData;
            var material=new Material(terrain.materialTemplate);AssetDatabase.CreateAsset(material,Folder+"/ForestTerrain.mat");terrain.materialTemplate=material;
            RefineTowerGround(terrain,original);
            GroundLayers(terrain);
            var atmosphere=Object.FindAnyObjectByType<ForestAtmosphere>();
            Set(atmosphere,"fogDensity",.055f);Set(atmosphere,"moonIntensity",.065f);
            ColorField(atmosphere,"fogColor",new Color(.008f,.016f,.021f));ColorField(atmosphere,"ambientColor",new Color(.025f,.038f,.043f));atmosphere.Apply();
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if(volume.sharedProfile==null)continue;
                var path=Folder+"/"+volume.sharedProfile.name+".asset";
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),path);volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if(volume.sharedProfile.TryGet<ColorAdjustments>(out var grade)){grade.postExposure.Override(-.2f);grade.contrast.Override(18);grade.saturation.Override(-28);EditorUtility.SetDirty(grade);}
            }
            var player=Object.FindAnyObjectByType<FirstPersonMotor>();var camera=player.GetComponentInChildren<Camera>();
            var beam=player.GetComponentsInChildren<Light>(true).First(l=>l.type==LightType.Spot);
            var visuals=beam.gameObject.AddComponent<FlashlightVisualSettings>();Set(visuals,"beam",beam);visuals.Apply();PrefabUtility.RecordPrefabInstancePropertyModifications(beam);
            int vegetation=AddVegetation(terrain);
            var brain=Object.FindAnyObjectByType<CryptidBrain>();var hunt=Object.FindAnyObjectByType<HuntController>();
            var home=new GameObject("M7B cryptid home territory - editable").transform;home.position=brain.transform.position;
            var placement=hunt.gameObject.AddComponent<HuntPlacement>();Set(placement,"navigation",brain.GetComponent<CryptidNavigation>());Set(placement,"player",player.transform);Set(placement,"playerCamera",camera);Set(placement,"home",home);
            var entryRoot=new GameObject("M7B optional hunt entry markers - validate at runtime").transform;
            var markers=new List<Transform>();
            foreach(var spot in Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
            {var t=new GameObject("Behind "+spot.name).transform;t.SetParent(entryRoot);var p=spot.transform.position+spot.transform.forward*3;p.y=terrain.SampleHeight(p)+terrain.transform.position.y;t.position=p;markers.Add(t);}
            var so=new SerializedObject(placement);var array=so.FindProperty("entryMarkers");array.arraySize=markers.Count;for(int i=0;i<markers.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=markers[i];so.ApplyModifiedPropertiesWithoutUndo();
            Set(hunt,"placement",placement);Set(hunt,"individualNoiseTriggers",false);
            var exposure=player.gameObject.AddComponent<NoiseExposure>();Set(exposure,"player",player.transform);Set(exposure,"hunt",hunt);Set(exposure,"channel",AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset"));
            var canvas=player.GetComponentInChildren<Canvas>();
            var meter=new GameObject("Noise exposure meter",typeof(RectTransform),typeof(CanvasRenderer),typeof(NoiseExposureIndicator));
            var rect=meter.GetComponent<RectTransform>();rect.SetParent(canvas.transform,false);rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(150,78);rect.sizeDelta=new Vector2(130,24);Set(meter.GetComponent<NoiseExposureIndicator>(),"exposure",exposure);
            var label=new GameObject("Noise label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));var lr=label.GetComponent<RectTransform>();lr.SetParent(rect,false);lr.anchorMin=lr.anchorMax=new Vector2(0,0);lr.pivot=Vector2.zero;lr.anchoredPosition=new Vector2(0,-15);lr.sizeDelta=new Vector2(130,20);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=12;text.text="NOISE";text.color=new Color(.46f,.55f,.53f,.8f);text.raycastTarget=false;
            var surface=Object.FindAnyObjectByType<NavMeshSurface>();surface.RemoveData();surface.navMeshData=null;Physics.SyncTransforms();surface.BuildNavMesh();AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/ForestNavMesh.asset");
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var builds=EditorBuildSettings.scenes.ToList();builds.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=builds.ToArray();
            File.WriteAllText("Logs/Milestone7B-build.txt",$"Copied designer M7 scene. Independent terrain, layers, grading, NavMesh. Added {vegetation} small conifers. Camera far clip retained: {camera.farClipPlane}.\n");
        }
        private static void RefineTowerGround(Terrain terrain,TerrainData original)
        {
            var tower=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="Watchtower - accessible ramps");
            var data=terrain.terrainData;var origin=terrain.transform.position;int n=data.heightmapResolution;
            var h=data.GetHeights(0,0,n,n);var center=tower.position;
            float target=Mathf.Max(.5f,original.GetInterpolatedHeight((center.x-origin.x)/data.size.x,(center.z-origin.z)/data.size.z)-3);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var point=origin+new Vector3(x/(float)(n-1)*data.size.x,0,z/(float)(n-1)*data.size.z);
                float d=Vector2.Distance(new Vector2(point.x,point.z),new Vector2(center.x,center.z));
                if(d>=23)continue;
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(13,23,d));h[z,x]=Mathf.Lerp(h[z,x],target/data.size.y,blend);
            }
            data.SetHeights(0,0,h);
            var root=terrain.transform.parent;
            var move=new List<Transform>();
            foreach(Transform region in root)
            {
                if(!region.name.StartsWith("0")||region.name.StartsWith("08"))continue;
                foreach(Transform child in region)
                    if(child.name=="Trees"||region.name.StartsWith("07"))foreach(Transform nested in child)move.Add(nested);else move.Add(child);
            }
            foreach(var t in move)
            {
                var p=t.position;float u=(p.x-origin.x)/data.size.x,v=(p.z-origin.z)/data.size.z;
                float difference=data.GetInterpolatedHeight(u,v)-original.GetInterpolatedHeight(u,v);
                if(Mathf.Abs(difference)<.0001f)continue;t.position+=Vector3.up*difference;
                if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            }
            EditorUtility.SetDirty(data);
        }
        private static void GroundLayers(Terrain terrain)
        {
            var layers=terrain.terrainData.terrainLayers;int index=0;
            foreach(var original in layers.ToArray())
            {
                var layer=Object.Instantiate(original);layer.name=original.name+" M7B";
                // Albedo alpha is deliberately low smoothness, not opaque/near-mirror snow highlights.
                const int size=256;var albedo=new Texture2D(size,size,TextureFormat.RGBA32,true);var normal=new Texture2D(size,size,TextureFormat.RGBA32,true,true);
                var random=new System.Random(790+index);float[,] heights=new float[size,size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)heights[x,y]=Mathf.PerlinNoise(x*.12f,y*.12f)*.6f+(float)random.NextDouble()*.4f;
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float grain=heights[x,y],moss=Mathf.PerlinNoise(x*.025f+17,y*.025f+31);
                    Color soil=index==1?new Color(.20f,.15f,.095f):index==2?new Color(.09f,.10f,.095f):Color.Lerp(new Color(.13f,.095f,.055f),new Color(.09f,.14f,.065f),Mathf.SmoothStep(.4f,.75f,moss));
                    soil*=.7f+grain*.55f;if(grain>.83f)soil=Color.Lerp(soil,new Color(.24f,.19f,.11f),.6f);soil.a=.1f;albedo.SetPixel(x,y,soil);
                    float dx=heights[(x+1)%size,y]-heights[(x+size-1)%size,y],dy=heights[x,(y+1)%size]-heights[x,(y+size-1)%size];var v=new Vector3(-dx*.7f,-dy*.7f,1).normalized;
                    normal.SetPixel(x,y,new Color(v.x*.5f+.5f,v.y*.5f+.5f,v.z*.5f+.5f,v.x*.5f+.5f));
                }
                albedo.Apply();normal.Apply();AssetDatabase.CreateAsset(albedo,Folder+"/Ground"+index+"Albedo.asset");AssetDatabase.CreateAsset(normal,Folder+"/Ground"+index+"Normal.asset");
                layer.diffuseTexture=albedo;layer.normalMapTexture=normal;layer.normalScale=.35f;layer.tileSize=new Vector2(index==1?4.7f:6.3f,index==1?4.7f:6.3f);layer.metallic=0;layer.smoothness=.1f;
                AssetDatabase.CreateAsset(layer,Folder+"/Ground"+index+".terrainlayer");layers[index++]=layer;
            }
            terrain.terrainData.terrainLayers=layers;terrain.terrainData.SetBaseMapDirty();EditorUtility.SetDirty(terrain.terrainData);terrain.Flush();
        }
        private static int AddVegetation(Terrain terrain)
        {
            var data=terrain.terrainData;var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var origin=terrain.transform.position;
            var anchors=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(l=>l.transform.parent!=null&&l.transform.parent.name=="Trees").ToArray();
            var protect=Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None).SelectMany(h=>new[]{h.HidingPosition.position,h.ExitPosition.position}).Concat(Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).Select(i=>i.transform.position)).ToArray();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Small BOTD URP.prefab");
            var groups=new Dictionary<Transform,Transform>();var random=new System.Random(728);int count=0;
            foreach(var anchor in anchors)
            {
                if(count>=420)break;float a=(float)random.NextDouble()*Mathf.PI*2;var p=anchor.transform.position+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*2;
                float u=(p.x-origin.x)/data.size.x,v=(p.z-origin.z)/data.size.z;if(u<0||u>1||v<0||v>1)continue;
                int x=Mathf.RoundToInt(u*(data.alphamapWidth-1)),z=Mathf.RoundToInt(v*(data.alphamapHeight-1));
                if(paint[z,x,1]>.12f||data.GetSteepness(u,v)>32||protect.Any(q=>Vector3.Distance(p,q)<4))continue;
                p.y=terrain.SampleHeight(p)+origin.y;Physics.SyncTransforms();
                if(Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*3,.85f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)))continue;
                var region=anchor.transform.parent.parent;
                if(!groups.TryGetValue(region,out var group)){group=new GameObject("M7B understory - individually editable").transform;group.SetParent(region,false);groups.Add(region,group);}
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group);tree.name="Small conifer understory";tree.transform.position=p;tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);tree.transform.localScale=Vector3.one*Mathf.Lerp(.22f,.38f,(float)random.NextDouble());
                foreach(var c in tree.GetComponentsInChildren<Collider>()){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
                PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);count++;
            }
            return count;
        }
        [MenuItem("Her Footsteps/Presentation/Open Milestone 7B")]
        public static void Open()=>EditorSceneManager.OpenScene(ScenePath);
    }
}
