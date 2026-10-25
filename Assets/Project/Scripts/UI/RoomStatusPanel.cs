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
    //
    // It also shows the first-use hint (OnboardingHint) under the status line while the room is loaded and
    // nothing is placed; PlacementController.MetricsChanged -> ShowMetrics reports the item count.
    // The "Room loaded" line fades out a few seconds after loading (StatusFade); loading and error messages stay.
    // The panel resizes to what is left (status, hint, or both) and disappears once both are gone.
    public class RoomStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Transform head;

        [Header("First-use hint")]
        [SerializeField] private Text hintLabel;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private float heightWithoutHint = 140f;
        [SerializeField] private float heightWithHint = 230f;
        [SerializeField] private float heightHintOnly = 120f;

        [Header("Fading")]
        [Tooltip("Seconds the room-loaded message stays before fading out.")]
        [SerializeField] private float statusHoldSeconds = StatusFade.DefaultHoldSeconds;
        [SerializeField] private float statusFadeSeconds = StatusFade.DefaultFadeSeconds;

        private readonly OnboardingHint hint = new OnboardingHint();
        private StatusFade fade;
        private Canvas canvas;
        private CanvasGroup group;
        private StatusPanelContent content = StatusPanelContent.StatusOnly;

        [Tooltip("Horizontal distance from the head, in metres.")]
        [SerializeField] private float distance = 1.1f;

        [Tooltip("Height relative to the eyes, in metres. Slightly below eye level is easiest to read.")]
        [SerializeField] private float heightOffset = -0.2f;

        [Tooltip("Recenter when the panel is more than this many degrees off the view direction.")]
        [SerializeField] private float recenterAngle = 30f;

        [SerializeField] private float followSpeed = 4f;

        private bool placed;
        private bool recentering;

        private void Awake()
        {
            fade = new StatusFade(statusHoldSeconds, statusFadeSeconds);
            canvas = GetComponent<Canvas>();
            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            if (hintLabel != null)
            {
                hintLabel.text = OnboardingHint.Text;
            }
        }

        private void OnEnable()
        {
            ShowLoading();
        }

        public void ShowLoading() => ShowSticky(RoomStatusFormatter.Loading());

        public void ShowLoaded(RoomFootprint room)
        {
            SetText(RoomStatusFormatter.Loaded(room));
            fade.Show(Time.unscaledTime);
            hint.OnRoomReady(0);
            ApplyVisibility();
        }

        public void ShowFailed(RoomLoadFailure failure) => ShowSticky(RoomStatusFormatter.Failed(failure));

        public void ShowMetrics(RoomMetrics metrics)
        {
            hint.OnItemCountChanged(metrics.ItemCount);
            ApplyVisibility();
        }

        public StatusPanelContent Content => content;
        public bool IsHintVisible => content == StatusPanelContent.HintOnly || content == StatusPanelContent.StatusAndHint;

        private void ShowSticky(string text)
        {
            SetText(text);
            fade.ShowSticky();
            ApplyVisibility();
        }

        // The status line fades on its own: with the hint showing only the line fades, otherwise the whole panel.
        private void ApplyVisibility()
        {
            float statusAlpha = fade.Alpha(Time.unscaledTime);
            bool hintVisible = hint.IsVisible && hintLabel != null;
            StatusPanelContent next = StatusFade.Content(statusAlpha > 0f, hintVisible);

            group.alpha = hintVisible ? 1f : statusAlpha;
            if (label != null)
            {
                Color color = label.color;
                color.a = hintVisible ? statusAlpha : 1f;
                label.color = color;
            }

            if (next == content)
            {
                return;
            }

            content = next;
            canvas.enabled = next != StatusPanelContent.Hidden;
            if (label != null)
            {
                label.gameObject.SetActive(next == StatusPanelContent.StatusOnly || next == StatusPanelContent.StatusAndHint);
            }

            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(hintVisible);
            }

            if (panelRect != null)
            {
                float height = next == StatusPanelContent.StatusAndHint ? heightWithHint
                    : next == StatusPanelContent.HintOnly ? heightHintOnly
                    : heightWithoutHint;
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, height);
            }
        }

        private void SetText(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void LateUpdate()
        {
            ApplyVisibility();

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
