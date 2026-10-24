using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Room
{
    // Loads the room through MRUK and converts it to a Core RoomFootprint.
    //
    // MRUK's own "Load Scene On Startup" must be OFF on the MRUK component. With it on, MRUK requests the
    // scene permission itself, but it raises nothing when the permission is denied or no room exists
    // (see MRUK.Awake / MRUK.LoadScene). This component starts the load instead, reads MRUK's
    // LoadDeviceResult for failures, and builds the footprint from MRUK.SceneLoadedEvent on success.
    //
    // On the headset: request com.oculus.permission.USE_SCENE, then load from the device (Space Setup data).
    // In the Editor: try device data first (Meta XR Simulator synthetic room or Link), then fall back to
    // one of the MRUK component's Room Prefabs.
    public class RoomLoader : MonoBehaviour
    {
        [SerializeField] private MRUK mruk;

#pragma warning disable CS0414 // Editor-only settings are unused in player builds.
        [Tooltip("Editor only: try device scene data first (Meta XR Simulator synthetic room, or Quest Link).")]
        [SerializeField] private bool useDeviceDataInEditor = true;

        [Tooltip("Editor only: if no device data is found, load one of the MRUK component's Room Prefabs.")]
        [SerializeField] private bool usePrefabFallbackInEditor = true;
#pragma warning restore CS0414

        [SerializeField] private UnityEvent<RoomFootprint> roomLoaded = new UnityEvent<RoomFootprint>();
        [SerializeField] private UnityEvent<RoomLoadFailure> roomLoadFailed = new UnityEvent<RoomLoadFailure>();

        private bool reported;

        public UnityEvent<RoomFootprint> RoomLoaded => roomLoaded;
        public UnityEvent<RoomLoadFailure> RoomLoadFailed => roomLoadFailed;
        public RoomFootprint Footprint { get; private set; } = RoomFootprint.Empty;

        private void Start()
        {
            if (mruk == null)
            {
                mruk = MRUK.Instance;
            }

            if (mruk == null)
            {
                Debug.LogError($"{nameof(RoomLoader)}: no MRUK component in the scene.");
                Fail(RoomLoadFailure.Error);
                return;
            }

            mruk.RegisterSceneLoadedCallback(OnSceneLoaded);

            if (mruk.SceneSettings.LoadSceneOnStartup)
            {
                Debug.LogWarning($"{nameof(RoomLoader)}: MRUK 'Load Scene On Startup' is on, so MRUK loads the scene and " +
                                 "permission-denied / no-room failures cannot be reported. Turn it off on the MRUK component.");
                return;
            }

            LoadAsync();
        }

        private void OnDestroy()
        {
            if (mruk != null)
            {
                mruk.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
            }
        }

        // async void is the entry point from Start; everything inside is caught so a failed load never throws.
        private async void LoadAsync()
        {
            try
            {
                MRUK.LoadDeviceResult result = await LoadFromBestSourceAsync();
                if (this == null)
                {
                    return;
                }

                if (result != MRUK.LoadDeviceResult.Success)
                {
                    Debug.LogWarning($"{nameof(RoomLoader)}: scene load finished with {result}.");
                    Fail(ToFailure(result));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Fail(RoomLoadFailure.Error);
            }
        }

#if UNITY_EDITOR
        private async Task<MRUK.LoadDeviceResult> LoadFromBestSourceAsync()
        {
            var result = MRUK.LoadDeviceResult.NoRoomsFound;

            if (useDeviceDataInEditor)
            {
                result = await mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                Debug.Log($"{nameof(RoomLoader)}: device/simulator scene load returned {result}.");
            }

            if (result != MRUK.LoadDeviceResult.Success && usePrefabFallbackInEditor)
            {
                GameObject prefab = PickRoomPrefab();
                if (prefab != null)
                {
                    Debug.Log($"{nameof(RoomLoader)}: loading MRUK room prefab '{prefab.name}'.");
                    result = await mruk.LoadSceneFromPrefab(prefab);
                }
            }

            return result;
        }

        private GameObject PickRoomPrefab()
        {
            GameObject[] prefabs = mruk.SceneSettings.RoomPrefabs;
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            int index = mruk.SceneSettings.RoomIndex;
            if (index < 0 || index >= prefabs.Length)
            {
                index = UnityEngine.Random.Range(0, prefabs.Length);
            }

            return prefabs[index];
        }
#else
        private async Task<MRUK.LoadDeviceResult> LoadFromBestSourceAsync()
        {
            if (!await EnsureScenePermissionAsync())
            {
                return MRUK.LoadDeviceResult.NoScenePermission;
            }

            // Do not launch Space Setup automatically; the status panel tells the user to run it.
            return await mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
        }

        private async Task<bool> EnsureScenePermissionAsync()
        {
#if UNITY_ANDROID
            const string permission = OVRPermissionsRequester.ScenePermission;
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                return true;
            }

            var answer = new TaskCompletionSource<bool>();
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += _ => answer.TrySetResult(true);
            callbacks.PermissionDenied += _ => answer.TrySetResult(false);
            UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks);

            // If another permission dialog was already open, Android can drop this request without calling back
            // (MRUK works around the same issue in MRUK.Update), so also poll until the permission shows up.
            while (!answer.Task.IsCompleted)
            {
                if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    answer.TrySetResult(true);
                }

                await Task.Yield();
            }

            return answer.Task.Result;
#else
            return await Task.FromResult(true);
#endif
        }
