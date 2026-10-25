using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoomPlanner.UI
{
    // World-space measurements panel: "Room: W x D m", "Floor", "Used", "Free" (m2) and "Items", from
    // MetricsFormatter, plus a status line for save/load messages. Text changes only when the scene calls
    // ShowMetrics (PlacementController.MetricsChanged, raised after a change) or ShowStatus
    // (LayoutPersistence.StatusChanged), so nothing is formatted per frame. Hidden until Show()
    // (RoomLoader.RoomLoaded); placed once to the right of the view and only re-placed if the user walks away.
    public class MetricsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text metricsLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Transform head;
        [SerializeField] private float distance = 1.0f;

        [Tooltip("Degrees to the left (negative) or right of the view direction.")]
        [SerializeField] private float yawOffsetDegrees = 35f;

        [Tooltip("Height of the panel centre relative to the eyes, in metres. At eye level the panel stays above floor furniture.")]
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private float replaceDistance = 2.0f;

        public bool IsShown => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Show()
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(true);
            if (head != null)
            {
                WorldPanelPlacement.PlaceInView(transform, head, yawOffsetDegrees, distance, heightOffset);
            }
        }

        public void ShowMetrics(RoomMetrics metrics)
        {
            if (metricsLabel != null)
            {
                metricsLabel.text = MetricsFormatter.All(metrics);
            }
        }

        public void ShowStatus(string status)
        {
            if (statusLabel != null)
            {
                statusLabel.text = status;
            }
        }

        private void LateUpdate()
        {
            if (IsShown && head != null && WorldPanelPlacement.ShouldReplace(transform, head, replaceDistance))
            {
                WorldPanelPlacement.PlaceInView(transform, head, yawOffsetDegrees, distance, heightOffset);
            }
        }
    }
}
