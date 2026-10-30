using UnityEngine;
using UnityEngine.UI;

namespace HerFootsteps
{
    public sealed class NoiseExposureIndicator : MaskableGraphic
    {
        [SerializeField] private NoiseExposure exposure;
        [SerializeField] private Color quiet = new Color(.38f,.52f,.49f,.85f);
        [SerializeField] private Color alert = new Color(.65f,.34f,.26f,.95f);
        private float shown, pulse, previous;
        public void Bind(NoiseExposure value)=>exposure=value;
        protected override void OnEnable(){base.OnEnable();raycastTarget=false;if(exposure!=null)exposure.Changed+=Changed;}
        protected override void OnDisable(){if(exposure!=null)exposure.Changed-=Changed;base.OnDisable();}
        private void Changed(){if(exposure.Current>previous)pulse=.6f;previous=exposure.Current;}
        private void Update(){if(exposure==null)return;shown=Mathf.MoveTowards(shown,exposure.Current/exposure.Threshold,Time.unscaledDeltaTime*2);pulse=Mathf.MoveTowards(pulse,0,Time.unscaledDeltaTime);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            for(int i=0;i<20;i++)
            {
                float x=r.xMin+i*r.width/20;float h=4+(i%4)*3;
                Color c=i/20f<shown?Color.Lerp(quiet,alert,shown):new Color(.12f,.16f,.16f,.65f);
                if(i/20f<shown)c=Color.Lerp(c,new Color(.8f,.7f,.5f,1),pulse*.4f);
                int n=vh.currentVertCount;float y=r.center.y-h*.5f;
                vh.AddVert(new Vector3(x,y,0),c,Vector2.zero);vh.AddVert(new Vector3(x,y+h,0),c,Vector2.zero);
                vh.AddVert(new Vector3(x+r.width/20-2,y+h,0),c,Vector2.zero);vh.AddVert(new Vector3(x+r.width/20-2,y,0),c,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
