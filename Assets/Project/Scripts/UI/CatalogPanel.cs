using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoomPlanner.UI
{
    // World-space furniture catalog: one button per FurnitureCatalog item (labels from FurnitureLabelFormatter),
    // highlights the selected one and raises ItemSelected(id). Hidden until Show(), which the scene wires to
    // RoomLoader.RoomLoaded. The controller ray reaches the buttons through the Interaction SDK
    // (RayInteractable + PointableCanvas on the canvas, PointableCanvasModule on the EventSystem).
    //
    // The panel is placed once, to the left of the view at about 1 m, and then stays put, so it does not
    // run away when the user turns towards it. It is only placed again if the user walks away from it.
    public class CatalogPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] buttons = new Button[0];
        [SerializeField] private Transform head;

        [Tooltip("Horizontal distance from the head, in metres.")]
        [SerializeField] private float distance = 1.0f;

        [Tooltip("Degrees to the left (negative) or right of the view direction.")]
        [SerializeField] private float yawOffsetDegrees = -35f;

        [SerializeField] private float heightOffset = -0.15f;

        [Tooltip("Place the panel in front of the user again when they are farther than this from it.")]
        [SerializeField] private float replaceDistance = 2.0f;

        [SerializeField] private Color normalColor = new Color(0.16f, 0.18f, 0.22f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.10f, 0.62f, 0.55f, 1f);

        [SerializeField] private UnityEvent<string> itemSelected = new UnityEvent<string>();

        private int selectedIndex = -1;

        public UnityEvent<string> ItemSelected => itemSelected;
        public bool IsShown => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            IReadOnlyList<FurnitureDefinition> items = FurnitureCatalog.Default;
            if (buttons.Length != items.Count)
            {
                Debug.LogWarning($"{nameof(CatalogPanel)}: {buttons.Length} buttons for {items.Count} catalog items.");
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                bool used = i < items.Count;
                buttons[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                int index = i;
                Text label = buttons[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = FurnitureLabelFormatter.Format(items[i]);
                }

                buttons[i].onClick.AddListener(() => SelectIndex(index));
            }

            ApplyHighlight();
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
            PlaceInView();
        }

        public void SelectIndex(int index)
        {
            IReadOnlyList<FurnitureDefinition> items = FurnitureCatalog.Default;
            if (index < 0 || index >= items.Count)
            {
                return;
            }

            selectedIndex = index;
            ApplyHighlight();
            itemSelected.Invoke(items[index].Id);
        }

        private void ApplyHighlight()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].image.color = i == selectedIndex ? selectedColor : normalColor;
            }
        }

        private void LateUpdate()
        {
            if (!IsShown || head == null)
            {
                return;
            }

            Vector3 offset = Vector3.ProjectOnPlane(transform.position - head.position, Vector3.up);
            if (offset.magnitude > replaceDistance)
            {
                PlaceInView();
            }
        }

        private void PlaceInView()
        {
            if (head == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            Vector3 direction = Quaternion.Euler(0f, yawOffsetDegrees, 0f) * forward;
            Vector3 position = head.position + direction * distance + Vector3.up * heightOffset;

            // A world-space canvas reads correctly when its forward axis points away from the viewer.
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
        }
    }
}
