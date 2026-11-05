using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static class Milestone7CSetup
    {
        public const string Folder = "Assets/HerFootsteps/Milestone7C";
        private static void Set(Object o,string field,object value) => Milestone6Setup.Set(o,field,value);
        private static void Number(Object o,string field,int value) { var s=new SerializedObject(o);s.FindProperty(field).intValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
        private static void ColorField(Object o,string field,Color value) { var s=new SerializedObject(o);s.FindProperty(field).colorValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
        private static T Find<T>() where T:Component => Object.FindAnyObjectByType<T>();
        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/"+name);
        [MenuItem("Her Footsteps/Presentation/Apply Milestone 7C")]
        public static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=Milestone7BSetup.ScenePath||EditorApplication.isPlaying)throw new Exception("Open the current M7B forest in Edit Mode.");
            if(Find<HallucinationDirector>()!=null)throw new Exception("7C already applied; refusing duplicate scene edits.");
            // Save the exact loaded scene first, including designer changes; retain a byte-for-byte backup.
            EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("Logs/Milestone7C-backup");
            if(!File.Exists("Logs/Milestone7C-backup/Forest-before-7C.unity"))File.Copy(scene.path,"Logs/Milestone7C-backup/Forest-before-7C.unity",false);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var terrain=Find<Terrain>(); var terrainPath=AssetDatabase.GetAssetPath(terrain.terrainData);
            if(!File.Exists("Logs/Milestone7C-backup/Terrain-before-7C.asset"))File.Copy(terrainPath,"Logs/Milestone7C-backup/Terrain-before-7C.asset",false);
            var player=Find<FirstPersonMotor>();var comp=player.GetComponent<PlayerComposure>();var flashlight=player.GetComponent<PlayerFlashlight>();
            var camera=player.GetComponentInChildren<Camera>();var beam=player.GetComponentsInChildren<Light>(true).First(l=>l.type==LightType.Spot);
            var hunt=Find<HuntController>();var brain=Find<CryptidBrain>();
            var glow=new GameObject("M7C subtle red silhouette light").AddComponent<Light>();glow.transform.SetParent(brain.transform,false);glow.transform.localPosition=new Vector3(.35f,1.4f,.8f);
            glow.type=LightType.Point;glow.color=new Color(1,.035f,.015f);glow.intensity=1.2f;glow.range=4.5f;glow.shadows=LightShadows.None;glow.enabled=false;
            Set(glow.gameObject.AddComponent<CryptidRedLight>(),"hunt",hunt);
            var twig=Excerpt(Clip("766034__lizzbitzz__twigs-snapping_muiltiple-snaps.wav"),"TwigSnap",0,1.2f);
            int obstacles=0;
            foreach(var obstacle in Object.FindObjectsByType<NoiseObstacle>(FindObjectsSortMode.None))
            {
                var source=obstacle.GetComponent<AudioSource>();if(source==null)source=obstacle.gameObject.AddComponent<AudioSource>(); ConfigureAudio(source,null,.8f,1,20);
                Set(obstacle,"audioSource",source);Set(obstacle,"activationClip",twig);Set(obstacle,"volume",.7f);Set(obstacle,"cooldown",2.5f);obstacles++;
            }
            Hud(player,comp,flashlight);
            var root=new GameObject("M7C hallucination encounters - editable");
            var director=root.AddComponent<HallucinationDirector>();Set(director,"composure",comp);Set(director,"view",camera);Set(director,"hunt",hunt);Set(director,"hiding",player.GetComponent<PlayerHiding>());Set(director,"debug",player.GetComponent<PresentationDebugMode>());
            var whisper=new GameObject("Whispers - breath placeholder").AddComponent<WhisperHallucination>();whisper.transform.SetParent(root.transform);
            var voice=new GameObject("Whisper spatial origin").AddComponent<AudioSource>();voice.transform.SetParent(root.transform);
            ConfigureAudio(voice,WhisperClip(),.45f,1,10);Set(whisper,"voice",voice);
            var phantom=Find<PhantomFootstepsHallucination>();
            var wildlife=Find<WildlifeHallucination>();
            // The old regional adapters only reached footsteps in three small boxes. Director now handles pacing.
            foreach(var area in Object.FindObjectsByType<HallucinationArea>(FindObjectsSortMode.None))area.enabled=false;
            var beast=new GameObject("Sanity beast - hallucination only").AddComponent<SanityBeastHallucination>();beast.transform.SetParent(root.transform);
            var visual=new GameObject("Replaceable beast silhouette");visual.transform.SetParent(beast.transform,false);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.12f,.15f,.16f);AssetDatabase.CreateAsset(material,Folder+"/SanityBeast.mat");
            Part(visual.transform,PrimitiveType.Capsule,"Body",new Vector3(0,.9f,0),new Vector3(.65f,.7f,.55f),material);
            Part(visual.transform,PrimitiveType.Sphere,"Head",new Vector3(0,1.65f,.15f),new Vector3(.55f,.5f,.55f),material);
            Part(visual.transform,PrimitiveType.Capsule,"Left reaching arm",new Vector3(-.55f,1,.3f),new Vector3(.2f,.65f,.2f),material);
            Part(visual.transform,PrimitiveType.Capsule,"Right reaching arm",new Vector3(.55f,1,.3f),new Vector3(.2f,.65f,.2f),material);
            var scream=new GameObject("Sanity scream - psychological audio only").AddComponent<AudioSource>();scream.transform.SetParent(root.transform);
            ConfigureAudio(scream,Excerpt(Clip("618112__nachtmahrtv__woman-scream.wav"),"SanityScream",0,2.5f),.6f,2,22);
            Set(beast,"flashlight",flashlight);Set(beast,"beam",beam);Set(beast,"scream",scream);Set(beast,"visualRoot",visual);
            var events=new HallucinationEvent[]{whisper,phantom,wildlife,beast};
            foreach(var e in events)
            {
                Set(e,"composure",comp);Set(e,"director",director);Set(e,"automatic",false);Set(e,"activationRange",20f);
                Set(e,"cooldown",e==beast?65f:35f);Set(e,"duration",e==beast?5f:e==whisper?5f:8f);
                Number(e,"minimumState",e==beast||e==wildlife?2:1);
            }
            var ds=new SerializedObject(director);var list=ds.FindProperty("events");list.arraySize=events.Length;
            for(int i=0;i<events.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=events[i];ds.ApplyModifiedPropertiesWithoutUndo();
            Set(player.GetComponent<PresentationDebugMode>(),"eventHint","F6 whisper | F9 footsteps | F10 wildlife | F11 beast");
            var atmosphere=Find<ForestAtmosphere>();
            // Old fog blended nearly to black, hiding the gradient under the dark grade.
            ColorField(atmosphere,"fogColor",new Color(.15f,.19f,.23f));Set(atmosphere,"fogDensity",.043f);atmosphere.Apply();
            camera.backgroundColor=new Color(.15f,.19f,.23f);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            int plants=Vegetation(terrain);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Milestone7C-setup.txt",$"Applied in place to {scene.path}. {obstacles} obstacle sources; {plants} vegetation patches. Far clip unchanged: {camera.farClipPlane}. Terrain and NavMesh untouched.\n");
        }
        private static void Part(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
        { var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material; }
        private static void ConfigureAudio(AudioSource source,AudioClip clip,float volume,float min,float max)
        { source.clip=clip;source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.volume=volume;source.minDistance=min;source.maxDistance=max;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0; }
        private static AudioClip Excerpt(AudioClip source,string name,float start,float seconds)
        {
            int frames=Mathf.Min(source.samples-(int)(start*source.frequency),(int)(seconds*source.frequency));
            var data=new float[frames*source.channels];source.LoadAudioData();source.GetData(data,(int)(start*source.frequency));
            // Short fades avoid clicks; original recording remains unchanged.
            for(int i=0;i<frames;i++)for(int c=0;c<source.channels;c++)data[i*source.channels+c]*=Mathf.Min(1,Mathf.Min(i/(source.frequency*.008f),(frames-1-i)/(source.frequency*.04f)));
            return SaveWav(name,data,source.channels,source.frequency);
        }
        private static AudioClip WhisperClip()
        {
            // Original synthetic nonverbal whispered breaths, used because no whisper recording was imported.
            const int rate=22050;var data=new float[rate*5];var random=new System.Random(731);float low=0,previous=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;float n=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,n,.28f);float high=low-previous;previous=Mathf.Lerp(previous,low,.06f);
                float envelope=0;foreach(float center in new[]{.6f,1.1f,2.1f,2.55f,3.65f})envelope+=Mathf.Exp(-Mathf.Pow((t-center)/.18f,2));
                data[i]=high*envelope*.65f;
            }
            return SaveWav("WhisperBreaths-placeholder",data,1,rate);
        }
        private static AudioClip SaveWav(string name,float[] data,int channels,int rate)
        {
            string path=Folder+"/"+name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            { writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);foreach(float f in data)writer.Write((short)(Mathf.Clamp(f,-1,1)*32767)); }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static void Anchor(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
        { rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;PrefabUtility.RecordPrefabInstancePropertyModifications(rect); }
        private static Text Label(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,int fontSize)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);
            Anchor(text.rectTransform,anchor,position,size);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.68f,.76f,.74f);text.raycastTarget=false;return text;
        }
        private static void Hud(FirstPersonMotor player,PlayerComposure comp,PlayerFlashlight flashlight)
        {
            Set(player.GetComponent<PresentationHud>(),"showLegacyFlashlightText",false);
            var canvas=player.GetComponentInChildren<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;PrefabUtility.RecordPrefabInstancePropertyModifications(scaler);
            var dial=canvas.GetComponentInChildren<ComposureIndicator>();Anchor(dial.rectTransform,new Vector2(1,1),new Vector2(-104,-90),new Vector2(130,130));
            var noise=canvas.GetComponentInChildren<NoiseExposureIndicator>();Anchor(noise.rectTransform,new Vector2(.5f,1),new Vector2(0,-70),new Vector2(340,48));
            var label=noise.GetComponentInChildren<Text>();label.text="NOISE EXPOSURE";label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;Anchor(label.rectTransform,new Vector2(.5f,0),new Vector2(0,-20),new Vector2(340,30));
            var icon=new GameObject("Flashlight charge icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(FlashlightIndicator)).GetComponent<FlashlightIndicator>();icon.transform.SetParent(canvas.transform,false);
            Anchor(icon.rectTransform,new Vector2(0,1),new Vector2(104,-75),new Vector2(136,80));Set(icon,"flashlight",flashlight);
            var readout=canvas.gameObject.AddComponent<HorrorHudReadout>();Set(readout,"flashlight",flashlight);Set(readout,"composure",comp);
            Set(readout,"batteryText",Label(canvas.transform,"Flashlight percent",new Vector2(0,1),new Vector2(104,-130),new Vector2(185,30),20));
            Set(readout,"composureText",Label(canvas.transform,"Composure percent",new Vector2(1,1),new Vector2(-115,-170),new Vector2(220,30),20));
        }
        private static int Vegetation(Terrain terrain)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vegetation/Models/Vegetation.fbx");
            var names=new[]{"Grass_P.001","Grass_P.002","Grass_P.003","Plane.014","Plane.016"};var prefabs=new List<GameObject>();var materials=new Dictionary<Material,Material>();
            foreach(string name in names)
            {
                var source=model.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name==name);
                var root=new GameObject("M7C "+name);var child=new GameObject("Foliage mesh",typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
                child.transform.localRotation=source.transform.rotation;child.transform.localScale=source.transform.lossyScale;child.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
                var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials.Select(original=>
                {
                    if(materials.TryGetValue(original,out var known))return known;
                    var m=new Material(original);m.name="M7C "+original.name;m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.4f);m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",.08f);m.SetFloat("_Metallic",0);m.renderQueue=2450;m.enableInstancing=true;
                    AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".mat");materials.Add(original,m);return m;
                }).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;
                var bounds=renderer.bounds;child.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,new Renderer[]{renderer})});lod.RecalculateBounds();
                prefabs.Add(PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab"));Object.DestroyImmediate(root);
            }
            var group=new GameObject("M7C Ground foliage - editable grass and shrubs").transform;
            var data=terrain.terrainData;var origin=terrain.transform.position;var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
            var protect=Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).Select(i=>i.transform.position).Concat(Object.FindObjectsByType<HidingSpot>(FindObjectsSortMode.None).Select(i=>i.transform.position)).ToArray();
            var random=new System.Random(730);int count=0;Physics.SyncTransforms();
            for(int i=0;i<4500&&count<650;i++)
            {
                float u=(float)random.NextDouble(),v=(float)random.NextDouble();var p=origin+new Vector3(u*data.size.x,0,v*data.size.z);p.y=terrain.SampleHeight(p)+origin.y;
                if(data.GetSteepness(u,v)>28||protect.Any(q=>Vector3.Distance(p,q)<5))continue;
                int x=Mathf.RoundToInt(u*(data.alphamapWidth-1)),z=Mathf.RoundToInt(v*(data.alphamapHeight-1));bool path=false;
                for(int dz=-3;dz<=3;dz++)for(int dx=-3;dx<=3;dx++)if(paint[Mathf.Clamp(z+dz,0,data.alphamapHeight-1),Mathf.Clamp(x+dx,0,data.alphamapWidth-1),1]>.10f)path=true;
                if(path||Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*1.6f,.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!(c is TerrainCollider)))continue;
                int index=count%5;var plant=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[index],group);plant.name=$"{(index<3?"Grass":"Shrub")} {count:000}";
                plant.transform.position=p;plant.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);plant.transform.localScale=Vector3.one*(index<3?.45f:.4f)*Mathf.Lerp(.8f,1.2f,(float)random.NextDouble());PrefabUtility.RecordPrefabInstancePropertyModifications(plant.transform);count++;
            }
            return count;
        }
        [MenuItem("Her Footsteps/Presentation/Refine Milestone 7C Fog")]
        public static void RefineFog()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Milestone7BSetup.ScenePath||EditorApplication.isPlaying)throw new Exception("Requires M7B Edit Mode");
            var atmosphere=Find<ForestAtmosphere>();ColorField(atmosphere,"fogColor",new Color(.15f,.19f,.23f));atmosphere.Apply();
            var camera=Find<FirstPersonMotor>().GetComponentInChildren<Camera>();camera.backgroundColor=new Color(.15f,.19f,.23f);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            Set(Find<PresentationHud>(),"showLegacyFlashlightText",false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
