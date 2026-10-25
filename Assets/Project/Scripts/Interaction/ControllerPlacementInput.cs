using Oculus.Interaction;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RoomPlanner.Interaction
{
    // Bridges the Touch controllers to placement events (names verified in OVRInput.cs, per-controller mappings):
    //   right trigger (Button.PrimaryIndexTrigger, RTouch)  -> TriggerPressed   place / pick up / drop
    //   right grip    (Button.PrimaryHandTrigger, RTouch)   -> GripPressed      cancel
    //   B             (Button.Two, RTouch = RawButton.B)    -> DeletePressed
    //   X             (Button.One, LTouch = RawButton.X)    -> UndoPressed
    //   Y             (Button.Two, LTouch = RawButton.Y)    -> RedoPressed
    //   right thumbstick left/right                          -> RotateStepped(-1/+1), one step per push
    // While the Interaction SDK ray (uiRayInteractor) is hovering a UI panel, the right-hand actions belong to the
    // UI and are not raised; PointingAtUiChanged lets placement hide the ghost meanwhile. Undo/redo on the left
    // controller are not pointing actions and always work.
    public class ControllerPlacementInput : MonoBehaviour
    {
        [Tooltip("Optional: the right controller's Interaction SDK RayInteractor used for UI.")]
        [SerializeField] private RayInteractor uiRayInteractor;

        [FormerlySerializedAs("placePressed")]
        [SerializeField] private UnityEvent triggerPressed = new UnityEvent();
        [SerializeField] private UnityEvent gripPressed = new UnityEvent();
        [SerializeField] private UnityEvent deletePressed = new UnityEvent();
        [SerializeField] private UnityEvent undoPressed = new UnityEvent();
        [SerializeField] private UnityEvent redoPressed = new UnityEvent();
        [SerializeField] private UnityEvent<int> rotateStepped = new UnityEvent<int>();
        [SerializeField] private UnityEvent<bool> pointingAtUiChanged = new UnityEvent<bool>();

        private readonly AxisStepDetector thumbstick = new AxisStepDetector();
        private bool pointingAtUi;

        public UnityEvent TriggerPressed => triggerPressed;
        public UnityEvent GripPressed => gripPressed;
        public UnityEvent DeletePressed => deletePressed;
        public UnityEvent UndoPressed => undoPressed;
        public UnityEvent RedoPressed => redoPressed;
        public UnityEvent<int> RotateStepped => rotateStepped;
        public UnityEvent<bool> PointingAtUiChanged => pointingAtUiChanged;

        private void Update()
        {
            bool overUi = uiRayInteractor != null && uiRayInteractor.isActiveAndEnabled && uiRayInteractor.HasInteractable;
            if (overUi != pointingAtUi)
            {
                pointingAtUi = overUi;
                pointingAtUiChanged.Invoke(overUi);
            }

            int step = thumbstick.Update(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x);
            if (!overUi)
            {
                if (step != 0)
                {
                    rotateStepped.Invoke(step);
                }

                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    triggerPressed.Invoke();
                }

                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                {
                    gripPressed.Invoke();
                }

                if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                {
                    deletePressed.Invoke();
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                undoPressed.Invoke();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            {
                redoPressed.Invoke();
            }
        }
    }
}
