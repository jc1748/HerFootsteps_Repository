using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace HerFootsteps
{
    // Encounter positioning only. The brain still owns evidence, sight, hearing and search.
    public sealed class HuntPlacement : MonoBehaviour
    {
        [SerializeField] private CryptidNavigation navigation;
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform home;
        [SerializeField] private Transform[] entryMarkers = new Transform[0];
        [Header("Provisional placement")]
        [SerializeField, Min(3)] private float minimumDistance = 16;
        [SerializeField, Min(3)] private float maximumDistance = 32;
        [SerializeField, Range(16,256)] private int candidateCount = 128;
        [SerializeField, Min(.1f)] private float navSampleRadius = 2;
        [SerializeField, Min(1)] private float silhouetteHeight = 3.8f;
        [SerializeField, Min(.1f)] private float silhouetteRadius = .9f;
        [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;
        [Header("Return home")]
        [SerializeField, Min(.1f)] private float returnSpeed = 2.5f;
        [SerializeField, Min(1)] private float hiddenReturnAfter = 120;
        [SerializeField] private bool allowHiddenReturn = true;
        [Header("Runtime debug")]
        [SerializeField] private string status = "At home";
        [SerializeField] private Vector3 lastEntry;
        [SerializeField] private bool returning;
        private float returnElapsed, repath;
        public string Status => status;
        public bool Returning => returning;
        public Vector3 LastEntry => lastEntry;
        public Vector3 HomePosition => home!=null?home.position:transform.position;
        private NavMeshQueryFilter Filter => new NavMeshQueryFilter{agentTypeID=navigation.Agent.agentTypeID,areaMask=navigation.Agent.areaMask};

        public bool IsInCameraView(Vector3 feet)
        {
            if(playerCamera==null)return true;
            var bounds=new Bounds(feet+Vector3.up*silhouetteHeight*.5f,new Vector3(silhouetteRadius*2,silhouetteHeight,silhouetteRadius*2));
            return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(playerCamera),bounds);
        }
        private bool RayBlocked(Vector3 point)
        {
            var origin=playerCamera.transform.position;var delta=point-origin;
            foreach(var hit in Physics.RaycastAll(origin,delta.normalized,delta.magnitude,obstructionMask,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(player)&&!hit.transform.IsChildOf(navigation.transform))return true;
            return false;
        }
        private bool Concealed(Vector3 feet)
        {
            return RayBlocked(feet+Vector3.up*.5f)&&RayBlocked(feet+Vector3.up*1.7f)&&RayBlocked(feet+Vector3.up*silhouetteHeight);
        }
        public bool IsVisible(Vector3 feet)=>IsInCameraView(feet)&&!Concealed(feet);
        public bool ValidPosition(Vector3 candidate, out Vector3 point)
        {
            point=default;
            if(navigation==null||!navigation.Ready||player==null)return false;
            if(!NavMesh.SamplePosition(candidate,out var hit,navSampleRadius,Filter))return false;
            point=hit.position;float radius=navigation.Agent.radius;float height=Mathf.Max(radius*2,navigation.Agent.height);
            foreach(var c in Physics.OverlapCapsule(point+Vector3.up*(radius+.12f),point+Vector3.up*(height-radius+.12f),radius,obstructionMask,QueryTriggerInteraction.Ignore))
                if(!(c is TerrainCollider)&&!c.transform.IsChildOf(navigation.transform))return false;
            if(Vector3.Distance(point,player.position)<Mathf.Max(3,radius*3))return false;
            if(!NavMesh.SamplePosition(player.position,out var playerHit,navSampleRadius,Filter))return false;
            var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(point,playerHit.position,Filter,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
            if(home!=null&&(!NavMesh.CalculatePath(point,home.position,Filter,path)||path.status!=NavMeshPathStatus.PathComplete))return false;
            return true;
        }
        public bool TryFindEntry(out Vector3 entry)
        {
            entry=default;if(playerCamera==null||player==null||navigation==null)return false;
            Physics.SyncTransforms();Vector3 fallback=default;bool hasFallback=false;
            bool Consider(Vector3 raw,out Vector3 valid)
            {
                valid=default;if(!ValidPosition(raw,out var p))return false;
                float distance=Vector3.Distance(p,player.position);
                if(distance<minimumDistance||distance>maximumDistance||IsInCameraView(p))return false;
                valid=p;return true;
            }
            foreach(var marker in entryMarkers.Where(m=>m!=null))
                if(Consider(marker.position,out var p)){if(Concealed(p)){entry=p;return true;}if(!hasFallback){fallback=p;hasFallback=true;}}
            float rotation=Random.Range(0f,360f);
            for(int i=0;i<candidateCount;i++)
            {
                float a=(rotation+i*137.508f)*Mathf.Deg2Rad;
                float distance=Mathf.Lerp(minimumDistance,Mathf.Max(minimumDistance,maximumDistance),(i%7)/6f);
                var raw=player.position+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*distance;
                if(!Consider(raw,out var p))continue;
                if(Concealed(p)){entry=p;return true;}
                if(!hasFallback){fallback=p;hasFallback=true;}
            }
            // Offscreen is mandatory; physical occlusion is preferred. Never use an onscreen fallback.
            if(hasFallback){entry=fallback;return true;}return false;
        }
        public bool TryEnter(out Vector3 entry)
        {
            entry=default;
            if(navigation==null||!navigation.Ready){status="Navigation unavailable";return false;}
            if(IsVisible(navigation.transform.position)){status="Origin visible; refusing reposition";return false;}
            if(!TryFindEntry(out entry)){status="No offscreen connected entry; request discarded";return false;}
            navigation.Stop();
            if(!navigation.Agent.Warp(entry)){status="Agent rejected entry";return false;}
            lastEntry=entry;status=Concealed(entry)?"Entered behind occluder":"Entered outside camera frustum";return true;
        }
        public void BeginReturn()
        {
            returning=home!=null;returnElapsed=0;repath=0;
            status=returning?"Returning to home territory":"No home assigned";
            TickReturn(.001f);
        }
        public void TickReturn(float deltaTime)
        {
            if(!returning||navigation==null)return;
            returnElapsed+=deltaTime;repath-=deltaTime;
            if(Vector3.Distance(navigation.transform.position,home.position)<1.2f)
            {navigation.Stop();returning=false;status="At home; awaiting gated hunt";return;}
            if(repath<=0){navigation.MoveTo(home.position,returnSpeed);repath=2;status="Returning: "+navigation.Status;}
            if(allowHiddenReturn&&returnElapsed>=hiddenReturnAfter&&!IsVisible(navigation.transform.position)&&
               ValidPosition(home.position,out var target)&&!IsVisible(target))
            {navigation.Stop();if(navigation.Agent.Warp(target)){returning=false;status="Hidden return completed";}}
        }
        private void OnDrawGizmosSelected()
        {
            if(player!=null){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(player.position,minimumDistance);Gizmos.DrawWireSphere(player.position,maximumDistance);}
            if(home!=null){Gizmos.color=Color.green;Gizmos.DrawWireSphere(home.position,1);}
            Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(lastEntry,.7f);
        }
    }
}
