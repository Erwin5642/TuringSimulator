using System.Collections.Generic;
using System.Linq;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.Tape
{
    public sealed class TapeSnapshot
    {
        public int HeadIndex { get; }
        public IReadOnlyDictionary<int, Symbol> Cells { get; }

        public TapeSnapshot(Dictionary<int, Symbol> source, int headIndex)
        {
            HeadIndex = headIndex;
            Cells = new Dictionary<int, Symbol>(source);
        }

        public Symbol Read(int index)
            => Cells.GetValueOrDefault(index, Symbol.Blank);
        
        public bool StructuralEquals(TapeSnapshot other)
        {
            var a = ExtractPattern(this);
            var b = ExtractPattern(other);

            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Symbol.Equals(b[i].Symbol))
                    return false;

                if (a[i].Offset != b[i].Offset)
                    return false;
            }

            return true;
        }

        private static List<(Symbol Symbol, int Offset)> ExtractPattern(TapeSnapshot tape)
        {
            var cells = tape.Cells
                .Where(c => c.Value != Symbol.Blank)
                .OrderBy(c => c.Key)
                .ToList();

            var result = new List<(Symbol, int)>();

            if (cells.Count == 0)
                return result;

            int baseIndex = cells[0].Key;

            foreach (var (index, symbol) in cells)
            {
                result.Add((symbol, index - baseIndex));
            }

            return result;
        }
    }
}