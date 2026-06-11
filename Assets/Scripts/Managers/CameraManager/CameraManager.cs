using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Spawn;
using DinoGame.Systems;
using CoreCreature = DinoGame.Core.Creature;

namespace DinoGame.Camera
{
    public enum CameraMode
    {
        FreeLook = 0,
        StateDriven = 1
    }

    [Serializable]
    public sealed class CameraRigDefinition
    {
        public CameraMode mode = CameraMode.FreeLook;
        public CinemachineCamera camera;
        [Min(0)] public int activePriority = 10;
        [Min(0)] public int inactivePriority = 0;
        public CinemachineTouchController touchController;
    }

    [Serializable]
    public sealed class CameraStateDefinition
    {
        public string stateId = "Default";
        public CinemachineCamera camera;
        [Min(0)] public int priority = 10;
    }

    /// <summary>
    /// Assigns the active player to Cinemachine rigs. Starts on FreeLook and can switch to state-driven cameras later.
    /// Player binding is event-driven (no per-frame polling).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CameraRigDefinition[] cameraRigs;
        [SerializeField] private bool autoCollectRigsFromChildren = true;

        [Header("Mode")]
        [SerializeField] private CameraMode defaultMode = CameraMode.FreeLook;

        [Header("Player Target")]
        [SerializeField] private bool autoBindPlayer = true;
        [SerializeField] private string cameraTargetChildName = "CameraTarget";
        [SerializeField] private Vector3 cameraTargetLocalOffset = new(0f, 2f, 0f);
        [SerializeField] private bool createCameraTargetIfMissing = true;

        [Header("Free Look")]
        [SerializeField] private bool resetFreeLookOrbitOnSpawn = true;

        [Header("State Driven")]
        [SerializeField] private CameraStateDefinition[] stateCameras;
        [SerializeField] private string defaultStateId = "Default";

        public CameraMode ActiveMode { get; private set; }
        public string ActiveStateId { get; private set; }
        public Transform FollowTarget { get; private set; }
        public CoreCreature TrackedPlayer { get; private set; }

        private readonly Dictionary<CameraMode, CameraRigDefinition> rigByMode = new();
        private readonly Dictionary<string, CameraStateDefinition> stateById = new();
        private readonly Dictionary<int, Transform> cameraTargetCache = new();

