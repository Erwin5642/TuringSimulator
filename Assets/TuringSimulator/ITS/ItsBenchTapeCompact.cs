using System;
using System.Collections.Generic;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;

namespace ITS
{
    /// <summary>
    /// Builds a finite factory-language tape window for ITS <c>/ask</c>:
    /// one blank past the first non-blank, occupied cells, one blank past the last,
    /// always including the head. An all-blank esteira still spans index 0 through
    /// the head so <c>head_offset</c> shows movement, not a collapsed 0.
    /// </summary>
    public static class ItsBenchTapeCompact
    {
        public const int MaxCells = 64;
        public const string Vazio = "vazio";
        public const string Engrenagem = "engrenagem";
        public const string Porca = "porca";
        public const string Parafuso = "parafuso";
        public const string Marcador = "marcador";

        public static AskTapeDto FromSnapshot(TapeSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            int? minOccupied = null;
            int? maxOccupied = null;
            foreach (var pair in snapshot.Cells)
            {
                if (!IsOccupied(pair.Value))
                    continue;
                if (minOccupied == null || pair.Key < minOccupied.Value)
                    minOccupied = pair.Key;
                if (maxOccupied == null || pair.Key > maxOccupied.Value)
                    maxOccupied = pair.Key;
            }

            int lo;
            int hi;
            var head = snapshot.HeadIndex;
            if (minOccupied == null)
            {
                lo = Math.Min(0, head);
                hi = Math.Max(0, head);
            }
            else
            {
                lo = minOccupied.Value - 1;
                hi = maxOccupied.Value + 1;
                if (head < lo)
                    lo = head - 1;
                if (head > hi)
                    hi = head + 1;
            }

            ClampWindow(ref lo, ref hi, head);

            var cells = new string[hi - lo + 1];
            for (var i = 0; i < cells.Length; i++)
                cells[i] = ToFactoryToken(snapshot.Read(lo + i));

            return new AskTapeDto
            {
                Cells = cells,
                HeadOffset = head - lo
            };
        }

        public static string ToFactoryToken(Symbol symbol)
        {
            return symbol switch
            {
                Symbol.Gear => Engrenagem,
                Symbol.Nut => Porca,
                Symbol.Screw => Parafuso,
                Symbol.Mark => Marcador,
                _ => Vazio
            };
        }

        static bool IsOccupied(Symbol symbol)
            => symbol != Symbol.Blank && symbol != Symbol.None;

        static void ClampWindow(ref int lo, ref int hi, int head)
        {
            if (head < lo)
                lo = head;
            if (head > hi)
                hi = head;

            while (hi - lo + 1 > MaxCells)
            {
                if (head - lo >= hi - head)
                    lo++;
                else
                    hi--;
            }
        }
    }
}
