using UnityEngine;
using UnityEngine.UI;

namespace HerFootsteps
{
    public sealed class FlashlightIndicator : MaskableGraphic
    {
        [SerializeField] private PlayerFlashlight flashlight;
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
        private void Update() => SetVerticesDirty();
        public static Color BatteryColor(float percent) => percent <= 0 ? new Color(.15f,.18f,.18f) :
            percent <= 25 ? new Color(.78f,.18f,.12f) : percent <= 50 ? new Color(.9f,.43f,.12f) :
            percent <= 75 ? new Color(.85f,.73f,.22f) : new Color(.35f,.72f,.46f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (flashlight == null) return;
            float fraction = flashlight.Battery / flashlight.Capacity;
            var tint = BatteryColor(fraction * 100);
            Quad(vh,.10f,.34f,.62f,.66f,new Color(.12f,.16f,.16f,.95f));
            Quad(vh,.12f,.38f,.12f+.48f*fraction,.62f,tint);
            Quad(vh,.62f,.24f,.77f,.76f,tint);
            Quad(vh,.78f,.31f,.84f,.69f,tint);
            Quad(vh,.27f,.68f,.41f,.75f,tint);
        }
        private void Quad(VertexHelper vh,float x0,float y0,float x1,float y1,Color tint)
        {
            var r=GetPixelAdjustedRect(); int n=vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin+x0*r.width,r.yMin+y0*r.height),tint,Vector2.zero);
            vh.AddVert(new Vector3(r.xMin+x0*r.width,r.yMin+y1*r.height),tint,Vector2.zero);
            vh.AddVert(new Vector3(r.xMin+x1*r.width,r.yMin+y1*r.height),tint,Vector2.zero);
            vh.AddVert(new Vector3(r.xMin+x1*r.width,r.yMin+y0*r.height),tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
