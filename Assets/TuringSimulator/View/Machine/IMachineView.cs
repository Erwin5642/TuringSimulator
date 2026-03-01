using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;

namespace TuringSimulator.View.Machine
{
    public interface IMachineView
    {
        ITapeVisual Tape { get;  }
        IHaltStatusIndicator Halt { get;  }
        
        void Initialize(ITapeVisual tape, IHaltStatusIndicator statusIndicator);
        Task UpdateStepForward(StepResult step);    
        Task UpdateStepBackward(StepResult step);

        void Reset();
    }
}