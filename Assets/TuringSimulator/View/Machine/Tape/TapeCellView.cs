using TMPro;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public class TapeCellView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro symbolText;

        public void SetSymbol(Symbol symbol)
        {
            symbolText.text = $"{symbol.ToChar()}";
        }
    }
}