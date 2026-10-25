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

        [Tooltip("Raised when the user clicks the selected item again to deselect it.")]
        [SerializeField] private UnityEvent selectionCleared = new UnityEvent();

        [Header("Layout buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Text clearAllLabel;
        [SerializeField] private string clearAllText = "Clear All";
        [SerializeField] private string clearAllConfirmText = "Confirm?";
        [SerializeField] private UnityEvent saveClicked = new UnityEvent();
        [SerializeField] private UnityEvent loadClicked = new UnityEvent();

        [Tooltip("Raised on the second Clear All press within 3 seconds.")]
        [SerializeField] private UnityEvent clearAllConfirmed = new UnityEvent();

        private readonly ConfirmGate clearAllGate = new ConfirmGate();
        private bool showingConfirm;
        private int selectedIndex = -1;

        public UnityEvent<string> ItemSelected => itemSelected;
        public UnityEvent SelectionCleared => selectionCleared;
        public UnityEvent SaveClicked => saveClicked;
        public UnityEvent LoadClicked => loadClicked;
        public UnityEvent ClearAllConfirmed => clearAllConfirmed;
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

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(saveClicked.Invoke);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(loadClicked.Invoke);
            }

            if (clearAllButton != null)
            {
                clearAllButton.onClick.AddListener(PressClearAll);
            }

            ApplyHighlight();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // First press turns the label into "Confirm?"; a second press within 3 seconds raises ClearAllConfirmed.
        public void PressClearAll()
        {
            bool confirmed = clearAllGate.Press(Time.unscaledTime);
            SetClearAllConfirm(!confirmed);
            if (confirmed)
            {
                clearAllConfirmed.Invoke();
            }
        }

        private void SetClearAllConfirm(bool confirm)
        {
            showingConfirm = confirm;
            if (clearAllLabel != null)
            {
                clearAllLabel.text = confirm ? clearAllConfirmText : clearAllText;
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

            // Clicking the selected item again deselects it, which switches back to picking up placed furniture.
            if (index == selectedIndex)
            {
                ClearSelection();
                selectionCleared.Invoke();
                return;
            }

            selectedIndex = index;
            ApplyHighlight();
            itemSelected.Invoke(items[index].Id);
        }

        // Clears the highlight only; called from placement when placing is cancelled elsewhere.
        public void ClearSelection()
        {
            selectedIndex = -1;
            ApplyHighlight();
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
            // The label only changes when the confirm window runs out, so no per-frame string work.
            if (showingConfirm && !clearAllGate.IsArmed(Time.unscaledTime))
            {
                SetClearAllConfirm(false);
            }

            if (IsShown && head != null && WorldPanelPlacement.ShouldReplace(transform, head, replaceDistance))
            {
                PlaceInView();
            }
        }

        private void PlaceInView()
        {
            if (head != null)
            {
                WorldPanelPlacement.PlaceInView(transform, head, yawOffsetDegrees, distance, heightOffset);
            }
        }
    }
}
