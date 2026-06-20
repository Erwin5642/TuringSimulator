using System;
using System.Collections.Generic;
using TuringSimulator.Core.Types;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>Immutable description of the authored block graph (no Unity types).</summary>
    public readonly struct ProgramGraphNodeData
    {
        public readonly string BlockId;
        public readonly ProgramBlockKind Kind;
        /// <summary>Symbol card slotted for Write / Condition; null if missing.</summary>
        public readonly Symbol? SymbolCard;
        /// <summary>Direction card slotted for Move; null if missing.</summary>
        public readonly MoveDirection? DirectionCard;

        public ProgramGraphNodeData(
            string blockId,
            ProgramBlockKind kind,
            Symbol? symbolCard = null,
            MoveDirection? directionCard = null)
        {
            BlockId = blockId ?? throw new ArgumentNullException(nameof(blockId));
            Kind = kind;
            SymbolCard = symbolCard;
            DirectionCard = directionCard;
        }
    }

    /// <summary>Directed link from an output port to a target block input.</summary>
    public readonly struct ProgramGraphEdgeData
    {
        /// <summary>Source block id.</summary>
        public readonly string FromBlockId;
        /// <summary>0 = single output, 1 = condition true, 2 = condition false.</summary>
        public readonly int OutputPortIndex;
        public readonly string ToBlockId;

        public ProgramGraphEdgeData(string fromBlockId, int outputPortIndex, string toBlockId)
        {
            FromBlockId = fromBlockId ?? throw new ArgumentNullException(nameof(fromBlockId));
            OutputPortIndex = outputPortIndex;
            ToBlockId = toBlockId ?? throw new ArgumentNullException(nameof(toBlockId));
        }
    }

    /// <summary>Full graph snapshot for compilation.</summary>
    public sealed class ProgramGraphSnapshot
    {
        public ProgramGraphSnapshot(
            IReadOnlyList<ProgramGraphNodeData> nodes,
            IReadOnlyList<ProgramGraphEdgeData> edges,
            string entryBlockId)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Edges = edges ?? throw new ArgumentNullException(nameof(edges));
            EntryBlockId = entryBlockId ?? throw new ArgumentNullException(nameof(entryBlockId));
        }

        public IReadOnlyList<ProgramGraphNodeData> Nodes { get; }
        public IReadOnlyList<ProgramGraphEdgeData> Edges { get; }
        public string EntryBlockId { get; }
    }
}
