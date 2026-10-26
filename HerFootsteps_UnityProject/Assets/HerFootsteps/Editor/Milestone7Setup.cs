using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class Milestone7Setup
    {
        public const string ScenePath="Assets/Scenes/Milestone7_ForestBlockout.unity";
        public const string Folder="Assets/HerFootsteps/Milestone7";
        // Hand-traced from the designer's 1920x1080 image; north is +Z, 0.14m per pixel.
        private static readonly Vector2[][] Routes={
            Points(790,940, 800,860, 790,790, 860,765, 880,720),
            Points(790,790, 700,760, 610,750, 590,680, 610,620, 580,560, 520,550),
            Points(610,750, 690,740, 700,680),
            Points(580,560, 620,505, 600,450, 570,410, 510,365),
            Points(510,365, 600,375, 700,380, 750,430, 700,510, 735,570, 780,600, 830,660, 880,720),
            Points(620,505, 700,510),
            Points(750,430, 840,420, 930,415, 980,430, 990,500, 980,535, 930,560, 880,610, 780,600),
            Points(880,720, 950,770, 1010,810, 1065,920),
            Points(880,610, 1020,615, 1100,650, 1170,700, 1160,770, 1100,780, 1010,810),
            Points(1100,650, 1150,600, 1240,610, 1290,680, 1280,800, 1300,880, 1410,910, 1460,820, 1450,700, 1400,590, 1440,510, 1450,390, 1400,310, 1310,285, 1240,275),
            Points(980,430, 980,320, 1070,290, 1150,285, 1240,275),
            Points(980,320, 910,300, 850,290, 800,240, 810,150),
            Points(810,150, 870,210, 910,240, 925,270, 980,320),
            Points(1240,275, 1205,350, 1200,450, 1250,510, 1340,520, 1390,470, 1400,420, 1340,390)
        };
        private static readonly string[] RouteNames={"01 South entrance","02 Southwest loop","03 Abandoned tent spur","04 Cabin west approach","05 Cabin to central fork","06 West crosslink","07 Route B - dead animals","08 Campsite to watchtower","09 Central east crosslink","10 Route A - corrupted eastern loop","11 Route B - northern approach","12 Skeleton western approach","13 Skeleton disturbed trail","14 Territory outer approach"};
        private static readonly Vector2[] Outline=Points(340,400,380,250,460,160,640,110,800,60,1030,80,1140,160,1200,235,1390,230,1490,310,1520,490,1500,560,1550,690,1570,950,1490,1000,1130,990,800,985,620,960,470,890,420,750,390,590);
        private static readonly Vector2[] Clearings=Points(790,940,510,365,700,680,880,720,1065,920,810,150,1340,390);
        private static Transform environment, guides;
        private static Transform[] regions;
        private static Terrain terrain;
        private static FirstPersonMotor player;
        private static Material wood, stone, cloth, dead, bone;
        private static System.Random rng;
        private static readonly List<BoxCollider> safeVolumes=new List<BoxCollider>();
        private static Vector2[] Points(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
        private static Vector3 World(Vector2 p,float y=0)=>new Vector3((p.x-960)*.14f,y,(1000-p.y)*.14f);
        private static Vector3 At(float u,float v,float extra=0){var p=World(new Vector2(u,v));p.y=terrain.SampleHeight(p)+terrain.transform.position.y+extra;return p;}
        private static float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
        private static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)*d);}
        private static float PathDistance(Vector2 p){float d=99999;foreach(var route in Routes)for(int i=1;i<route.Length;i++)d=Mathf.Min(d,Segment(p,route[i-1],route[i]));return d*.14f;}
        private static bool Inside(Vector2 p){bool yes=false;for(int i=0,j=Outline.Length-1;i<Outline.Length;j=i++)if((Outline[i].y>p.y)!=(Outline[j].y>p.y)&&p.x<(Outline[j].x-Outline[i].x)*(p.y-Outline[i].y)/(Outline[j].y-Outline[i].y)+Outline[i].x)yes=!yes;return yes;}
        private static Transform Region(Vector2 p)=>regions[p.x>1190?5:p.x>1000&&p.y>650?4:p.y<340?3:p.x<690?1:p.y>800?0:2];
        private static void Set(Object target,string field,object value)=>Milestone6Setup.Set(target,field,value);
        private static void SetArray(Object target,string field,Object[] values){var s=new SerializedObject(target);var p=s.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}

        [MenuItem("Her Footsteps/Presentation/Build Milestone 7")]
        public static void Build()
        {
            if(File.Exists(ScenePath)||Directory.Exists(Folder))throw new InvalidOperationException("M7 already exists. Refusing to overwrite designer edits.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before building M7.");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Folder+"/Prefabs");AssetDatabase.Refresh();
            AssetDatabase.CopyAsset(Milestone6Setup.ScenePath,ScenePath);
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            player=Object.FindAnyObjectByType<FirstPersonMotor>();var brain=Object.FindAnyObjectByType<CryptidBrain>();
            // Remove only the copied old forest, retaining player, AI, hunt, audio and hallucinations.
            var oldSurface=Object.FindAnyObjectByType<NavMeshSurface>();Object.DestroyImmediate(oldSurface.gameObject);
            environment=new GameObject("M7 Forest Blockout").transform;
            regions=new[]{"01 South - starting area","02 West - cabin and abandoned tent","03 Central - campsite and branches","04 North - skeleton and noise route","05 Southeast - watchtower","06 East - corrupted forest and territory"}.Select(n=>Child(n,environment)).ToArray();
            guides=Child("07 Design guides - select routes for gizmos",environment);rng=new System.Random(707);safeVolumes.Clear();
            wood=LoadMat("Weathered wood");stone=LoadMat("Damp stone");cloth=LoadMat("Faded cloth");dead=LoadMat("Branch silhouette");
            bone=new Material(Shader.Find("Universal Render Pipeline/Lit"));bone.SetColor("_BaseColor",new Color(.43f,.42f,.34f));AssetDatabase.CreateAsset(bone,Folder+"/Bone placeholder.mat");
            MakeTerrain();MakeRoutes();MakePrefabs();PlaceLandmarks();PlaceGameplay();MakeTrees();MakeBoundary();
            player.transform.position=At(790,940,.12f);player.transform.rotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            var camera=player.GetComponentInChildren<Camera>();camera.farClipPlane=240;PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            brain.transform.position=At(1340,390,.1f);brain.transform.rotation=Quaternion.Euler(0,200,0);PrefabUtility.RecordPrefabInstancePropertyModifications(brain.transform);
            // Longer paths on the new map need a travel timeout adequate for its scale; AI architecture unchanged.
            Set(brain,"travelTimeout",90f);
            SetArray(player.GetComponent<ComposureRateSource>(),"suppressInside",safeVolumes.Cast<Object>().ToArray());
            var atmosphere=Object.FindAnyObjectByType<ForestAtmosphere>();Set(atmosphere,"fogDensity",.0145f);atmosphere.Apply();
            var wildlife=Object.FindAnyObjectByType<WildlifeHallucination>();wildlife.transform.position=At(985,535);wildlife.transform.rotation=Quaternion.Euler(0,180,0);
            // Existing visual child remains associated with wildlife's root.
            var phantom=Object.FindAnyObjectByType<PhantomFootstepsHallucination>();Set(phantom,"automatic",false);
            var surface=environment.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=Physics.DefaultRaycastLayers;surface.overrideVoxelSize=true;surface.voxelSize=.18f;
            Physics.SyncTransforms();surface.BuildNavMesh();if(surface.navMeshData==null)throw new Exception("M7 NavMesh did not build.");AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/ForestNavMesh.asset");
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(s=>s.path==ScenePath)){builds.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=builds.ToArray();}
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Milestone7-build.txt","M7 saved. Hand-traced map; native terrain and modular landmarks. Explicit builder only.\n");
        }
        private static Material LoadMat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(PresentationSetup.Folder+"/"+name+".mat");
        private static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static float Height(Vector2 p)
        {
            float x=(p.x-960)*.14f,z=(1000-p.y)*.14f;
            float broad=2.5f+1.0f*Mathf.Sin(x*.035f)+1.1f*Mathf.Sin(z*.055f);
            float tower=3.2f*Mathf.Exp(-((p.x-1065)*(p.x-1065)+(p.y-920)*(p.y-920))/18000f);
            float northeast=3f*Mathf.Exp(-((p.x-1340)*(p.x-1340)+(p.y-390)*(p.y-390))/42000f);
            float detail=(Mathf.PerlinNoise(p.x*.014f,p.y*.014f)-.5f)*2;
            float clear=Clearings.Min(c=>Vector2.Distance(c,p))*.14f;
            float result=broad+tower+northeast+detail*Mathf.SmoothStep(0,1,Mathf.Min(PathDistance(p)/5,clear/8));
            foreach(var c in Clearings)
            {
                float distance=Vector2.Distance(c,p)*.14f, radius=c.x==1065?16:6;
                if(distance>radius+3)continue;
                float cx=(c.x-960)*.14f,cz=(1000-c.y)*.14f;
                float center=2.5f+Mathf.Sin(cx*.035f)+1.1f*Mathf.Sin(cz*.055f)+3.2f*Mathf.Exp(-((c.x-1065)*(c.x-1065)+(c.y-920)*(c.y-920))/18000f)+3f*Mathf.Exp(-((c.x-1340)*(c.x-1340)+(c.y-390)*(c.y-390))/42000f);
                result=Mathf.Lerp(center,result,Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+3,distance)));
            }
            return result;
        }
        private static void MakeTerrain()
        {
            var data=new TerrainData{heightmapResolution=513,alphamapResolution=512,size=new Vector3(196,24,154),baseMapResolution=512};
            var heights=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){var p=new Vector2(280+x/512f*1400,1040-z/512f*1100);heights[z,x]=Height(p)/24f;}
            data.SetHeights(0,0,heights);AssetDatabase.CreateAsset(data,Folder+"/ForestTerrain.asset");
            var layers=new List<TerrainLayer>();
            foreach(var pair in new[]{("Forest floor",new Color(.16f,.22f,.18f)),("Worn trail",new Color(.34f,.31f,.25f)),("Corrupted ash",new Color(.19f,.21f,.23f))})
            {
                var texture=new Texture2D(64,64);for(int y=0;y<64;y++)for(int x=0;x<64;x++)texture.SetPixel(x,y,pair.Item2*Rand(.8f,1.15f));texture.Apply();AssetDatabase.CreateAsset(texture,Folder+"/"+pair.Item1+".asset");
                var layer=new TerrainLayer{diffuseTexture=texture,tileSize=new Vector2(4,4)};AssetDatabase.CreateAsset(layer,Folder+"/"+pair.Item1+".terrainlayer");layers.Add(layer);
            }
            data.terrainLayers=layers.ToArray();var alpha=new float[512,512,3];
            for(int z=0;z<512;z++)for(int x=0;x<512;x++)
            {var p=new Vector2(280+x/511f*1400,1040-z/511f*1100);float d=PathDistance(p);float clearing=Clearings.Min(c=>Vector2.Distance(c,p))*.14f;float path=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Min(d,clearing*.45f)-1.5f)/1.2f));float corruption=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1170,1260,p.x));alpha[z,x,1]=path;alpha[z,x,2]=(1-path)*corruption;alpha[z,x,0]=(1-path)*(1-corruption);}
            data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);foreach(var texture in data.alphamapTextures)EditorUtility.SetDirty(texture);
            var go=Terrain.CreateTerrainGameObject(data);go.name="Editable Unity Terrain - sculpt and paint";go.transform.SetParent(environment,false);go.transform.position=World(new Vector2(280,1040));terrain=go.GetComponent<Terrain>();terrain.heightmapPixelError=5;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(mat,Folder+"/ForestTerrain.mat");terrain.materialTemplate=mat;
        }
        private static void MakeRoutes()
        {
            for(int r=0;r<Routes.Length;r++){var root=Child(RouteNames[r],guides);var route=root.gameObject.AddComponent<BlockoutRoute>();route.designIntent="Map-traced guide only. Sculpt/paint terrain and move props manually; moving nodes does not rebuild.";for(int i=0;i<Routes[r].Length;i++)Child("Node "+i,root).position=At(Routes[r][i].x,Routes[r][i].y);}
        }
        private static void MakeTrees()
        {
            string directory="Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/";
            var prefabs=new[]{"Tall","Medium","Bare"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(directory+"PF Conifer "+n+" BOTD URP.prefab")).ToArray();
            for(float v=90;v<985;v+=27)for(float u=360;u<1550;u+=28)
            {
                var p=new Vector2(u+Rand(-8,8),v+Rand(-8,8));if(!Inside(p)||PathDistance(p)<3.4f)continue;
                if(Clearings.Any(c=>Vector2.Distance(c,p)*.14f<(c.x==1065?14:7)))continue;
                Physics.SyncTransforms();
                var basePosition=At(p.x,p.y);
                if(Physics.OverlapCapsule(basePosition+Vector3.up,basePosition+Vector3.up*12,.75f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)))continue;
                var parent=Region(p);var forest=parent.Find("Trees");if(forest==null)forest=Child("Trees",parent);
                bool corrupted=p.x>1200;
                GameObject tree;
                if(corrupted)tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/CorruptedTree.prefab"),forest);
                else tree=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[rng.Next(prefabs.Length)],forest);
                tree.transform.position=At(p.x,p.y);tree.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);tree.transform.localScale=Vector3.one*(corrupted?Rand(.85f,1.35f):Rand(.38f,.56f));
                if(!corrupted){foreach(var c in tree.GetComponentsInChildren<Collider>())c.enabled=false;var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.5f;trunk.height=18;trunk.center=Vector3.up*9;}
                PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
                foreach(var c in tree.GetComponentsInChildren<Collider>())if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
        }
        private static void MakeBoundary()
        {
            var boundary=Child("08 Natural boundary - editable rock ridge",environment);
            for(int i=0;i<Outline.Length;i++)
            {var a=Outline[i];var b=Outline[(i+1)%Outline.Length];int count=Mathf.CeilToInt(Vector2.Distance(a,b)*.14f/3);for(int j=0;j<count;j++){var p=Vector2.Lerp(a,b,j/(float)count);var o=Shape("Boundary rock",PrimitiveType.Sphere,At(p.x,p.y,2),new Vector3(5,6,5),stone,boundary);}}
        }
    }
}

