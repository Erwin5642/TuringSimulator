using System;
using System.Collections.Generic;
using System.Text;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>
    /// Deterministic fingerprint of a directed start-rooted program graph
    /// (nodes, cards, edges). Used to skip recompile when nothing changed.
    /// </summary>
    public static class ProgramGraphFingerprint
    {
        public const string HaltFingerprint = "halt";

        public static string Compute(ProgramGraphSnapshot snap)
        {
            if (snap == null)
                throw new ArgumentNullException(nameof(snap));

            var sb = new StringBuilder(256);
            sb.Append("entry=").Append(snap.EntryBlockId).Append(';');

            var nodes = new List<ProgramGraphNodeData>(snap.Nodes);
            nodes.Sort((a, b) => string.CompareOrdinal(a.BlockId, b.BlockId));
            for (var i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                sb.Append(n.BlockId).Append('|')
                    .Append((int)n.Kind).Append('|')
                    .Append(n.SymbolCard.HasValue ? ((int)n.SymbolCard.Value).ToString() : "-")
                    .Append('|')
                    .Append(n.DirectionCard.HasValue ? ((int)n.DirectionCard.Value).ToString() : "-")
                    .Append(';');
            }

            var edges = new List<ProgramGraphEdgeData>(snap.Edges);
            edges.Sort(CompareEdges);
            for (var i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                sb.Append(e.FromBlockId).Append('>')
                    .Append(e.OutputPortIndex).Append('>')
                    .Append(e.ToBlockId).Append(';');
            }

            return sb.ToString();
        }

        static int CompareEdges(ProgramGraphEdgeData a, ProgramGraphEdgeData b)
        {
            var c = string.CompareOrdinal(a.FromBlockId, b.FromBlockId);
            if (c != 0)
                return c;
            c = a.OutputPortIndex.CompareTo(b.OutputPortIndex);
            if (c != 0)
                return c;
            return string.CompareOrdinal(a.ToBlockId, b.ToBlockId);
        }
    }
}
