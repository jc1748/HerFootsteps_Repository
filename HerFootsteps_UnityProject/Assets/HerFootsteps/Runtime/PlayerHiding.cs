using UnityEngine;

namespace HerFootsteps
{
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed class PlayerHiding : MonoBehaviour
    {
        private FirstPersonMotor motor;
        private Vector3 entryPosition;
        public HidingSpot ActiveSpot { get; private set; }
        public bool IsHidden => ActiveSpot != null;
        public bool ExitBlocked { get; private set; }
        public event System.Action<bool> HiddenChanged;

        public bool TryEnter(HidingSpot spot)
        {
            if (!isActiveAndEnabled || IsHidden || spot == null || !spot.CanInteract) return false;
            if (motor == null) motor = GetComponent<FirstPersonMotor>();
            if (!PositionClear(spot.HidingPosition.position) || !spot.Claim(this)) return false;
            entryPosition = transform.position;
            ActiveSpot = spot;
            ExitBlocked = false;
            motor.SetMovementLocked(true);
            motor.Teleport(spot.HidingPosition.position);
            HiddenChanged?.Invoke(true);
            return true;
        }

        public bool TryLeave()
        {
            if (!IsHidden) return false;
            Vector3 destination = ActiveSpot.ExitPosition != null ? ActiveSpot.ExitPosition.position : entryPosition;
            if (!PositionClear(destination)) destination = entryPosition;
            if (!PositionClear(destination)) { ExitBlocked = true; return false; }
            Leave(destination);
            return true;
        }

        // Lifecycle cleanup must never strand the motor in its locked state.
        public void ForceLeave()
        {
            if (IsHidden) Leave(entryPosition);
        }

        private void Leave(Vector3 destination)
        {
            ActiveSpot.Release(this);
            ActiveSpot = null;
            ExitBlocked = false;
            motor.Teleport(destination);
            motor.SetMovementLocked(false);
            HiddenChanged?.Invoke(false);
        }

        private bool PositionClear(Vector3 position)
        {
            var controller = GetComponent<CharacterController>();
            // Root scale must be 1, as on the existing player prefab. Skin avoids floor contact rejection.
            float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
            float half = Mathf.Max(0, controller.height * 0.5f - controller.radius);
            Vector3 center = position + controller.center;
            foreach (var collider in Physics.OverlapCapsule(center + Vector3.up * half,
                center - Vector3.up * half, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (collider != controller && !collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        private void OnDisable() => ForceLeave();
    }
}
