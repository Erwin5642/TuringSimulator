using System.Collections.Generic;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>
    /// Undirected union-find over programming-block ids plus a virtual start node.
    /// Used to skip recompiles when wires change outside the start-rooted forest.
    /// </summary>
    public interface IProgramBlockConnectivity
    {
        /// <summary>Virtual node id for the workbench start/power port.</summary>
        string StartNodeId { get; }

        void Clear();

        void EnsureNode(string nodeId);

        void Union(string a, string b);

        /// <summary>Rebuild from scratch (correct after edge deletion).</summary>
        void Rebuild(
            IEnumerable<string> nodeIds,
            IEnumerable<(string A, string B)> undirectedEdges);

        bool SameComponent(string a, string b);

        string Find(string nodeId);
    }
}
