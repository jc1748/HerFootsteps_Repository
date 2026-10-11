using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HerFootsteps.Editor
{
    public static partial class PresentationSetup
    {
        private static void Limb(string name,Vector3 a,Vector3 b,float radius,Transform parent)
        {var o=Shape(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),dark,parent,false);o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        private static GameObject Creature(string name,Transform parent,bool animal)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);
            if(animal)
            {
                Shape("Gaunt body",PrimitiveType.Sphere,new Vector3(0,1.05f,0),new Vector3(.52f,.62f,1.4f),dark,root.transform,false);
                Limb("Long neck",new Vector3(0,1.2f,.45f),new Vector3(0,1.9f,.8f),.19f,root.transform);
                Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.9f,.85f),new Vector3(.28f,.3f,.58f),dark,root.transform,false);
                foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-.5f,.5f})Limb("Spindly leg",new Vector3(side*.2f,1,z),new Vector3(side*.25f,.05f,z-.15f),.055f,root.transform);
            }
            else
            {
                Shape("Narrow torso",PrimitiveType.Sphere,new Vector3(0,1.85f,0),new Vector3(.55f,1.5f,.42f),dark,root.transform,false);
                Shape("Head",PrimitiveType.Sphere,new Vector3(0,2.7f,.04f),new Vector3(.31f,.47f,.3f),dark,root.transform,false);
                foreach(float side in new[]{-1f,1f})
                {Limb("Leg",new Vector3(side*.15f,1.3f,0),new Vector3(side*.25f,.04f,0),.1f,root.transform);Limb("Upper arm",new Vector3(side*.25f,2.2f,0),new Vector3(side*.55f,1.45f,0),.085f,root.transform);Limb("Hanging arm",new Vector3(side*.55f,1.45f,0),new Vector3(side*.48f,.65f,.12f),.05f,root.transform);}
            }
            float h=animal?2:2.85f;
            foreach(float side in new[]{-1f,1f})
            {Limb("Branch crown",new Vector3(side*.1f,h,animal?.75f:0),new Vector3(side*.55f,h+.6f,animal?.65f:0),.045f,root.transform);Limb("Crown tine",new Vector3(side*.35f,h+.35f,animal?.7f:0),new Vector3(side*.28f,h+.9f,animal?.6f:0),.023f,root.transform);Limb("Crown outer tine",new Vector3(side*.5f,h+.5f,animal?.65f:0),new Vector3(side*.78f,h+.73f,animal?.5f:0),.024f,root.transform);}
            return root;
        }
        private static void RebuildHallucinations(FalseTrailHallucination trail,WildlifeHallucination wildlife)
        {
            foreach(Transform child in trail.transform)Object.DestroyImmediate(child.gameObject);
            foreach(Transform child in wildlife.transform)Object.DestroyImmediate(child.gameObject);
            trail.transform.position=At(3,0);var visual=new GameObject("False trail - weathered direction stakes");visual.transform.SetParent(trail.transform,false);
            for(int i=0;i<3;i++)
            {var p=new Vector3(2+i*2,.65f,i*1.4f);Shape("Old stake",PrimitiveType.Cylinder,p,new Vector3(.09f,.65f,.09f),bark,visual.transform,false);var plank=Shape("Misleading marker",PrimitiveType.Cube,p+Vector3.up*.35f,new Vector3(.8f,.13f,.06f),cloth,visual.transform,false);plank.transform.localRotation=Quaternion.Euler(0,-35,12);}
            Set(trail,"visualRoot",visual);visual.SetActive(false);
            wildlife.transform.position=At(-7,13);wildlife.transform.rotation=Quaternion.Euler(0,160,0);
            foreach(Transform child in wildlife.transform)Object.DestroyImmediate(child.gameObject);
            var animal=Creature("Uncertain wildlife silhouette",wildlife.transform,true);
            var target=new GameObject("Flashlight reaction point").transform;target.SetParent(wildlife.transform,false);target.localPosition=new Vector3(0,1.2f,0);
            Set(wildlife,"visualRoot",animal);Set(wildlife,"reactionTarget",target);animal.SetActive(false);
        }
        private static Sprite MakeIcon()
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Battery hotbar icon",filterMode=FilterMode.Bilinear};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {bool body=x>=20&&x<=43&&y>=9&&y<=51,cap=x>=27&&x<=36&&y>51&&y<=56;bool border=body&&(x<23||x>40||y<12||y>48);bool plus=(x>=28&&x<=35&&y>=38&&y<=40)||(x>=31&&x<=32&&y>=35&&y<=43);texture.SetPixel(x,y,cap||border||plus?new Color(.66f,.73f,.67f,1):body?new Color(.24f,.3f,.27f,1):Color.clear);}
            texture.Apply();System.IO.File.WriteAllBytes(Folder+"/BatteryIcon.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(Folder+"/BatteryIcon.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/BatteryIcon.png");importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/BatteryIcon.png");
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {var o=new GameObject(name,typeof(RectTransform));var r=o.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        private static Image Image(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var image=Rect(name,parent,anchor,position,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        private static Text Text(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,TextAnchor alignment=TextAnchor.MiddleCenter)
        {var text=Rect(name,parent,anchor,position,size).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=alignment;text.color=new Color(.73f,.79f,.75f);text.raycastTarget=false;return text;}
        private static void MakeUi(FirstPersonMotor player)
        {
            var root=new GameObject("Gameplay HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(player.transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var center=new Vector2(.5f,.5f);var bottom=new Vector2(.5f,0);
            var pulse=Image("Composure wash",root.transform,center,Vector2.zero,new Vector2(4000,2400),Color.clear);
            Image("Crosshair",root.transform,center,Vector2.zero,new Vector2(3,3),new Color(.7f,.75f,.7f,.55f));
            var prompt=Text("Interaction prompt",root.transform,center,new Vector2(0,-52),new Vector2(780,40),20);
            var hold=Image("Hold E progress",root.transform,center,new Vector2(0,-78),new Vector2(180,2),new Color(.62f,.72f,.64f));hold.type=UnityEngine.UI.Image.Type.Filled;hold.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;hold.fillAmount=0;
            var slots=new HotbarSlotView[5];
            for(int i=0;i<5;i++)
            {
                var border=Image("Slot "+(i+1),root.transform,bottom,new Vector2((i-2)*76,63),new Vector2(70,70),new Color(.3f,.4f,.35f,.7f));
                var bg=Image("Inset",border.transform,center,Vector2.zero,new Vector2(68,68),new Color(.02f,.03f,.025f,.8f));
                var icon=Image("Item icon",border.transform,center,Vector2.zero,new Vector2(48,48),Color.white);icon.preserveAspect=true;
                var key=Text("Key",border.transform,center,new Vector2(-23,23),new Vector2(18,18),12);
                var count=Text("Count",border.transform,center,new Vector2(22,-23),new Vector2(24,20),16);
                var fallback=Text("Fallback icon",border.transform,center,Vector2.zero,new Vector2(55,30),14);
                slots[i]=border.gameObject.AddComponent<HotbarSlotView>();slots[i].Bind(bg,border,icon,key,count,fallback);
            }
            // Reusable slot prefab can be restyled without changing inventory mechanics.
            PrefabUtility.SaveAsPrefabAsset(slots[0].gameObject,Folder+"/HotbarSlot.prefab");
            var selected=Text("Selected item",root.transform,bottom,new Vector2(0,116),new Vector2(600,32),17);
            var resources=Text("Relevant resources",root.transform,bottom,new Vector2(0,160),new Vector2(900,30),15);
            var notice=Text("Feedback",root.transform,new Vector2(.5f,.75f),Vector2.zero,new Vector2(1000,90),21);var group=notice.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            var hud=player.gameObject.AddComponent<PresentationHud>();Set(hud,"input",player.GetComponent<PlayerInputReader>());Set(hud,"inventory",player.GetComponent<PlayerInventory>());Set(hud,"interactor",player.GetComponent<PlayerInteractor>());Set(hud,"composure",player.GetComponent<PlayerComposure>());Set(hud,"flashlight",player.GetComponent<PlayerFlashlight>());Set(hud,"breath",player.GetComponent<PlayerBreath>());Set(hud,"motor",player);SetArray(hud,"slots",slots);
            Set(hud,"prompt",prompt);Set(hud,"selectedName",selected);Set(hud,"resources",resources);Set(hud,"notice",notice);Set(hud,"noticeGroup",group);Set(hud,"holdProgress",hold);Set(hud,"composurePulse",pulse);
            var debug=player.gameObject.AddComponent<PresentationDebugMode>();Set(debug,"composure",player.GetComponent<PlayerComposure>());Set(debug,"passive",player.GetComponent<ComposureRateSource>());
            SetArray(debug,"panels",new Object[]{player.GetComponent<PrototypeHud>(),player.GetComponent<Milestone2Hud>(),player.GetComponent<Milestone3Hud>(),player.GetComponent<Milestone5Hud>()});
            Set(player.GetComponent<Milestone5Hud>(),"controlHint","F3 debug | F4 -25 | F5 +25 | F7 trail | F8 wildlife");
            foreach(var panel in new MonoBehaviour[]{player.GetComponent<PrototypeHud>(),player.GetComponent<Milestone2Hud>(),player.GetComponent<Milestone3Hud>(),player.GetComponent<Milestone5Hud>()})panel.enabled=false;
        }
    }
}
