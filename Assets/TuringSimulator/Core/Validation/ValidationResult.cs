using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Validation
{
    public record ValidationResult
    {
        public int TestIndex;
        public string ScenarioId;
        public bool Passed;
        public HaltStatus ActualStatus;
        public HaltStatus ExpectedStatus;
        public ITape ActualTape;
        public ITape ExpectedTape;
        public string Error;
    }
}