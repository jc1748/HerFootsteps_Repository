using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace HerFootsteps
{
    // Pacing and nearby placement adapter. Events retain their own composure/lifecycle gates.
    public sealed class HallucinationDirector : MonoBehaviour
    {
        [SerializeField] private PlayerComposure composure;
        [SerializeField] private Camera view;
        [SerializeField] private HuntController hunt;
        [SerializeField] private PlayerHiding hiding;
        [SerializeField] private PresentationDebugMode debug;
        [SerializeField] private HallucinationEvent[] events;
        [SerializeField, Min(0)] private float recoveryInterval = 18;
        [SerializeField, Min(1)] private float spawnDistance = 10;
        private float nextAttempt = 8, rest;
        private int nextEvent;
        public string PlacementStatus { get; private set; } = "Waiting for first encounter";
        public float RestRemaining => rest;
        public string BlockReason(HallucinationEvent candidate)
        {
            if (events != null && events.Any(e => e != null && e != candidate && e.IsActive)) return "Another hallucination active";
            if (rest > 0) return $"Recovery interval {rest:F1}s";
            if (hunt != null && (hunt.State == HuntState.Warning || hunt.State == HuntState.Active || hunt.State == HuntState.Failed)) return "Real hunt / failure takes priority";
            if (hiding != null && hiding.IsHidden) return "Player is hiding";
            return null;
        }
        private void OnEnable()
        { if (events != null) foreach (var e in events) if (e != null) e.Ended += Ended; }
        private void OnDisable()
        { if (events != null) foreach (var e in events) if (e != null) e.Ended -= Ended; }
        private void Ended(HallucinationEvent e, HallucinationEnd reason) => rest = recoveryInterval;
        private void Update()
        {
            rest = Mathf.Max(0, rest - Time.deltaTime); nextAttempt -= Time.deltaTime;
            if (debug != null && debug.Visible && (Application.isEditor || Debug.isDebugBuild) && Keyboard.current != null)
            {
                if (Keyboard.current.f6Key.wasPressedThisFrame) Request(0);
                if (Keyboard.current.f9Key.wasPressedThisFrame) Request(1);
                if (Keyboard.current.f10Key.wasPressedThisFrame) Request(2);
                if (Keyboard.current.f11Key.wasPressedThisFrame) Request(3);
            }
            if (events == null || events.Length == 0 || nextAttempt > 0) return;
            nextAttempt = 3;
            for (int i = 0; i < events.Length; i++)
            {
                int index = (nextEvent + i) % events.Length;
                if (Request(index)) { nextEvent = (index + 1) % events.Length; break; }
            }
        }
        public bool Request(int index)
        {
            if (events == null || index < 0 || index >= events.Length || composure == null || view == null) return false;
            var e = events[index];
            if (e == null || e.IsActive || !e.isActiveAndEnabled || e.CooldownRemaining > 0 || composure.State < e.MinimumState || BlockReason(e) != null) return false;
            if (e is WildlifeHallucination || e is SanityBeastHallucination)
            {
                if (!TrySpawn(out var point)) { PlacementStatus = "No clear nearby NavMesh spawn with complete approach path"; return false; }
                e.transform.position = point;
                var direction = composure.transform.position - point; direction.y = 0;
                if (direction.sqrMagnitude > .01f) e.transform.rotation = Quaternion.LookRotation(direction);
            }
            else e.transform.position = composure.transform.position;
            bool started = e.TryActivate(); PlacementStatus = started ? "Started " + e.name : e.Status;
            return started;
        }
        private bool TrySpawn(out Vector3 point)
        {
            point = default;
            if (!NavMesh.SamplePosition(composure.transform.position, out var player, 2, NavMesh.AllAreas)) return false;
            var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            var path = new NavMeshPath();
            for (int i = 0; i < 16; i++)
            {
                var direction = Quaternion.Euler(0, Random.Range(-32f,32f),0) * forward;
                var desired = player.position + direction * Random.Range(spawnDistance - 2, spawnDistance + 2);
                if (!NavMesh.SamplePosition(desired, out var hit, 2, NavMesh.AllAreas) || Vector3.Distance(hit.position, player.position) < 6) continue;
                var feet = hit.position + Vector3.up * .4f;
                if (Physics.CheckCapsule(feet, hit.position + Vector3.up * 1.6f, .32f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(view.transform.position, hit.position + Vector3.up * 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (!NavMesh.CalculatePath(hit.position, player.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                point = hit.position; return true;
            }
            return false;
        }
        private void OnGUI()
        {
            if (debug == null || !debug.Visible || composure == null) return;
            float width = Mathf.Min(700, Screen.width - 32);
            GUILayout.BeginArea(new Rect(16, 540, width, 250), GUI.skin.box);
            GUILayout.Label($"Composure {composure.Current:F1} | Hallucination tier: {composure.State} | Recovery {rest:F1}s");
            GUILayout.Label("F6 whisper | F9 footsteps | F10 wildlife | F11 sanity beast (all gates respected)");
            if (events != null) foreach (var e in events) if (e != null)
                GUILayout.Label($"{e.name}: {(e.IsActive ? $"ACTIVE {e.Remaining:F1}s" : e.BlockReason ?? "Eligible")} | cooldown {e.CooldownRemaining:F1}s");
            GUILayout.Label(PlacementStatus); GUILayout.EndArea();
        }
    }
}
