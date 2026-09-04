using System.Collections;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public interface ITapeStepFeedback
    {
        IEnumerator PlayRead(Symbol readSymbol, Symbol writeSymbol, Vector3 worldPosition);
        IEnumerator PlayWrite(TapeWriteEffectKind kind, Vector3 worldPosition);
    }
}
