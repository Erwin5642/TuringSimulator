using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyLibrary.Scripts
{
    public class TM : MonoBehaviour
    {
        public List<Tape> tapes;

        public void Reset()
        {
            foreach (var tape in tapes)
            {
                tape.ClearTape();
            }
        }

        public void SetTape(SymbolType[] symbols, int tapeIndex)
        {
            tapes[tapeIndex].ClearTape();
            StartCoroutine(_SetTape(symbols, tapeIndex));
        }

        private IEnumerator _SetTape(SymbolType[] symbols, int tapeIndex)
        {
            foreach (var symbol in symbols)
            {
                tapes[tapeIndex].Write(symbol);
                yield return tapes[tapeIndex].Move(1);
            }

            tapes[tapeIndex].ResetPosition();
        }

        public IEnumerator ExecuteProgram(int[] parameters, Action<bool> callback)
        {
            if (parameters.Length % 3 != 0 || parameters.Length / 3 > tapes.Count)
            {
                callback?.Invoke(false);
                yield break;
            }

            int instructionCount = parameters.Length / 3;

            for (int i = 0; i < instructionCount; i++)
            {
                int baseIndex = i * 3;
                SymbolType condSymbol = SymbolData.IntToSymbolType(parameters[baseIndex]);
                Tape tape = tapes[i];

                if (condSymbol != SymbolType.None && tape.Read() != condSymbol)
                {
                    callback?.Invoke(false);
                    yield break;
                }
            }

            int pendingMoves = instructionCount;
            for (int i = 0; i < instructionCount; i++)
            {
                int baseIndex = i * 3;
                SymbolType actionSymbol = SymbolData.IntToSymbolType(parameters[baseIndex + 1]);
                int moveDirection = parameters[baseIndex + 2];

                Tape tape = tapes[i];

                tape.Write(actionSymbol);

                StartCoroutine(tape.MoveAndNotify(moveDirection, () => pendingMoves--));
            }

            yield return new WaitUntil(() => pendingMoves == 0);
            callback?.Invoke(true);
        }
    }
}
