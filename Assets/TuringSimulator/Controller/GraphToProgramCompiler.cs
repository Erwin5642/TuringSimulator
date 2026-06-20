using System;
using System.Collections.Generic;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Controller
{
    /// <summary>Compiles a directed block graph (with cycles) into a transition table.</summary>
    public static class GraphToProgramCompiler
    {
        public static bool TryCompile(ProgramGraphSnapshot snap, out TableProgramBuilder builder, out string error)
        {
            builder = null;
            var nodeById = new Dictionary<string, ProgramGraphNodeData>(StringComparer.Ordinal);
            foreach (var n in snap.Nodes)
            {
                if (!nodeById.TryAdd(n.BlockId, n))
                {
                    error = $"Duplicate block id '{n.BlockId}'.";
                    return false;
                }
            }

            if (!nodeById.ContainsKey(snap.EntryBlockId))
            {
                error = "Entry block id is not in the node list.";
                return false;
            }

            foreach (var e in snap.Edges)
            {
                if (!nodeById.ContainsKey(e.FromBlockId))
                {
                    error = $"Edge references unknown from-block '{e.FromBlockId}'.";
                    return false;
                }

                if (!nodeById.ContainsKey(e.ToBlockId))
                {
                    error = $"Edge references unknown to-block '{e.ToBlockId}'.";
                    return false;
                }
            }

            foreach (var e in snap.Edges)
            {
                var k = nodeById[e.FromBlockId].Kind;
                if (k == ProgramBlockKind.Accept || k == ProgramBlockKind.Reject)
                {
                    error = $"Terminal block '{e.FromBlockId}' ({k}) cannot have outgoing wires.";
                    return false;
                }
            }

            // BFS from entry — assigns deterministic indices 0..n-1 (supports cycles)
            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var q = new Queue<string>();
            q.Enqueue(snap.EntryBlockId);

            var outs = new Dictionary<string, List<(int port, string to)>>(StringComparer.Ordinal);
            foreach (var id in nodeById.Keys)
                outs[id] = new List<(int, string)>();

            foreach (var e in snap.Edges)
                outs[e.FromBlockId].Add((e.OutputPortIndex, e.ToBlockId));

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                if (!seen.Add(u))
                    continue;

                ordered.Add(u);

                foreach (var (_, v) in outs[u])
                {
                    if (!seen.Contains(v))
                        q.Enqueue(v);
                }
            }

            if (ordered.Count != nodeById.Count)
            {
                error = "Graph has unreachable blocks (disconnected from entry).";
                return false;
            }

            var stateOf = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < ordered.Count; i++)
                stateOf[ordered[i]] = i;

            builder = new TableProgramBuilder(0);

            foreach (var id in ordered)
            {
                var node = nodeById[id];
                var s = stateOf[id];

                switch (node.Kind)
                {
                    case ProgramBlockKind.Write:
                    {
                        if (!node.SymbolCard.HasValue)
                        {
                            error = $"Write block '{id}' needs a symbol card.";
                            return false;
                        }

                        if (!TryGetUniqueSuccessor(snap.Edges, id, 0, out var wNext, out var succErr))
                        {
                            error = succErr;
                            return false;
                        }

                        if (wNext == null)
                        {
                            error = $"Write block '{id}' needs exactly one outgoing wire.";
                            return false;
                        }

                        var γ = node.SymbolCard.Value;
                        var sw = stateOf[wNext];
                        foreach (var σ in TapeAlphabet.All)
                            builder.AddTransition(s, σ, new Transition(sw, γ, MoveDirection.Stay));
                        break;
                    }

                    case ProgramBlockKind.Move:
                    {
                        if (!node.DirectionCard.HasValue)
                        {
                            error = $"Move block '{id}' needs a direction card.";
                            return false;
                        }

                        if (!TryGetUniqueSuccessor(snap.Edges, id, 0, out var mNext, out var succErr))
                        {
                            error = succErr;
                            return false;
                        }

                        if (mNext == null)
                        {
                            error = $"Move block '{id}' needs exactly one outgoing wire.";
                            return false;
                        }

                        var dir = node.DirectionCard.Value;
                        var sm = stateOf[mNext];
                        foreach (var σ in TapeAlphabet.All)
                            builder.AddTransition(s, σ, new Transition(sm, σ, dir));
                        break;
                    }

                    case ProgramBlockKind.Condition:
                    {
                        if (!node.SymbolCard.HasValue)
                        {
                            error = $"Condition block '{id}' needs a symbol card.";
                            return false;
                        }

                        if (!TryGetUniqueSuccessor(snap.Edges, id, 1, out var tNext, out var succErr))
                        {
                            error = succErr;
                            return false;
                        }

                        if (!TryGetUniqueSuccessor(snap.Edges, id, 2, out var fNext, out succErr))
                        {
                            error = succErr;
                            return false;
                        }

                        if (tNext == null || fNext == null)
                        {
                            error = $"Condition block '{id}' needs true (port 1) and false (port 2) wires.";
                            return false;
                        }

                        var cmp = node.SymbolCard.Value;
                        var st = stateOf[tNext];
                        var sf = stateOf[fNext];
                        foreach (var σ in TapeAlphabet.All)
                        {
                            var tgt = σ == cmp ? st : sf;
                            builder.AddTransition(s, σ, new Transition(tgt, σ, MoveDirection.Stay));
                        }

                        break;
                    }

                    case ProgramBlockKind.Accept:
                        builder.AddFinalState(s);
                        break;

                    case ProgramBlockKind.Reject:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            error = null;
            return true;
        }

        /// <summary>Unique successor for one output port, or false if duplicate wires.</summary>
        static bool TryGetUniqueSuccessor(
            IReadOnlyList<ProgramGraphEdgeData> edges,
            string blockId,
            int outputPortIndex,
            out string toBlockId,
            out string duplicateMessage)
        {
            duplicateMessage = null;
            toBlockId = null;
            foreach (var e in edges)
            {
                if (e.FromBlockId != blockId || e.OutputPortIndex != outputPortIndex)
                    continue;
                if (toBlockId != null)
                {
                    duplicateMessage =
                        $"Multiple edges from '{blockId}' port {outputPortIndex}.";
                    return false;
                }

                toBlockId = e.ToBlockId;
            }

            return true;
        }
    }
}
