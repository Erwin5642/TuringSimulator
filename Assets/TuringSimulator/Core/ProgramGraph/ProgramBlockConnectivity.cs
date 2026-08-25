using System;
using System.Collections.Generic;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>Disjoint-set (union-find) over program-block ids.</summary>
    public sealed class ProgramBlockConnectivity : IProgramBlockConnectivity
    {
        public const string StartId = "__START__";

        readonly Dictionary<string, string> _parent = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> _rank = new(StringComparer.Ordinal);

        public string StartNodeId => StartId;

        public void Clear()
        {
            _parent.Clear();
            _rank.Clear();
            EnsureNode(StartId);
        }

        public void EnsureNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                throw new ArgumentException("Node id is required.", nameof(nodeId));

            if (_parent.ContainsKey(nodeId))
                return;

            _parent[nodeId] = nodeId;
            _rank[nodeId] = 0;
        }

        public void Union(string a, string b)
        {
            EnsureNode(a);
            EnsureNode(b);

            var ra = Find(a);
            var rb = Find(b);
            if (ra == rb)
                return;

            var rankA = _rank[ra];
            var rankB = _rank[rb];
            if (rankA < rankB)
            {
                _parent[ra] = rb;
            }
            else if (rankA > rankB)
            {
                _parent[rb] = ra;
            }
            else
            {
                _parent[rb] = ra;
                _rank[ra] = rankA + 1;
            }
        }

        public void Rebuild(
            IEnumerable<string> nodeIds,
            IEnumerable<(string A, string B)> undirectedEdges)
        {
            if (nodeIds == null)
                throw new ArgumentNullException(nameof(nodeIds));
            if (undirectedEdges == null)
                throw new ArgumentNullException(nameof(undirectedEdges));

            Clear();
            foreach (var id in nodeIds)
            {
                if (!string.IsNullOrEmpty(id))
                    EnsureNode(id);
            }

            foreach (var (a, b) in undirectedEdges)
            {
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                    continue;
                Union(a, b);
            }
        }

        public bool SameComponent(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            if (!_parent.ContainsKey(a) || !_parent.ContainsKey(b))
                return false;
            return Find(a) == Find(b);
        }

        public string Find(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                throw new ArgumentException("Node id is required.", nameof(nodeId));

            EnsureNode(nodeId);

            var parent = _parent[nodeId];
            if (parent != nodeId)
                _parent[nodeId] = Find(parent);
            return _parent[nodeId];
        }
    }
}
