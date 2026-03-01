using System.Collections;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;
using UnityEngine.Serialization;

namespace TuringSimulator.View.Machine
{
    public class MachineViewer : MonoBehaviour, IMachineView
    {
        private ITapeVisual _tape;
        private IHaltStatusIndicator _halt;

        public ITapeVisual Tape => _tape;
        public IHaltStatusIndicator Halt => _halt;

        public void Initialize(ITapeVisual tape, IHaltStatusIndicator statusIndicator)
        {
            _tape = tape;
            _halt = statusIndicator;
        }

        public Task UpdateStepForward(StepResult step)
        {
            return RunCoroutine(UpdateStepForwardCoroutine(step));
        }

        public Task UpdateStepBackward(StepResult step)
        {
            return RunCoroutine(UpdateStepBackwardCoroutine(step));
        }

        private IEnumerator UpdateStepForwardCoroutine(StepResult step)
        {
            switch (step.Kind)
            {
                case ResultKind.Halt:
                    yield return _halt.Show(step.AsHalt());
                    break;

                case ResultKind.Diff:
                    var diff = step.AsDiff();
                    
                    yield return _tape.ShowRead();
                    
                    yield return _tape.ShowWrite(diff.SymbolAfter);
                    
                    yield return _tape.MoveHead(diff.DirectionMoved);

                    break;
            }
        }

        private IEnumerator UpdateStepBackwardCoroutine(StepResult step)
        {
            switch (step.Kind)
            {
                case ResultKind.Halt:
                    yield return _halt.Show(step.AsHalt());
                    break;

                case ResultKind.Diff:
                    var diff = step.AsDiff();
                    
                    yield return _tape.MoveHead(diff.DirectionMoved);
                    
                    yield return _tape.ShowWrite(diff.SymbolAfter);
                    
                    yield return _tape.ShowRead();

                    break;
            }
        }

        private Task RunCoroutine(IEnumerator coroutine)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(CoroutineWrapper(coroutine, tcs));
            return tcs.Task;
        }

        private IEnumerator CoroutineWrapper(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
        {
            yield return coroutine;
            tcs.SetResult(true);
        }

        public void Reset()
        {
            Tape.Reset();
            Halt.Reset();
        }
    }
}