#endif

        private void OnSceneLoaded()
        {
            MRUKRoom room = mruk.GetCurrentRoom();
            if (room == null && mruk.Rooms.Count > 0)
            {
                room = mruk.Rooms[0];
            }

            if (room == null || room.FloorAnchors.Count == 0)
            {
                Debug.LogWarning($"{nameof(RoomLoader)}: scene loaded but it has no room with a floor.");
                Fail(RoomLoadFailure.NoRoomFound);
                return;
            }

            try
            {
                Footprint = BuildFootprint(room);
            }
            catch (ArgumentException exception)
            {
                Debug.LogException(exception);
                Fail(RoomLoadFailure.Error);
                return;
            }

            Debug.Log($"{nameof(RoomLoader)}: room '{room.name}' {Footprint.Width:0.00} x {Footprint.Depth:0.00} x " +
                      $"{Footprint.Height:0.00} m, area {Footprint.Area:0.00} m2, {Footprint.WallCount} outline walls, " +
                      $"{room.WallAnchors.Count} MRUK wall anchors.");
            reported = true;
            roomLoaded.Invoke(Footprint);
        }

        // The floor anchor's PlaneBoundary2D is the wall outline in the floor plane's own 2D frame
        // (the same points MRUKRoom.GetRoomOutline transforms to world space). Using the local frame keeps
        // Width/Depth aligned with the room instead of with the world axes.
        private static RoomFootprint BuildFootprint(MRUKRoom room)
        {
            MRUKAnchor floor = room.FloorAnchors[0];
            var corners = new List<Vector2>(floor.PlaneBoundary2D);

            float height = 0f;
            if (room.CeilingAnchors.Count > 0)
            {
                height = Mathf.Max(0f, room.CeilingAnchors[0].transform.position.y - floor.transform.position.y);
            }

            return new RoomFootprint(corners, height);
        }

        private void Fail(RoomLoadFailure failure)
        {
            if (reported)
            {
                return;
            }

            reported = true;
            roomLoadFailed.Invoke(failure);
        }

        private static RoomLoadFailure ToFailure(MRUK.LoadDeviceResult result)
        {
            switch (result)
            {
                case MRUK.LoadDeviceResult.NoScenePermission:
                case MRUK.LoadDeviceResult.FailurePermissionInsufficient:
                    return RoomLoadFailure.PermissionDenied;
                case MRUK.LoadDeviceResult.NoRoomsFound:
                case MRUK.LoadDeviceResult.NotInitialized:
                    return RoomLoadFailure.NoRoomFound;
                default:
                    return RoomLoadFailure.Error;
            }
        }
    }
}
