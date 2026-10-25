using UnityEngine;

namespace RoomPlanner.UI
{
    // Puts a world-space panel in front of the user at a fixed angle from the view direction and keeps it there
    // (world-locked) so it does not run away when the user turns towards it. Panels call ShouldReplace each frame and
    // PlaceInView again only when the user has walked away from the panel.
    public static class WorldPanelPlacement
    {
        public static void PlaceInView(Transform panel, Transform head, float yawOffsetDegrees, float distance, float heightOffset)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            Vector3 direction = Quaternion.Euler(0f, yawOffsetDegrees, 0f) * forward;
            Vector3 position = head.position + direction * distance + Vector3.up * heightOffset;

            // A world-space canvas reads correctly when its forward axis points away from the viewer.
            panel.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
        }

        public static bool ShouldReplace(Transform panel, Transform head, float replaceDistance)
        {
            return Vector3.ProjectOnPlane(panel.position - head.position, Vector3.up).magnitude > replaceDistance;
        }
    }
}
