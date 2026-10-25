using UnityEngine;

namespace RoomPlanner.Room
{
    // Quest GPU savings applied at startup (Meta XR Foveation is enabled for Android in the OpenXR settings):
    // foveated rendering renders the lens edges, where it is hard to notice, at lower resolution. With dynamic
    // foveation the runtime only raises the level up to `foveationLevel` when the GPU actually needs headroom.
    // Does nothing where foveation is not supported (Editor without a headset, Link on some setups).
    public class QuestPerformanceSettings : MonoBehaviour
    {
        [SerializeField] private OVRManager.FoveatedRenderingLevel foveationLevel = OVRManager.FoveatedRenderingLevel.High;
        [SerializeField] private bool dynamicFoveation = true;

        private void Start()
        {
            if (!OVRManager.fixedFoveatedRenderingSupported)
            {
                Debug.Log($"{nameof(QuestPerformanceSettings)}: foveated rendering not supported here; skipped.");
                return;
            }

            OVRManager.foveatedRenderingLevel = foveationLevel;
            OVRManager.useDynamicFoveatedRendering = dynamicFoveation;
            Debug.Log($"{nameof(QuestPerformanceSettings)}: foveation {OVRManager.foveatedRenderingLevel}, dynamic {OVRManager.useDynamicFoveatedRendering}.");
        }
    }
}
