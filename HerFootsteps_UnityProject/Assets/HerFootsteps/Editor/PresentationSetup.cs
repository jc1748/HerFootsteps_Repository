using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class PresentationSetup
    {
        public const string Folder = "Assets/HerFootsteps/Presentation";
        public const string ScenePath = "Assets/Scenes/Milestone5_Presentation.unity";
        private const string Trees = "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/";
        private static Material ground, bark, stone, cloth, dark, metal;
        private static Transform environment;
        private static System.Random random;
        private static readonly Vector2[] Main = { new Vector2(0,-30), new Vector2(-3,-18), new Vector2(0,-5), new Vector2(5,10), new Vector2(0,26) };
        private static readonly Vector2[] Branch = { new Vector2(-3,-18), new Vector2(-15,-8), new Vector2(-16,9), new Vector2(-8,17), new Vector2(5,10) };
        [MenuItem("Her Footsteps/Presentation/Build")]
        public static void Build()
        {
            if (File.Exists(ScenePath) || Directory.Exists(Folder)) throw new InvalidOperationException("Presentation content exists; refusing to overwrite it.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Trees + "PF Conifer Tall BOTD URP.prefab") == null) throw new InvalidOperationException("Import the supplied Conifers URP package first.");
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) throw new InvalidOperationException("Expected the project's current URP pipeline.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); random = new System.Random(510);
            AssetDatabase.CopyAsset(Milestone5Setup.ScenePath, ScenePath);
            var previous = SceneManager.GetActiveScene(); var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var player = roots.SelectMany(r => r.GetComponentsInChildren<FirstPersonMotor>(true)).Single();
                var brain = roots.SelectMany(r => r.GetComponentsInChildren<CryptidBrain>(true)).Single();
                var trail = roots.SelectMany(r => r.GetComponentsInChildren<FalseTrailHallucination>(true)).Single();
                var wildlife = roots.SelectMany(r => r.GetComponentsInChildren<WildlifeHallucination>(true)).Single();
                var oldSurface = roots.SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>(true)).Single();
                // Only the new scene copy's obsolete test geometry is replaced.
                Object.DestroyImmediate(oldSurface.gameObject);
                foreach (var light in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray())
                    if (!light.transform.IsChildOf(player.transform)) Object.DestroyImmediate(light.gameObject);
                environment = new GameObject("Forest - editable environment").transform;
                MakeMaterials(); MakeGround(); MakeTrees(); MakeCamp();
                var surface = environment.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children; surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = Physics.DefaultRaycastLayers; surface.overrideVoxelSize = true; surface.voxelSize = 0.12f;
                var composure = player.GetComponent<PlayerComposure>(); var inventory = player.GetComponent<PlayerInventory>();
                var input = player.GetComponent<PlayerInputReader>();
                var actionSource = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                var actions = InputActionAsset.FromJson(actionSource.ToJson());
                var map = actions.FindActionMap("Player"); map.FindAction("Inventory")?.RemoveAction();
                for (int i = 1; i <= 5; i++) map.AddAction("Slot" + i, InputActionType.Button).AddBinding("<Keyboard>/digit" + i, groups:"Keyboard&Mouse");
                map.AddAction("UseItem", InputActionType.Button).AddBinding("<Keyboard>/r", groups:"Keyboard&Mouse");
                map.AddAction("Map", InputActionType.Button).AddBinding("<Keyboard>/tab", groups:"Keyboard&Mouse");
                File.WriteAllText(Folder + "/PresentationInput.inputactions", actions.ToJson()); Object.DestroyImmediate(actions); AssetDatabase.Refresh();
                Set(input, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(Folder + "/PresentationInput.inputactions"));
                player.GetComponent<InventoryView>().enabled = false;
                player.GetComponent<DiscoveryComposureRecovery>().enabled = false;
                player.transform.position = At(0,-29); player.transform.rotation = Quaternion.identity;
                var camera = player.GetComponentInChildren<Camera>(); camera.farClipPlane = 100; camera.backgroundColor = new Color(0.085f,0.125f,0.14f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                var beam = player.GetComponentInChildren<Light>(); beam.range = 19; beam.spotAngle = 46; beam.innerSpotAngle = 28; beam.intensity = 35; beam.color = new Color(0.85f,0.9f,0.82f); beam.shadows = LightShadows.Soft;
                Set(player.GetComponent<PlayerFlashlight>(), "startsOn", true);
                var safe = SafeArea(composure); var passive = player.GetComponent<ComposureRateSource>();
                Set(passive, "sourceActive", true); Set(passive, "ratePerSecond", -0.5f); SetArray(passive, "suppressInside", new Object[] { safe });
                var threat = player.GetComponent<CryptidComposureSource>(); Set(threat,"proximityEnabled",true); Set(threat,"pursuitEnabled",true); Set(threat,"detectionEnabled",true);
                MakeClue(); MakeSupplies(); MakeHiding(); MakeHazards();
                RebuildHallucinations(trail, wildlife);
                var capsule = brain.transform.Find("Temporary Visual");
                foreach (var renderer in brain.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                var silhouette = Creature("Cryptid silhouette - replaceable", brain.transform, false); silhouette.transform.localPosition = Vector3.zero;
                brain.transform.position = At(5,21); brain.transform.rotation = Quaternion.Euler(0,200,0);
                brain.GetComponent<CryptidSenses>().Configure(player, camera.transform, brain.transform.Find("Eyes"));
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain.GetComponent<CryptidSenses>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain.transform);
                foreach (var r in brain.GetComponentsInChildren<Renderer>()) if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                MakeAtmosphere(); MakeUi(player);
                // Save a separate player variant. Scene references remain overrides.
                PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, Folder + "/PresentationPlayer.prefab", InteractionMode.AutomatedAction);
                // Reassign scene-only references after prefab save.
                SetArray(passive,"suppressInside",new Object[]{safe}); Set(threat,"cryptid",brain);
                Set(player.GetComponent<Milestone5Hud>(),"trail",trail); Set(player.GetComponent<Milestone5Hud>(),"wildlife",wildlife);
                var debug = player.GetComponent<PresentationDebugMode>();
                SetArray(debug,"panels",new Object[]{player.GetComponent<PrototypeHud>(),player.GetComponent<Milestone2Hud>(),player.GetComponent<Milestone3Hud>(),player.GetComponent<Milestone5Hud>(),brain.GetComponent<CryptidDebugView>()});
                surface.BuildNavMesh(); if(surface.navMeshData==null)throw new InvalidOperationException("Forest NavMesh failed"); AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/ForestNavMesh.asset");
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/Presentation-build.txt", "Built presentation scene from M5; URP trees, separate terrain/NavMesh, Canvas HUD and player variant. Seed 510 editor layout only.");
            }
            finally { if(previous.IsValid())SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
        private static void Set(Object target, string field, object value)
        {
            var so=new SerializedObject(target); var p=so.FindProperty(field); if(p==null)throw new Exception(target.name+" missing "+field);
            if(value is bool b)p.boolValue=b; else if(value is float f)p.floatValue=f; else if(value is int i)p.intValue=i; else if(value is string s)p.stringValue=s; else p.objectReferenceValue=value as Object;
            so.ApplyModifiedPropertiesWithoutUndo(); if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        private static void SetArray(Object target,string field,Object[] values)
        { var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
        private static float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        private static float RouteDistance(Vector2 point,Vector2[] route)
        { float result=999;for(int i=1;i<route.Length;i++){var v=route[i]-route[i-1];float t=Mathf.Clamp01(Vector2.Dot(point-route[i-1],v)/v.sqrMagnitude);result=Mathf.Min(result,Vector2.Distance(point,route[i-1]+t*v));}return result; }
        private static float PathDistance(float x,float z)=>Mathf.Min(RouteDistance(new Vector2(x,z),Main),RouteDistance(new Vector2(x,z),Branch));
        private static float Height(float x,float z)=>0.2f*Mathf.Sin(z*0.09f)+Mathf.SmoothStep(0,1,Mathf.Clamp01((PathDistance(x,z)-2)/6))*(Mathf.PerlinNoise(x*.065f+8,z*.065f+8)*2.4f-0.7f);
        private static Vector3 At(float x,float z,float y=0)=>new Vector3(x,Height(x,z)+y,z);
        private static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat, Transform parent, bool collider=true)
        {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(o.GetComponent<Collider>());else if(type==PrimitiveType.Sphere){Object.DestroyImmediate(o.GetComponent<Collider>());o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;}return o;}
        private static Material Mat(string name,Color color,float smooth=0)
        {var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
        private static void MakeMaterials()
        {
            ground=Mat("Forest soil",new Color(.38f,.4f,.35f)); bark=Mat("Weathered wood",new Color(.19f,.16f,.12f));stone=Mat("Damp stone",new Color(.25f,.3f,.28f));cloth=Mat("Faded cloth",new Color(.32f,.2f,.16f));dark=Mat("Branch silhouette",new Color(.025f,.035f,.03f));metal=Mat("Worn battery",new Color(.32f,.37f,.32f),.35f);
            var tex=new Texture2D(256,256,TextureFormat.RGB24,false){name="Soil detail",wrapMode=TextureWrapMode.Repeat};
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float n=Mathf.PerlinNoise(x*.32f,y*.32f)*.55f+(float)random.NextDouble()*.25f;float path=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((PathDistance(-38+x/255f*76,-38+y/255f*80)-.8f)/2));tex.SetPixel(x,y,Color.Lerp(new Color(.13f+n*.3f,.19f+n*.28f,.15f+n*.23f),new Color(.29f+n*.33f,.27f+n*.29f,.21f+n*.25f),path));}tex.Apply();AssetDatabase.CreateAsset(tex,Folder+"/SoilDetail.asset");ground.SetTexture("_BaseMap",tex);ground.SetColor("_BaseColor",new Color(.72f,.75f,.7f));
        }
        private static void MakeGround()
        {
            const int n=100;var v=new Vector3[(n+1)*(n+1)];var uv=new Vector2[v.Length];var colors=new Color[v.Length];var t=new int[n*n*6];int k=0;
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;float px=-38+x*.76f,pz=-38+z*.8f;v[i]=At(px,pz);uv[i]=new Vector2(x/(float)n,z/(float)n);colors[i]=Color.white;if(x<n&&z<n){t[k++]=i;t[k++]=i+n+1;t[k++]=i+1;t[k++]=i+1;t[k++]=i+n+1;t[k++]=i+n+2;}}
            var mesh=new Mesh{name="Forest ground"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/ForestGround.asset");
            var o=new GameObject("Uneven forest ground",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));o.transform.SetParent(environment,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=ground;o.GetComponent<MeshCollider>().sharedMesh=mesh;
            // Rough natural edge rocks provide a readable physical perimeter.
            for(int i=0;i<28;i++){float z=-36+i*2.8f;foreach(float x in new[]{-35f,35f})Shape("Boundary rock",PrimitiveType.Sphere,At(x,z,.4f),new Vector3(5,4,4),stone,environment);}
            for(int i=0;i<26;i++){float x=-35+i*2.8f;foreach(float z in new[]{-36f,39f})Shape("Boundary rock",PrimitiveType.Sphere,At(x,z,.3f),new Vector3(4,4,5),stone,environment);}
        }
        private static void MakeTrees()
        {
            var grove=new GameObject("Conifers URP - hand-edit these placements");grove.transform.SetParent(environment,false);
            string[] kinds={"Tall","Tall","Medium","Bare","Small"};
            for(float z=-36;z<40;z+=4.4f)for(float x=-34;x<35;x+=4.3f)
            {
                float px=x+Rand(-1.4f,1.4f),pz=z+Rand(-1.4f,1.4f);
                if(PathDistance(px,pz)<3.3f || Vector2.Distance(new Vector2(px,pz),new Vector2(0,-29))<5 || Vector2.Distance(new Vector2(px,pz),new Vector2(-12,7))<3.5f)continue;
                string kind=kinds[random.Next(kinds.Length)];var tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Trees+"PF Conifer "+kind+" BOTD URP.prefab"));
                tree.transform.SetParent(grove.transform,false);tree.transform.position=At(px,pz);tree.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);tree.transform.localScale=Vector3.one*Rand(.7f,1.05f);
                foreach(var c in tree.GetComponentsInChildren<Collider>())c.enabled=false;
                var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.32f;trunk.height=10;trunk.center=Vector3.up*5;
            }
            // Small existing conifers create understory silhouettes; no substitute tree meshes.
            for(int i=0;i<75;i++)
            {float x=Rand(-28,28),z=Rand(-31,32);if(PathDistance(x,z)<3)continue;var shrub=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Trees+"PF Conifer Small BOTD URP.prefab"));shrub.name="Conifer understory";shrub.transform.SetParent(grove.transform,false);shrub.transform.position=At(x,z);shrub.transform.localScale=Vector3.one*Rand(.08f,.18f);foreach(var c in shrub.GetComponentsInChildren<Collider>())c.enabled=false;}
            for(int i=0;i<70;i++){float x=Rand(-29,29),z=Rand(-32,34);if(PathDistance(x,z)<2.5f)continue;var rock=Shape("Forest stone",PrimitiveType.Sphere,At(x,z,.1f),new Vector3(Rand(.4f,1.6f),Rand(.4f,1.2f),Rand(.7f,2)),stone,environment);rock.transform.rotation=Quaternion.Euler(Rand(-15,15),Rand(0,360),Rand(-10,10));}
        }
        private static void MakeCamp()
        {
            var camp=new GameObject("Starting campsite");camp.transform.SetParent(environment,false);
            var tent=Shape("Canvas shelter",PrimitiveType.Cube,At(-3,-30,.8f),new Vector3(2.5f,.08f,2.6f),cloth,camp.transform);tent.transform.rotation=Quaternion.Euler(0,0,40);
            Shape("Bedroll",PrimitiveType.Capsule,At(-3,-30,.16f),new Vector3(.65f,1.1f,.25f),cloth,camp.transform).transform.rotation=Quaternion.Euler(90,0,0);
            Shape("Camp seat",PrimitiveType.Cylinder,At(2,-30,.3f),new Vector3(.7f,.3f,.7f),bark,camp.transform);
            for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9;Shape("Cold fire ring",PrimitiveType.Sphere,At(1+Mathf.Cos(a)*.8f,-28+Mathf.Sin(a)*.8f,.1f),new Vector3(.3f,.23f,.35f),stone,camp.transform);}
        }
        private static BoxCollider SafeArea(PlayerComposure composure)
        {
            var area=new GameObject("Campsite calm - interrupts passive only");area.transform.SetParent(environment,false);area.transform.position=At(0,-30);
            var box=area.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up;box.size=new Vector3(8,4,7);
            var rate=area.AddComponent<ComposureRateSource>();Set(rate,"player",composure);Set(rate,"area",box);Set(rate,"sourceName","Campsite calm");Set(rate,"ratePerSecond",3f);Set(rate,"totalBudget",20f);Set(rate,"recoveryCeiling",75f);return box;
        }
        private static void MakeClue()
        {
            var log=Shape("Fallen trunk by sister clue",PrimitiveType.Cylinder,At(-15,9,.45f),new Vector3(.8f,2,.8f),bark,environment);log.transform.rotation=Quaternion.Euler(0,0,90);
            var scarf=Shape("Sister scarf - hold E",PrimitiveType.Cube,At(-15,9,.92f),new Vector3(.9f,.06f,.35f),cloth,environment);scarf.AddComponent<ClueDiscovery>();
        }
        private static void MakeSupplies()
        {
            var battery=Object.Instantiate(AssetDatabase.LoadAssetAtPath<ItemDefinition>(Milestone5Setup.DataFolder+"/Battery.asset"));battery.name="Battery";Set(battery,"displayName","Battery");Set(battery,"icon",MakeIcon());AssetDatabase.CreateAsset(battery,Folder+"/Battery.asset");
            foreach(var p in new[]{At(2,-30,.7f),At(-15,-5,.3f),At(2,20,.3f)})
            {var pack=Shape("Battery supply",PrimitiveType.Cube,p,new Vector3(.22f,.3f,.12f),metal,environment);pack.AddComponent<InventoryPickup>().Configure(battery,2);Shape("Battery contact",PrimitiveType.Cube,new Vector3(0,.56f,0),new Vector3(.35f,.16f,.7f),stone,pack.transform,false);}
        }
        private static void MakeHiding()
        {
            var hide=new GameObject("Root shelter - hiding location");hide.transform.SetParent(environment,false);hide.transform.position=At(-12,7);var spot=hide.AddComponent<HidingSpot>();
            Shape("Root wall",PrimitiveType.Sphere,new Vector3(0,1.1f,1.3f),new Vector3(4,3,.9f),stone,hide.transform);
            Shape("Root left",PrimitiveType.Sphere,new Vector3(-1.35f,1.1f,0),new Vector3(.9f,3,3.5f),stone,hide.transform);
            Shape("Root right",PrimitiveType.Sphere,new Vector3(1.35f,1.1f,0),new Vector3(.9f,3,3.5f),stone,hide.transform);
            var roof=Shape("Fallen root roof",PrimitiveType.Cylinder,new Vector3(0,2.5f,0),new Vector3(1.1f,2,1.1f),bark,hide.transform);roof.transform.rotation=Quaternion.Euler(0,0,90);
            var hidden=new GameObject("Hidden position").transform;hidden.SetParent(hide.transform,false);hidden.localPosition=Vector3.zero;
            var exit=new GameObject("Exit position").transform;exit.SetParent(hide.transform,false);exit.position=At(-12,4);spot.Configure(hidden,exit);
        }
        private static void MakeHazards()
        {
            var channel=AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset");
            foreach(var p in new[]{At(-2,-11),At(3,8)})
            {var hazard=new GameObject("Dry branch litter");hazard.transform.SetParent(environment,false);hazard.transform.position=p;var trigger=hazard.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(2.4f,.5f,2);trigger.center=Vector3.up*.2f;var noise=hazard.AddComponent<NoiseObstacle>();noise.Configure(channel);Set(noise,"prompt","Step through dry branches");for(int i=0;i<12;i++){var twig=Shape("Dry branch",PrimitiveType.Cylinder,new Vector3(Rand(-1,1),.04f,Rand(-.8f,.8f)),new Vector3(.035f,Rand(.25f,.65f),.035f),bark,hazard.transform,false);twig.transform.rotation=Quaternion.Euler(90,Rand(0,180),0);}}
        }
        private static void MakeAtmosphere()
        {
            var root=new GameObject("Atmosphere - fog, moon and grading");var moon=new GameObject("Moon through canopy").AddComponent<Light>();moon.transform.SetParent(root.transform,false);moon.type=LightType.Directional;moon.color=new Color(.55f,.72f,.77f);moon.intensity=.38f;moon.shadows=LightShadows.Soft;moon.transform.rotation=Quaternion.Euler(32,-25,0);
            var atmosphere=root.AddComponent<ForestAtmosphere>();Set(atmosphere,"moon",moon);atmosphere.Apply();
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/ForestGrading.asset");
            var color=profile.Add<ColorAdjustments>(true);color.saturation.Override(-24);color.contrast.Override(12);color.postExposure.Override(.1f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.28f);vignette.smoothness.Override(.6f);
            foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
            var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var ambience=root.AddComponent<AudioSource>();ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/333221__hdfreema__night-crickets-back-porch.aiff");ambience.loop=true;ambience.playOnAwake=true;ambience.volume=.12f;ambience.spatialBlend=0;
        }
    }
}
