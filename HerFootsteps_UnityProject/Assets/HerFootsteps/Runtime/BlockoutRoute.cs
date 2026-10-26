using UnityEngine;

namespace HerFootsteps
{
    // Authoring guide only. Moving nodes never regenerates terrain or overwrites the scene.
    public sealed class BlockoutRoute : MonoBehaviour
    {
        public Color color = new Color(.55f,.7f,.6f);
        [TextArea] public string designIntent;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color=color;
            for(int i=0;i<transform.childCount;i++)
            {
                Vector3 point=transform.GetChild(i).position+Vector3.up*.2f;
                Gizmos.DrawSphere(point,.25f);
                if(i>0)Gizmos.DrawLine(transform.GetChild(i-1).position+Vector3.up*.2f,point);
            }
        }
    }
}
