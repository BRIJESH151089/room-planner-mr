using Oculus.Interaction;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Interaction
{
    // Bridges the right Touch controller to placement events:
    //   trigger press -> PlacePressed, thumbstick pushed right/left -> RotateStepped(+1/-1), one step per push.
    // While the Interaction SDK ray (uiRayInteractor) is hovering a UI panel the trigger belongs to the UI,
    // so PlacePressed is not raised; PointingAtUiChanged lets the ghost hide meanwhile.
    public class ControllerPlacementInput : MonoBehaviour
    {
        [Tooltip("Optional: the right controller's Interaction SDK RayInteractor used for UI.")]
        [SerializeField] private RayInteractor uiRayInteractor;

        [SerializeField] private UnityEvent placePressed = new UnityEvent();
        [SerializeField] private UnityEvent<int> rotateStepped = new UnityEvent<int>();
        [SerializeField] private UnityEvent<bool> pointingAtUiChanged = new UnityEvent<bool>();

        private readonly AxisStepDetector thumbstick = new AxisStepDetector();
        private bool pointingAtUi;

        public UnityEvent PlacePressed => placePressed;
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
            if (step != 0)
            {
                rotateStepped.Invoke(step);
            }

            if (!overUi && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                placePressed.Invoke();
            }
        }
    }
}