        private CameraRigDefinition activeRig;
        private CameraStateDefinition activeStateCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate CameraManager detected. Destroying the newer instance.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CacheReferences();
            BuildLookups();
        }

        private void OnEnable()
        {
            if (!autoBindPlayer)
                return;

            GameEventBus.CreatureSpawned += OnCreatureSpawned;
            GameEventBus.CreatureDied += OnCreatureDied;
        }

        private void OnDisable()
        {
            GameEventBus.CreatureSpawned -= OnCreatureSpawned;
            GameEventBus.CreatureDied -= OnCreatureDied;
        }

        private void Start()
        {
            SetCameraMode(defaultMode, force: true);

            if (autoBindPlayer)
                TryBindExistingPlayer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCameraMode(CameraMode mode, bool force = false)
        {
            if (!force && ActiveMode == mode)
                return;

            if (!rigByMode.TryGetValue(mode, out CameraRigDefinition rig) || rig.camera == null)
            {
                Debug.LogWarning($"CameraManager has no rig configured for mode '{mode}'.", this);
                return;
            }

            ActiveMode = mode;
            activeRig = rig;
            ApplyRigPriorities();

            if (mode == CameraMode.StateDriven)
                ApplyStateCamera(string.IsNullOrWhiteSpace(ActiveStateId) ? defaultStateId : ActiveStateId, force: true);
            else
                DeactivateStateCameras();

            if (FollowTarget != null)
                AssignTargetToRig(activeRig, FollowTarget);
        }

        public bool SetStateCamera(string stateId, bool force = false)
        {
            if (ActiveMode != CameraMode.StateDriven)
            {
                SetCameraMode(CameraMode.StateDriven);
                force = true;
            }

            return ApplyStateCamera(stateId, force);
        }

        public void BindToPlayer(CoreCreature player, bool resetFreeLookOrbit = true)
        {
            if (player == null)
                return;

            TrackedPlayer = player;
            FollowTarget = ResolveCameraTarget(player.transform);
            AssignTargetToAllRigs(FollowTarget);

            if (resetFreeLookOrbit && resetFreeLookOrbitOnSpawn)
                ResetFreeLookOrbit(FollowTarget);
        }

        public void ClearPlayerBinding()
        {
            TrackedPlayer = null;
            FollowTarget = null;
            AssignTargetToAllRigs(null);
        }

        private void CacheReferences()
        {
            if (cinemachineBrain == null && UnityEngine.Camera.main != null)
                cinemachineBrain = UnityEngine.Camera.main.GetComponent<CinemachineBrain>();

            if (!autoCollectRigsFromChildren || (cameraRigs != null && cameraRigs.Length > 0))
                return;

            CinemachineCamera[] cameras = GetComponentsInChildren<CinemachineCamera>(includeInactive: true);
            if (cameras.Length == 0)
                return;

            var collected = new List<CameraRigDefinition>(cameras.Length);
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineCamera camera = cameras[i];
                if (camera == null)
                    continue;

                collected.Add(new CameraRigDefinition
                {
                    mode = i == 0 ? CameraMode.FreeLook : CameraMode.StateDriven,
                    camera = camera,
                    activePriority = 10,
                    inactivePriority = 0,
                    touchController = camera.GetComponent<CinemachineTouchController>()
                });
            }

            cameraRigs = collected.ToArray();
        }

        private void BuildLookups()
        {
            rigByMode.Clear();
            stateById.Clear();

            if (cameraRigs != null)
            {
                for (int i = 0; i < cameraRigs.Length; i++)
                {
                    CameraRigDefinition rig = cameraRigs[i];
                    if (rig == null || rig.camera == null)
                        continue;

                    if (rig.touchController == null)
                        rig.touchController = rig.camera.GetComponent<CinemachineTouchController>();

                    rigByMode[rig.mode] = rig;
                }
            }

            if (stateCameras != null)
            {
                for (int i = 0; i < stateCameras.Length; i++)
                {
                    CameraStateDefinition state = stateCameras[i];
                    if (state == null || state.camera == null || string.IsNullOrWhiteSpace(state.stateId))
                        continue;

                    stateById[state.stateId] = state;
                }
            }
        }

        private void TryBindExistingPlayer()
        {
            if (SpawnManager.Instance != null && SpawnManager.Instance.Player != null)
                BindToPlayer(SpawnManager.Instance.Player);
        }

        private void OnCreatureSpawned(CoreCreature creature)
        {
            if (!autoBindPlayer || creature == null || creature.TeamId != (int)TeamType.Player)
                return;

            BindToPlayer(creature);
        }

        private void OnCreatureDied(CoreCreature creature, GameObject source)
        {
            if (creature != TrackedPlayer)
                return;

            if (creature != null)
                cameraTargetCache.Remove(creature.transform.GetInstanceID());

            ClearPlayerBinding();
        }

        private Transform ResolveCameraTarget(Transform playerRoot)
        {
            if (playerRoot == null)
                return null;

            int rootId = playerRoot.GetInstanceID();
            if (cameraTargetCache.TryGetValue(rootId, out Transform cached) && cached != null)
                return cached;

            Transform target = playerRoot.Find(cameraTargetChildName);
            if (target == null && createCameraTargetIfMissing)
            {
                var pivot = new GameObject(cameraTargetChildName);
                pivot.transform.SetParent(playerRoot, false);
                pivot.transform.localPosition = cameraTargetLocalOffset;
                target = pivot.transform;
            }

            if (target == null)
                target = playerRoot;

            cameraTargetCache[rootId] = target;
            return target;
        }

        private void AssignTargetToAllRigs(Transform target)
        {
            if (cameraRigs == null)
                return;

            for (int i = 0; i < cameraRigs.Length; i++)
            {
                CameraRigDefinition rig = cameraRigs[i];
                if (rig?.camera == null)
                    continue;

                AssignTargetToRig(rig, target);
            }

            if (stateCameras != null)
            {
                for (int i = 0; i < stateCameras.Length; i++)
                {
                    CameraStateDefinition state = stateCameras[i];
                    if (state?.camera == null)
                        continue;

                    AssignFollowTarget(state.camera, target);
                }
            }
        }

        private static void AssignTargetToRig(CameraRigDefinition rig, Transform target)
        {
            if (rig?.camera == null)
                return;

            AssignFollowTarget(rig.camera, target);
        }

        private static void AssignFollowTarget(CinemachineCamera camera, Transform target)
        {
            camera.Target.TrackingTarget = target;
            camera.Target.LookAtTarget = target;
        }

        private void ApplyRigPriorities()
        {
            if (cameraRigs == null)
                return;

            for (int i = 0; i < cameraRigs.Length; i++)
            {
                CameraRigDefinition rig = cameraRigs[i];
                if (rig?.camera == null)
                    continue;

                bool isActive = rig == activeRig;
                rig.camera.Priority.Value = isActive ? rig.activePriority : rig.inactivePriority;
                rig.camera.StandbyUpdate = isActive
                    ? CinemachineVirtualCameraBase.StandbyUpdateMode.Always
                    : CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
            }
        }

        private bool ApplyStateCamera(string stateId, bool force)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                stateId = defaultStateId;

            if (!force && ActiveStateId == stateId && activeStateCamera != null)
                return true;

            if (!stateById.TryGetValue(stateId, out CameraStateDefinition nextState) || nextState.camera == null)
            {
                Debug.LogWarning($"CameraManager could not find a state camera with id '{stateId}'.", this);
                return false;
            }

            ActiveStateId = stateId;
            activeStateCamera = nextState;
            DeactivateStateCameras();

            nextState.camera.Priority.Value = nextState.priority;
            nextState.camera.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;

            if (FollowTarget != null)
                AssignFollowTarget(nextState.camera, FollowTarget);

            return true;
        }

        private void DeactivateStateCameras()
        {
            if (stateCameras == null)
                return;

            for (int i = 0; i < stateCameras.Length; i++)
            {
                CameraStateDefinition state = stateCameras[i];
                if (state?.camera == null)
                    continue;

                state.camera.Priority.Value = 0;
                state.camera.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
            }

            activeStateCamera = null;
        }

        private void ResetFreeLookOrbit(Transform target)
        {
            if (!rigByMode.TryGetValue(CameraMode.FreeLook, out CameraRigDefinition freeLookRig))
                return;

            CinemachineTouchController touch = freeLookRig.touchController;
            if (touch == null || target == null)
                return;

            if (cinemachineBrain != null && cinemachineBrain.OutputCamera != null)
                touch.SetInitialOrbitFromWorldPose(cinemachineBrain.OutputCamera.transform.position, target);
            else
                touch.ResetToInitialOrbit();
        }
    }
}
