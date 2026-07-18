using System;
using TuringSimulator.View;
using UnityEngine;
using TuringSimulator.View.Machine;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;

namespace TuringSimulator.GameFlow
{
    [Serializable]
    public class ViewSceneBindings
    {
        [Tooltip("Scene object implementing IMachineView.")]
        public MonoBehaviour machine;
        [Tooltip("Scene object implementing ITapeVisual.")]
        public MonoBehaviour tape;
        [Tooltip("Scene object implementing IHaltStatusIndicator.")]
        public MonoBehaviour halt;
        public LevelUI levelUI;
    }

    [Serializable]
    public class ViewPrefabs
    {
        public GameObject machine;
        public GameObject tape;
        public GameObject halt;
        public GameObject levelUI;
    }

    public sealed class ViewInstaller
    {
        public IMachineView Machine { get; private set;  }
        public ITapeVisual Tape { get; private set;  }
        public IHaltStatusIndicator Halt { get;  private set; }
        
        public LevelUI LevelUI { get; private set;  }
        
        readonly ViewPrefabs _prefabs;
        readonly ViewSceneBindings _scene;
        readonly bool _useSceneBindings;

        public ViewInstaller(ViewPrefabs prefabs)
        {
            if (prefabs == null) throw new ArgumentNullException(nameof(prefabs));
            if (prefabs.machine == null) throw new ArgumentNullException(nameof(prefabs.machine));
            if (prefabs.tape == null) throw new ArgumentNullException(nameof(prefabs.tape));
            if (prefabs.halt == null) throw new ArgumentNullException(nameof(prefabs.halt));
            if (prefabs.levelUI == null) throw new ArgumentNullException(nameof(prefabs.levelUI));
            _prefabs = prefabs;
            _useSceneBindings = false;
        }

        public ViewInstaller(ViewSceneBindings sceneBindings)
        {
            if (sceneBindings == null) throw new ArgumentNullException(nameof(sceneBindings));
            if (sceneBindings.machine == null) throw new ArgumentNullException(nameof(sceneBindings.machine));
            if (sceneBindings.tape == null) throw new ArgumentNullException(nameof(sceneBindings.tape));
            if (sceneBindings.halt == null) throw new ArgumentNullException(nameof(sceneBindings.halt));
            if (sceneBindings.levelUI == null) throw new ArgumentNullException(nameof(sceneBindings.levelUI));
            _scene = sceneBindings;
            _useSceneBindings = true;
        }

        public bool CanUseSceneBindings() =>
            _scene != null &&
            _scene.machine != null &&
            _scene.tape != null &&
            _scene.halt != null &&
            _scene.levelUI != null;

        public void Install() {
            if (_useSceneBindings)
            {
                Machine = ResolveInterface<IMachineView>(_scene.machine, nameof(_scene.machine));
                Tape = ResolveInterface<ITapeVisual>(_scene.tape, nameof(_scene.tape));
                Halt = ResolveInterface<IHaltStatusIndicator>(_scene.halt, nameof(_scene.halt));
                LevelUI = _scene.levelUI;
            }
            else
            {
                Machine = UnityEngine.Object.Instantiate(_prefabs.machine)
                    .GetComponent<IMachineView>();
                Tape = UnityEngine.Object.Instantiate(_prefabs.tape)
                    .GetComponent<ITapeVisual>();
                Halt = UnityEngine.Object.Instantiate(_prefabs.halt)
                    .GetComponent<IHaltStatusIndicator>();
                LevelUI = UnityEngine.Object.Instantiate(_prefabs.levelUI)
                    .GetComponent<LevelUI>();
            }

            Tape.Initialize();
            Halt.Initialize();
            Machine.Initialize(Tape, Halt);
        }

        static TInterface ResolveInterface<TInterface>(MonoBehaviour source, string fieldName)
            where TInterface : class
        {
            if (source is TInterface direct)
                return direct;

            var fromGo = source.GetComponent(typeof(TInterface)) as TInterface;
            if (fromGo != null)
                return fromGo;

            throw new InvalidOperationException(
                $"View binding '{fieldName}' must implement {typeof(TInterface).Name}.");
        }
    }
}