using System;
using TuringSimulator.View;
using UnityEngine;
using TuringSimulator.View.Machine;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;

namespace TuringSimulator.GameFlow
{
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
        
        public ViewPrefabs Prefabs { get; private set; }

        public ViewInstaller(ViewPrefabs prefabs)
        {
            if (prefabs == null) throw new ArgumentNullException(nameof(prefabs));
            if (prefabs.machine == null) throw new ArgumentNullException(nameof(prefabs.machine));
            if (prefabs.tape == null) throw new ArgumentNullException(nameof(prefabs.tape));
            if (prefabs.halt == null) throw new ArgumentNullException(nameof(prefabs.halt));
            if (prefabs.levelUI == null) throw new ArgumentNullException(nameof(prefabs.levelUI));
            Prefabs = prefabs;
        }

        public void Install() { 
            Machine = UnityEngine.Object.Instantiate(Prefabs.machine)
                .GetComponent<IMachineView>();
            Tape = UnityEngine.Object.Instantiate(Prefabs.tape)
                .GetComponent<ITapeVisual>();
            Halt = UnityEngine.Object.Instantiate(Prefabs.halt)
                .GetComponent<IHaltStatusIndicator>();
            LevelUI = UnityEngine.Object.Instantiate(Prefabs.levelUI)
                .GetComponent<LevelUI>();

            Tape.Initialize();
            Halt.Initialize();
            Machine.Initialize(Tape, Halt);
        }
    }
}