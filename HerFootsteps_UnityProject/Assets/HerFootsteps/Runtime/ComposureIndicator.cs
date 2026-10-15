using UnityEngine;
using UnityEngine.UI;

namespace HerFootsteps
{
    // Original vector HUD: open circular dial surrounding two folded brain lobes.
    // No exact value/source labels here; those belong to the development overlay.
    public sealed class ComposureIndicator : MaskableGraphic
    {
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private Color high = new Color(.48f, .65f, .60f, .9f);
        [SerializeField] private Color medium = new Color(.65f, .65f, .46f, .9f);
        [SerializeField] private Color low = new Color(.66f, .43f, .34f, .95f);
        [SerializeField] private Color broken = new Color(.65f, .27f, .29f, .95f);
        [SerializeField, Min(0)] private float responseSpeed = 2;
        private float shown = 1, pulse;
        private Color feedback;
        public float DisplayedFraction => shown;
        public void Bind(PlayerComposure value) => composure = value;
        protected override void OnEnable()
        {
            base.OnEnable(); raycastTarget = false;
            if (composure != null) { shown = composure.Current / composure.Capacity; composure.Changed += Changed; }
        }
        protected override void OnDisable() { if (composure != null) composure.Changed -= Changed; base.OnDisable(); }
        private void Changed(ComposureChange change)
        {
            // Rate sources cannot keep a full-bright flash permanently lit.
            pulse = Mathf.Clamp01(pulse + Mathf.Abs(change.Current - change.Previous) * .04f);
            feedback = change.Current > change.Previous ? high : low;
        }
        private void Update()
        {
            if (composure == null) return;
            shown = Mathf.MoveTowards(shown, composure.Current / composure.Capacity, Time.unscaledDeltaTime * responseSpeed);
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * .7f);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (composure == null) return;
            var rect = GetPixelAdjustedRect(); Vector2 center = rect.center;
            float scale = Mathf.Min(rect.width, rect.height) * .5f;
            Color tint = composure.State == ComposureState.High ? high : composure.State == ComposureState.Medium ? medium : composure.State == ComposureState.Low ? low : broken;
            tint = Color.Lerp(tint, feedback, pulse * .55f);
            Ring(vh, center, scale * .87f, scale * .075f, 1, new Color(.13f,.18f,.18f,.85f));
            Ring(vh, center, scale * .87f, scale * .075f, shown, tint);
            // Hand-drawn lobe contours and folds, mirrored around an irregular central seam.
            Vector2[] outline = { new Vector2(-.05f,-.48f), new Vector2(-.25f,-.47f), new Vector2(-.43f,-.34f), new Vector2(-.51f,-.12f), new Vector2(-.48f,.10f), new Vector2(-.51f,.24f), new Vector2(-.38f,.43f), new Vector2(-.2f,.53f), new Vector2(-.06f,.44f), new Vector2(-.05f,-.48f) };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i=1;i<outline.Length;i++) Line(vh, center+new Vector2(outline[i-1].x*side,outline[i-1].y)*scale, center+new Vector2(outline[i].x*side,outline[i].y)*scale, scale*.035f,tint);
                Vector2[] folds = { new Vector2(-.12f,.28f), new Vector2(-.29f,.23f), new Vector2(-.31f,.08f), new Vector2(-.18f,.02f), new Vector2(-.19f,-.15f), new Vector2(-.36f,-.23f) };
                for(int i=1;i<folds.Length;i++) Line(vh,center+new Vector2(folds[i-1].x*side,folds[i-1].y)*scale,center+new Vector2(folds[i].x*side,folds[i].y)*scale,scale*.025f,tint);
            }
            if(composure.IsBroken) Line(vh,center+new Vector2(-.63f,-.64f)*scale,center+new Vector2(.63f,.64f)*scale,scale*.045f,broken);
        }
        private static void Ring(VertexHelper vh, Vector2 center,float radius,float width,float fraction,Color tint)
        {
            int segments=Mathf.CeilToInt(Mathf.Clamp01(fraction)*96);
            for(int i=0;i<segments;i++)
            {
                float a=(90-i/96f*360)*Mathf.Deg2Rad,b=(90-Mathf.Min((i+1)/96f,fraction)*360)*Mathf.Deg2Rad;
                Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,tint);
            }
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f; int first=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);
            vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);
        }
    }
}
