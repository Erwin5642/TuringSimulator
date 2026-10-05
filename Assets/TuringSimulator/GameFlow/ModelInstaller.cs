using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Validation;

namespace TuringSimulator.GameFlow
{
    public sealed class ModelInstaller
    {
        public LevelContext Levels { get; }
        public LevelLoader LevelLoader { get; }
        
        public SimulationRunner Simulation { get; }
        public SimulationBuffer Buffer { get; }
        public IValidationRunner Validation { get; }
        public IPlayTestSelector PlayTestSelector { get; }
        public ValidationTest ActivePlayTest { get; set; }
        public IProgram CurrentProgram { get; set; }
        public SimulationTape CurrentTape { get; set; }

        public ModelInstaller(LevelDatabase database)
            : this(database, new PlayTestSelector())
        {
        }

        public ModelInstaller(LevelDatabase database, IPlayTestSelector playTestSelector)
        {
            Levels = new LevelContext();
            LevelLoader = new LevelLoader(database, Levels);
            
            Buffer = new SimulationBuffer();
            Simulation = new SimulationRunner(Buffer);
            Validation = new ValidationRunner();
            PlayTestSelector = playTestSelector ?? throw new System.ArgumentNullException(nameof(playTestSelector));
        }
        
        public void Install() {}
    }
}
