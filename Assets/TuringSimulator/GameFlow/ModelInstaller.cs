using TuringSimulator.Controller;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation;
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

        public ModelInstaller(LevelDatabase database)
        {
            Levels = new LevelContext();
            LevelLoader = new LevelLoader(database, Levels);
            
            Buffer = new SimulationBuffer();
            Simulation = new SimulationRunner(Buffer);
            Validation = new ValidationRunner();
        }
        
        public void Install() {}
    }
}