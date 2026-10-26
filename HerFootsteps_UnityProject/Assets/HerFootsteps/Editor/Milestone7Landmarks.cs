using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class Milestone7Setup
    {
        private static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,Transform parent,bool solid=true)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());
            else if(type==PrimitiveType.Sphere){Object.DestroyImmediate(o.GetComponent<Collider>());o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;}
            return o;
        }
        private static GameObject Beam(string name,Vector3 a,Vector3 b,float width,float depth,Material mat,Transform parent,bool solid=true)
        {var o=Shape(name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,depth,Vector3.Distance(a,b)),mat,parent,solid);o.transform.localRotation=Quaternion.LookRotation(b-a);return o;}
        private static void SavePrefab(Transform t,string name){PrefabUtility.SaveAsPrefabAsset(t.gameObject,Folder+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(t.gameObject);}
        private static GameObject Place(string prefab,string name,float u,float v,Transform parent=null)
        {var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/"+prefab+".prefab"),parent??Region(new Vector2(u,v)));o.name=name;o.transform.position=At(u,v);PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;}
        private static void MakePrefabs()
        {
            var cabin=new GameObject("Cabin").transform;
            Shape("Floor",PrimitiveType.Cube,new Vector3(0,.05f,0),new Vector3(8,.2f,7),wood,cabin);
            Shape("West wall",PrimitiveType.Cube,new Vector3(-4,1.7f,0),new Vector3(.3f,3.4f,7),wood,cabin);
            Shape("East wall",PrimitiveType.Cube,new Vector3(4,1.7f,0),new Vector3(.3f,3.4f,7),wood,cabin);
            Shape("Rear wall",PrimitiveType.Cube,new Vector3(0,1.7f,3.5f),new Vector3(8,3.4f,.3f),wood,cabin);
            foreach(float x in new[]{-2.6f,2.6f})Shape("Front wall - open doorway",PrimitiveType.Cube,new Vector3(x,1.7f,-3.5f),new Vector3(2.8f,3.4f,.3f),wood,cabin);
            Shape("Roof",PrimitiveType.Cube,new Vector3(0,3.6f,0),new Vector3(9,.3f,8),wood,cabin);
            Child("Future NPC interaction - no dialogue",cabin).localPosition=new Vector3(2,0,1.5f);
            Child("Access marker",cabin).localPosition=new Vector3(0,.2f,-1);
            SavePrefab(cabin,"HunterCabin");
            var tent=new GameObject("Tent").transform;
            foreach(float side in new[]{-1f,1f}){var sheet=Shape("Canvas",PrimitiveType.Cube,new Vector3(side*.85f,1,0),new Vector3(.12f,2.8f,3.6f),cloth,tent);sheet.transform.localRotation=Quaternion.Euler(0,0,side*37);}
            Shape("Bedroll",PrimitiveType.Cube,new Vector3(0,.08f,0),new Vector3(.9f,.16f,2),cloth,tent);SavePrefab(tent,"AbandonedTent");
            var camp=new GameObject("Campfire").transform;
            for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;Shape("Fire ring stone",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.9f,.15f,Mathf.Sin(a)*.9f),new Vector3(.45f,.3f,.45f),stone,camp);}
            for(int i=0;i<3;i++)Beam("Charred log",new Vector3(-.55f,.2f,i*.3f-.3f),new Vector3(.55f,.25f,-i*.3f+.3f),.22f,.2f,dead,camp);
            var ember=Child("Provisional ember light",camp).gameObject.AddComponent<Light>();ember.type=LightType.Point;ember.color=new Color(1,.48f,.18f);ember.intensity=2;ember.range=7;ember.transform.localPosition=Vector3.up*.5f;
            SavePrefab(camp,"Campfire");
            var tower=new GameObject("Watchtower").transform;
            for(int flight=0;flight<3;flight++)
            {
                float x=-7+flight*3;float z=flight%2==0?-7:7;float y=flight*10/3f;
                Beam("Ramp flight "+(flight+1),new Vector3(x,y,z),new Vector3(x,y+10/3f,-z),2.4f,.16f,wood,tower);
                if(flight<2)Shape("Turn landing",PrimitiveType.Cube,new Vector3(x+1.5f,y+10/3f,-z-Mathf.Sign(z)*1.5f),new Vector3(5.4f,.18f,3),wood,tower);
                foreach(float side in new[]{-1.25f,1.25f})Beam("Ramp handrail",new Vector3(x+side,y+.9f,z),new Vector3(x+side,y+10/3f+.9f,-z),.08f,.08f,wood,tower);
            }
            Shape("Lookout deck",PrimitiveType.Cube,new Vector3(-1,10,11),new Vector3(8,.2f,8),wood,tower);
            foreach(float x in new[]{-4.7f,2.7f})foreach(float z in new[]{6.3f,13.7f})Shape("Tower support",PrimitiveType.Cube,new Vector3(x,5,z),new Vector3(.35f,10,.35f),wood,tower);
            Beam("North parapet",new Vector3(-4.8f,10.9f,14),new Vector3(2.8f,10.9f,14),.12f,.12f,wood,tower);
            Beam("East parapet",new Vector3(3,10.9f,6),new Vector3(3,10.9f,14),.12f,.12f,wood,tower);
            Child("Access marker",tower).localPosition=new Vector3(-7,.15f,-7);
            Child("Overlook marker - face northeast",tower).localPosition=new Vector3(-1,10.2f,12);
            SavePrefab(tower,"Watchtower");
            var hiding=new GameObject("Root shelter").transform;
            Shape("Back cover",PrimitiveType.Cube,new Vector3(0,1.1f,1.2f),new Vector3(3.4f,2.6f,.6f),stone,hiding);
            Shape("Left cover",PrimitiveType.Cube,new Vector3(-1.35f,1.1f,0),new Vector3(.6f,2.6f,3),stone,hiding);
            Shape("Right cover",PrimitiveType.Cube,new Vector3(1.35f,1.1f,0),new Vector3(.6f,2.6f,3),stone,hiding);
            Shape("Root roof",PrimitiveType.Cube,new Vector3(0,2.5f,0),new Vector3(3.5f,.4f,3),wood,hiding);
            var hidden=Child("Hidden position",hiding);hidden.localPosition=Vector3.up*.12f;var exit=Child("Exit position",hiding);exit.localPosition=new Vector3(0,.12f,-2.6f);hiding.gameObject.AddComponent<HidingSpot>().Configure(hidden,exit);SavePrefab(hiding,"RootShelter");
            var tree=new GameObject("Corrupted tree").transform;
            Beam("Black trunk",Vector3.zero,new Vector3(.3f,10,0),.65f,.65f,dead,tree);
            for(int i=0;i<7;i++){float a=i*2.4f;var start=new Vector3(0,3+i*.8f,0);var end=start+new Vector3(Mathf.Cos(a)*3,2,Mathf.Sin(a)*3);Beam("Dead limb",start,end,.2f,.2f,dead,tree,false);Beam("Broken tip",end,end+new Vector3(.6f,1.6f,-.2f),.1f,.1f,dead,tree,false);}
            SavePrefab(tree,"CorruptedTree");
            var hazard=new GameObject("Branch noise patch").transform;var box=hazard.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up*.3f;box.size=new Vector3(2,1,2);hazard.gameObject.AddComponent<NoiseObstacle>().Configure(AssetDatabase.LoadAssetAtPath<NoiseChannel>("Assets/HerFootsteps/Settings/PrototypeNoise.asset"));
            for(int i=0;i<7;i++)Beam("Dry branch",new Vector3(Rand(-1,1),.08f,Rand(-1,1)),new Vector3(Rand(-1,1),.08f,Rand(-1,1)),.04f,.04f,wood,hazard,false);SavePrefab(hazard,"NoisePatch");
            var animal=new GameObject("Dead animal placeholder").transform;
            Shape("Body",PrimitiveType.Sphere,new Vector3(0,.32f,0),new Vector3(1.6f,.6f,.6f),cloth,animal);
            Shape("Head",PrimitiveType.Sphere,new Vector3(.9f,.28f,0),Vector3.one*.4f,dead,animal);
            for(int i=0;i<4;i++)Beam("Limb",new Vector3(i<2?-.5f:.5f,.2f,0),new Vector3(i<2?-.7f:.7f,.12f,i%2==0?.7f:-.7f),.1f,.1f,dead,animal);
            SavePrefab(animal,"DeadAnimal");
        }
        private static void PlaceLandmarks()
        {
            Place("HunterCabin","Hunters cabin - future NPC",510,365);
            Place("AbandonedTent","Abandoned tent - flare reservation",700,680);
            Child("Future flare pickup - no flare mechanic",regions[1]).position=At(710,705);
            Place("Campfire","Starting fire and supplies",790,945);
            Place("Campfire","Central campsite fire",880,720);
            var tower=Place("Watchtower","Watchtower - accessible ramps",1065,920);tower.transform.position+=Vector3.forward*5;PrefabUtility.RecordPrefabInstancePropertyModifications(tower.transform);
            var story=Child("Territory - future sister rescue",regions[5]);story.position=At(1340,390);
            Child("Sister reservation - no rescue script",story).localPosition=new Vector3(-2,0,1);
            Shape("Sister proxy - reserved",PrimitiveType.Capsule,new Vector3(-2,.8f,1),new Vector3(.45f,.8f,.45f),cloth,story,false);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var p=new Vector2(1340+Mathf.Cos(a)*90,390+Mathf.Sin(a)*85);if(i==1||i==2)continue;Place("CorruptedTree","Territory enclosing dead tree",p.x,p.y);}
            var skeleton=Child("Human skeleton - future scripted scream reservation",regions[3]);skeleton.position=At(810,150);
            Shape("Skull proxy",PrimitiveType.Sphere,new Vector3(0,.22f,.65f),Vector3.one*.4f,bone,skeleton);
            Beam("Spine",new Vector3(0,.12f,.45f),new Vector3(0,.12f,-.5f),.12f,.12f,bone,skeleton);
            for(int i=0;i<4;i++)Beam("Rib",new Vector3(-.25f,.15f,.3f-i*.17f),new Vector3(.25f,.15f,.3f-i*.17f),.06f,.06f,bone,skeleton);
            Child("Future Scream NoiseChannel hook - intentionally inactive",skeleton);
            Place("DeadAnimal","Route B - dead animal 01",940,415);Place("DeadAnimal","Route B - dead animal 02",980,535);
        }
        private static void Supplies(float u,float v,string name,int count=2)
        {
            var o=Shape(name,PrimitiveType.Cube,At(u,v,.35f),new Vector3(.6f,.45f,.4f),wood,Region(new Vector2(u,v)));
            o.AddComponent<InventoryPickup>().Configure(AssetDatabase.LoadAssetAtPath<ItemDefinition>(PresentationSetup.Folder+"/Battery.asset"),count);
        }
        private static BoxCollider Volume(string name,float u,float v,Vector3 size)
        {var t=Child(name,Region(new Vector2(u,v)));t.position=At(u,v);var box=t.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up*2;box.size=size;return box;}
        private static void PlaceGameplay()
        {
            var composure=player.GetComponent<PlayerComposure>();
            foreach(var p in Points(790,940,880,720))
            {var box=Volume("Camp calm - finite recovery",p.x,p.y,new Vector3(9,5,9));safeVolumes.Add(box);var rate=box.gameObject.AddComponent<ComposureRateSource>();Set(rate,"player",composure);Set(rate,"area",box);Set(rate,"ratePerSecond",3f);Set(rate,"totalBudget",20f);Set(rate,"sourceName","Camp calm");}
            foreach(var p in Points(1290,720,1430,700,1380,540))
            {var box=Volume("Route A - corrupted composure pressure",p.x,p.y,new Vector3(13,8,16));var rate=box.gameObject.AddComponent<ComposureRateSource>();Set(rate,"player",composure);Set(rate,"area",box);Set(rate,"ratePerSecond",-.5f);Set(rate,"sourceName","Corrupted trees");}
            foreach(var p in Points(790,920,895,755,1200,455))Supplies(p.x,p.y,"Battery cache");
            foreach(var p in Points(520,550,730,345,925,270,700,700))Supplies(p.x,p.y,"Toolbox - provisional battery supplies");
            int clueIndex=0;foreach(var p in Points(595,375,790,790,960,790,1120,290,1380,595))
            {var o=Shape("Sister clue "+(++clueIndex)+" - hold E",PrimitiveType.Cube,At(p.x,p.y,.55f),new Vector3(.7f,.08f,.4f),cloth,Region(p));var clue=o.AddComponent<ClueDiscovery>();Set(clue,"clueId","m7-scarf-fragment-"+clueIndex);Set(clue,"title","a fragment of her scarf");Shape("Clue stone",PrimitiveType.Sphere,At(p.x,p.y,.2f),new Vector3(1,.5f,.8f),stone,Region(p));}
            int index=0;foreach(var p in Points(585,570,765,600,750,430,980,320,1140,330,1030,660,1300,580))
            {var hide=Place("RootShelter","Hiding shelter "+(++index),p.x,p.y);var spot=hide.GetComponent<HidingSpot>();spot.ExitPosition.position=At(p.x,p.y+19,.12f);PrefabUtility.RecordPrefabInstancePropertyModifications(spot.ExitPosition);}
            foreach(var p in Points(610,745,620,650,695,515,585,435,710,380,930,560,1010,440,950,305,1245,275,1385,490,1105,780))Place("NoisePatch","Dry branch noise hazard",p.x,p.y);
            var phantom=Object.FindAnyObjectByType<PhantomFootstepsHallucination>();
            foreach(var p in Points(980,500,1290,720,1430,650))
            {var box=Volume("Phantom footsteps eligibility area",p.x,p.y,new Vector3(16,8,18));var area=box.gameObject.AddComponent<HallucinationArea>();Set(area,"player",composure);Set(area,"area",box);Set(area,"hallucination",phantom);}
        }
    }
}
