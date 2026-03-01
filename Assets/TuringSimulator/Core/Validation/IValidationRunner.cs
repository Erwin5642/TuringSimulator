using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;

namespace TuringSimulator.Core.Validation
{
    public interface IValidationRunner
    {
        void SetTests(ValidationTest[] tests);
        void SetProgram(IProgram program);
        
        bool AllPassed { get; }

        Task Start(CancellationToken cancellationToken = default);
        void Cancel();
    }
}