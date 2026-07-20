using System;
using System.Threading.Tasks;
using TuringSimulator.Core.Level;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Legacy composition root.
    /// Prefer editor-assigned scene bindings over runtime prefab instantiation.
    /// </summary>
    public class TuringBootstrap : MonoBehaviour
    {
        public static TuringBootstrap Instance { get; private set; }

        [Header("Preferred: Editor Scene Wiring")]
        [SerializeField] private ViewSceneBindings viewSceneBindings;
        [SerializeField] private ControllerSceneBindings controllerSceneBindings;
        
        [Header("Legacy Fallback: Prefab Wiring")]
        [SerializeField] private ViewPrefabs viewPrefabs;
        [SerializeField] private ControllerPrefabs controllerPrefabs;

        [Header("Levels")]
        [SerializeField] private LevelDatabase levelDatabase;

        [Header("ITS Components (assign in editor)")]
        [SerializeField] private ITSClient itsClient;
        [SerializeField] private SkillTracker skillTracker;
        [SerializeField] private AgentTTS agentTTS;
        [SerializeField] private AgentDialogue agentDialogue;
        [SerializeField] private LiveTutorSocket liveTutorSocket;

        [Header("Behavior")]
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool dontDestroyRoot = true;

        private ModelInstaller _modelInstaller;
        private ViewInstaller _viewInstaller;
        private ControllerInstaller _controllerInstaller;

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyRoot)
                DontDestroyOnLoad(gameObject);

            try
            {
                BindObjects();
                await InitializeObjects();
                await CreateObjects();
                await PrepareGameObjects();
                if (autoStartOnAwake)
                    await BeginGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] Failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private void BindObjects()
        {
            // Editor-first wiring: use assigned references first, then same-root lookups.
            itsClient ??= GetComponent<ITSClient>();
            skillTracker ??= GetComponent<SkillTracker>();
            agentTTS ??= GetComponent<AgentTTS>();
            agentDialogue ??= GetComponent<AgentDialogue>();
            liveTutorSocket ??= GetComponent<LiveTutorSocket>();

            itsClient ??= ITSClient.Instance;
            skillTracker ??= SkillTracker.Instance;
            agentTTS ??= AgentTTS.Instance;
            agentDialogue ??= AgentDialogue.Instance;
            liveTutorSocket ??= LiveTutorSocket.Instance;

            if (itsClient == null) Debug.LogWarning("[Bootstrap] ITSClient is not assigned.");
            if (skillTracker == null) Debug.LogWarning("[Bootstrap] SkillTracker is not assigned.");
            if (agentTTS == null) Debug.LogWarning("[Bootstrap] AgentTTS is not assigned.");
            if (agentDialogue == null) Debug.LogWarning("[Bootstrap] AgentDialogue is not assigned.");
            if (liveTutorSocket == null) Debug.LogWarning("[Bootstrap] LiveTutorSocket is not assigned.");
        }

        private Task InitializeObjects()
        {
            _modelInstaller = new ModelInstaller(levelDatabase);
            _modelInstaller.Install();
            
            return Task.CompletedTask;
        }

        private Task CreateObjects()
        {
            if (viewSceneBindings != null &&
                viewSceneBindings.machine != null &&
                viewSceneBindings.tape != null &&
                viewSceneBindings.halt != null &&
                viewSceneBindings.levelUI != null)
            {
                _viewInstaller = new ViewInstaller(viewSceneBindings);
                Debug.Log("[Bootstrap] Using editor scene view bindings.");
            }
            else
            {
                _viewInstaller = new ViewInstaller(viewPrefabs);
                Debug.LogWarning("[Bootstrap] Using legacy prefab view fallback.");
            }

            _viewInstaller.Install();

            return Task.CompletedTask;
        }

        private Task PrepareGameObjects()
        {
            if (controllerSceneBindings != null && controllerSceneBindings.input != null)
            {
                _controllerInstaller = new ControllerInstaller(
                    controllerSceneBindings,
                    _modelInstaller,
                    _viewInstaller);
                Debug.Log("[Bootstrap] Using editor scene controller bindings.");
            }
            else
            {
                _controllerInstaller = new ControllerInstaller(
                    controllerPrefabs,
                    _modelInstaller,
                    _viewInstaller);
                Debug.LogWarning("[Bootstrap] Using legacy prefab controller fallback.");
            }

            _controllerInstaller.Install();

            return Task.CompletedTask;
        }

        private async Task BeginGame()
        {
            if (SkillTracker.Instance != null)
            {
                var sessionId = SkillTracker.Instance.StudentId;
                if (SceneReloadSessionState.TryConsumeStudent(out var preservedStudentId))
                    sessionId = preservedStudentId;

                if (string.IsNullOrWhiteSpace(sessionId) && ITSClient.Instance != null)
                    sessionId = await ITSClient.Instance.RequestNewSessionAsync();

                SkillTracker.Instance.BeginSession(sessionId);
            }

            _controllerInstaller.GameFlowController.Start();
        }

        /// <summary>
        /// Hook this from a future main-menu Start button to force a fresh student session.
        /// </summary>
        public async void StartFromMainMenu()
        {
            if (SkillTracker.Instance == null || ITSClient.Instance == null)
            {
                Debug.LogWarning("[Bootstrap] Cannot start session from menu: missing ITS components.");
                return;
            }

            var studentId = await ITSClient.Instance.RequestNewSessionAsync();
            SkillTracker.Instance.BeginSession(studentId);
            _controllerInstaller?.GameFlowController.Start();
        }

        /// <summary>
        /// XR/VR-facing command: starts from menu or runs from editing.
        /// Wire this to world-space button events instead of keyboard input.
        /// </summary>
        public void StartOrRunFromInteraction()
        {
            if (_controllerInstaller == null)
            {
                Debug.LogWarning("[Bootstrap] Cannot process Start/Run command: controller installer not ready.");
                return;
            }

            _controllerInstaller.RequestStartOrRun();
        }

        public void PausePlaybackFromInteraction()
        {
            _controllerInstaller?.RequestPausePlayback();
        }

        public void PlayPlaybackFromInteraction()
        {
            _controllerInstaller?.RequestPlayPlayback();
        }

        public void StepForwardFromInteraction()
        {
            _controllerInstaller?.RequestStepForward();
        }

        public void StepBackwardFromInteraction()
        {
            _controllerInstaller?.RequestStepBackward();
        }

        public void NextLevelFromInteraction()
        {
            _controllerInstaller?.RequestNextLevel();
        }

        /// <summary>
        /// Hook this from a future main-menu Return button to detach active student session.
        /// </summary>
        public void ReturnToMainMenu()
        {
            _controllerInstaller?.RequestReturnToMenu();
        }

        /// <summary>
        /// Stops the current simulation and tears down this scene's bootstrap before
        /// reloading the scene. The active student remains the same.
        /// </summary>
        public void PrepareForSceneReload()
        {
            SceneReloadSessionState.PreserveStudent(SkillTracker.Instance?.StudentId);
            _modelInstaller?.Simulation.Cancel();
            LiveTutorSocket.Instance?.ClearActiveStudentSession();

            dontDestroyRoot = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

    }
}
