using System.Collections;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;

namespace TuringSimulator.View.Machine
{
    public class MachineViewer : MonoBehaviour, IMachineView
    {
        private ITapeVisual _tape;

        public ITapeVisual Tape => _tape;

        public void Initialize(ITapeVisual tape)
        {
            _tape = tape;
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
            if (_tape == null)
            {
                Debug.LogWarning("[MachineViewer] UpdateStepForward called before Initialize(tape).", this);
                yield break;
            }

            switch (step.Kind)
            {
                case ResultKind.Halt:
                    yield break;

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
            if (_tape == null)
            {
                Debug.LogWarning("[MachineViewer] UpdateStepBackward called before Initialize(tape).", this);
                yield break;
            }

            switch (step.Kind)
            {
                case ResultKind.Halt:
                    yield break;

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
            if (_tape == null)
            {
                Debug.LogWarning("[MachineViewer] Reset called before Initialize(tape). Check ViewSceneBindings wiring in TuringBootstrap.", this);
                return;
            }

            _tape.Reset();
        }
    }
}
