using UnityEngine;
using UnityEngine.AI;

namespace HerFootsteps
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CryptidNavigation : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float destinationSampleRadius = 2;
        [SerializeField, Min(0)] private float arrivalTolerance = 0.2f;
        private NavMeshAgent agent;
        private NavMeshPath path;
        public Vector3 Destination { get; private set; }
        public string Status { get; private set; } = "Stopped";
        public bool HasDestination { get; private set; }
        public bool Ready => Agent.isActiveAndEnabled && Agent.isOnNavMesh;
        public NavMeshAgent Agent => agent != null ? agent : (agent = GetComponent<NavMeshAgent>());
        public bool Arrived => Ready && HasDestination && !Agent.pathPending &&
            Agent.remainingDistance <= Agent.stoppingDistance + arrivalTolerance;

        public bool MoveTo(Vector3 position, float speed)
        {
            if (!Ready) { HasDestination = false; Status = "Agent off NavMesh"; return false; }
            var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
            if (!NavMesh.SamplePosition(position, out var hit, destinationSampleRadius, filter))
            {
                Stop(); Status = "No NavMesh near target"; return false;
            }
            if (path == null) path = new NavMeshPath();
            if (!Agent.CalculatePath(hit.position, path) || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length == 0)
            {
                Stop(); Status = "No usable path"; return false;
            }
            // Partial paths approach only the reachable edge. No warping through obstacles.
            Destination = path.corners[path.corners.Length - 1];
            Agent.speed = Mathf.Max(0, speed);
            Agent.isStopped = false;
            HasDestination = Agent.SetPath(path);
            Status = HasDestination ? (path.status == NavMeshPathStatus.PathComplete ? "Complete path" : "Partial path: approach edge")
                : "Path rejected";
            return HasDestination;
        }

        public void Stop()
        {
            if (Ready) { Agent.isStopped = true; Agent.ResetPath(); }
            HasDestination = false;
            Status = "Stopped";
        }

        private void OnDisable() => Stop();
    }
}
