using System;
using System.Threading.Tasks;
using TuringSimulator.Core.Level;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public class TuringBootstrap : MonoBehaviour
    {
        [Header("View Prefabs")]
        [SerializeField] private ViewPrefabs viewPrefabs;
        
        [Header("Controller Prefabs")]
        [SerializeField] private ControllerPrefabs controllerPrefabs;

        [Header("Levels")]
        [SerializeField] private LevelDatabase levelDatabase;

        // Installers
        private ModelInstaller _modelInstaller;
        private ViewInstaller _viewInstaller;
        private ControllerInstaller _controllerInstaller;

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            try
            {
                BindObjects();
                await InitializeObjects();
                await CreateObjects();
                await PrepareGameObjects();
                await BeginGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] Failed: {e.Message}\n{e.StackTrace}");
            }
        }

        // Phase 1 – Foundation
        private void BindObjects()
        {
            AddComponentIfMissing<ITSClient>();
            AddComponentIfMissing<SkillTracker>();
            AddComponentIfMissing<AgentTTS>();
            AddComponentIfMissing<AgentDialogue>();
            AddComponentIfMissing<LiveTutorSocket>();
        }

        private void AddComponentIfMissing<TComponent>()
            where TComponent : Component
        {
            if (GetComponent<TComponent>() == null)
                gameObject.AddComponent<TComponent>();
        }

        // Phase 2 – Data / Services
        private Task InitializeObjects()
        {
            // 1. Model installer: simulation, tape, program, buffer, validation
            _modelInstaller = new ModelInstaller(levelDatabase);
            _modelInstaller.Install();
            
            return Task.CompletedTask;
        }

        // Phase 3 – Views
        private Task CreateObjects()
        {
            // 2. View installer: machine, tape, halt
            _viewInstaller = new ViewInstaller(viewPrefabs);
            _viewInstaller.Install();

            return Task.CompletedTask;
        }

        //Phase 4 – Controllers
        private Task PrepareGameObjects()
        {
            // 3. Controller installer: playback, step applier, input, game flow
            _controllerInstaller = new ControllerInstaller(
                controllerPrefabs,
                _modelInstaller,
                _viewInstaller);

            // 4. Start controllers (wire events, subscriptions, and internal state)
            _controllerInstaller.Install();

            return Task.CompletedTask;
        }

        //Phase 5 – Run
        private Task BeginGame()
        {
            _controllerInstaller.GameFlowController.Start();
            return Task.CompletedTask;
        }

    }
}
