using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoomPlanner.UI
{
    // World-space status line for room loading. This assembly only knows Core: the scene wires
    // RoomLoader.RoomLoaded -> ShowLoaded and RoomLoader.RoomLoadFailed -> ShowFailed as UnityEvents.
    //
    // The panel lazily follows the head: it stays put while it is roughly in view and glides back
    // in front of the user when they turn or walk away, so it never sticks to the face.
    public class RoomStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Transform head;

        [Tooltip("Horizontal distance from the head, in metres.")]
        [SerializeField] private float distance = 1.1f;

        [Tooltip("Height relative to the eyes, in metres. Slightly below eye level is easiest to read.")]
        [SerializeField] private float heightOffset = -0.2f;

        [Tooltip("Recenter when the panel is more than this many degrees off the view direction.")]
        [SerializeField] private float recenterAngle = 30f;

        [SerializeField] private float followSpeed = 4f;

        private bool placed;
        private bool recentering;

        private void OnEnable() => ShowLoading();

        public void ShowLoading() => SetText(RoomStatusFormatter.Loading());

        public void ShowLoaded(RoomFootprint room) => SetText(RoomStatusFormatter.Loaded(room));

        public void ShowFailed(RoomLoadFailure failure) => SetText(RoomStatusFormatter.Failed(failure));

        private void SetText(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void LateUpdate()
        {
            if (head == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                return; // looking straight up or down: keep the panel where it is
            }

            forward.Normalize();
            Vector3 target = head.position + forward * distance + Vector3.up * heightOffset;

            if (!placed)
            {
                transform.position = target;
                placed = true;
            }
            else
            {
                Vector3 toPanel = Vector3.ProjectOnPlane(transform.position - head.position, Vector3.up);
                bool outOfView = Vector3.Angle(toPanel, forward) > recenterAngle;
                bool wrongDistance = Mathf.Abs(toPanel.magnitude - distance) > 0.5f;
                if (outOfView || wrongDistance)
                {
                    recentering = true;
                }

                if (recentering)
                {
                    transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
                    recentering = (transform.position - target).sqrMagnitude > 0.0004f;
                }
            }

            // A world-space canvas reads correctly when its forward axis points away from the viewer.
            Vector3 away = Vector3.ProjectOnPlane(transform.position - head.position, Vector3.up);
            if (away.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }
    }
}
