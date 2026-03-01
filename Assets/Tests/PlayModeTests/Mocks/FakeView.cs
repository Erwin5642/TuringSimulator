/*using System;
using System.Threading.Tasks;
using Tests.EditModeTests.Mocks;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace Tests.PlayModeTests.Mocks
{
    public class FakeView : IMachineView
    {
        private readonly TestSimulationTape _simulationTape = new ();
        public HaltStatus Status { get; private set;  } 

        public Symbol[] GetTape()
        {
            return _simulationTape.ToArray();
        }

        public void Initialize(ITapeVisual tape, IHaltStatusIndicator statusIndicator)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateStepForward(StepResult step)
        {
            switch (step.Kind)
            {
                case ResultKind.Diff:
                {
                    var diff = step.AsDiff();
                    _simulationTape.Write(diff.SymbolAfter); 
                    _simulationTape.Move(diff.DirectionMoved);
                    break;
                }
                case ResultKind.Halt:
                {
                    var halt = step.AsHalt();
                    Status = halt;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            await Task.Delay(100);
        }

        public async Task UpdateStepBackward(StepResult step)
        {
            switch (step.Kind)
            {
                case ResultKind.Diff:
                {
                    var diff = step.AsDiff();
                    _simulationTape.Move(diff.DirectionMoved);
                    _simulationTape.Write(diff.SymbolAfter);
                    break;
                }
                case ResultKind.Halt:
                {
                    var halt = step.AsHalt();
                    Status = halt;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            await Task.Delay(100);
        }
    }
}*/