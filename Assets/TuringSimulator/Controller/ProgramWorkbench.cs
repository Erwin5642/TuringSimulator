using System;
using System.Collections.Generic;
using TuringSimulator.Core.ProgramGraph;
using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Holds serialized references to all XR blocks/cards and mirrors <see cref="IProgramEditController"/> lock state.
    /// Recompiles only when the start-rooted program graph fingerprint changes.
    /// </summary>
    public sealed class ProgramWorkbench : MonoBehaviour, IProgramEditingUi, IProgramExecutionHighlight
    {
        public static ProgramWorkbench Instance { get; private set; }

        [Header("Program Start")]
        [Tooltip("Workbench start/power output port. Its connected peer defines the program entry block.")]
        [SerializeField] WireSocketBehaviour startOutputPort;
        [Tooltip("Legacy fallback only when startOutputPort is unassigned. Must match ProgramBlockBehaviour.blockId.")]
        [SerializeField] string entryBlockId;

        [SerializeField] ProgramBlockBehaviour[] blocks;

        [SerializeField] SymbolCardBehaviour[] symbolCards;

        [SerializeField] DirectionCardBehaviour[] directionCards;

        readonly List<GameObject> _spawnedCardRoots = new();
        readonly List<GameObject> _spawnedBlockRoots = new();
        readonly IProgramBlockConnectivity _connectivity = new ProgramBlockConnectivity();
        readonly List<WireSocketBehaviour> _executionActiveSockets = new();

        IProgramEditController _edit;
        IReadOnlyDictionary<int, string> _blockIdByState;

        float _debounceUntil = -1f;
        string _lastFingerprint;
        bool _connectivityInitialized;

        const float DebounceSeconds = 0.12f;

        public bool HasStartOutputPortAssigned => startOutputPort != null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _connectivity.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_edit != null)
                _edit.EditingAvailabilityChanged -= OnEditingAvailabilityChanged;
        }

        void Update()
        {
            if (_debounceUntil < 0f)
                return;

            if (Time.unscaledTime < _debounceUntil)
                return;

            _debounceUntil = -1f;
            RebuildProgramFromScene();
        }

        /// <summary>Debounced full check (cards, spawn/despawn, unknown dirty).</summary>
        public void MarkTopologyDirty()
        {
            _debounceUntil = Time.unscaledTime + DebounceSeconds;
        }

        /// <summary>
        /// Wire connect/disconnect. Updates union-find and only schedules a rebuild when the
        /// start-rooted undirected forest may have changed.
        /// </summary>
        public void MarkWireChanged(string nodeA, string nodeB, bool connected)
        {
            if (string.IsNullOrEmpty(nodeA) || string.IsNullOrEmpty(nodeB))
            {
                MarkTopologyDirty();
                return;
            }

            EnsureConnectivityInitialized();

            var startId = _connectivity.StartNodeId;
            if (connected)
            {
                var touchesStart =
                    _connectivity.SameComponent(nodeA, startId) ||
                    _connectivity.SameComponent(nodeB, startId);
                _connectivity.Union(nodeA, nodeB);
                if (!touchesStart && !_connectivity.SameComponent(nodeA, startId))
                    return;
            }
            else
            {
                var touchesStart =
                    _connectivity.SameComponent(nodeA, startId) ||
                    _connectivity.SameComponent(nodeB, startId);
                RebuildConnectivityFromScene();
                if (!touchesStart)
                    return;
            }

            MarkTopologyDirty();
        }

        public void Initialize(IProgramEditController editController)
        {
            _edit = editController ?? throw new ArgumentNullException(nameof(editController));
            _edit.EditingAvailabilityChanged += OnEditingAvailabilityChanged;
            SetEditingEnabled(_edit.CanEdit);
            RebuildConnectivityFromScene();
            _lastFingerprint = null;
        }

        void OnEditingAvailabilityChanged(bool canEdit)
        {
            SetEditingEnabled(canEdit);
        }

        public void SetEditingEnabled(bool allowEditing)
        {
            startOutputPort?.SetInteractionEnabled(allowEditing);

            if (blocks != null)
            {
                foreach (var b in blocks)
                {
                    if (b != null)
                        b.SetInteractionEnabled(allowEditing);
                }
            }

            if (symbolCards != null)
            {
                foreach (var c in symbolCards)
                {
                    if (c != null)
                        c.SetInteractionEnabled(allowEditing);
                }
            }

            if (directionCards != null)
            {
                foreach (var c in directionCards)
                {
                    if (c != null)
                        c.SetInteractionEnabled(allowEditing);
                }
            }

            foreach (var root in _spawnedCardRoots)
            {
                if (root == null)
                    continue;

                foreach (var s in root.GetComponentsInChildren<SymbolCardBehaviour>(true))
                    s.SetInteractionEnabled(allowEditing);
                foreach (var d in root.GetComponentsInChildren<DirectionCardBehaviour>(true))
                    d.SetInteractionEnabled(allowEditing);
            }

            foreach (var root in _spawnedBlockRoots)
            {
                if (root == null)
                    continue;

                foreach (var b in root.GetComponentsInChildren<ProgramBlockBehaviour>(true))
                    b.SetInteractionEnabled(allowEditing);
            }
        }

        /// <summary>Register a card instantiated at runtime (e.g. from a card drawer) for edit/run lock.</summary>
        public void RegisterSpawnedCard(GameObject root)
        {
            if (root == null)
                return;

            if (!_spawnedCardRoots.Contains(root))
                _spawnedCardRoots.Add(root);

            var reg = root.GetComponent<SpawnedCardRegistry>();
            if (reg == null)
                reg = root.AddComponent<SpawnedCardRegistry>();
            reg.Initialize(this, root);

            if (_edit != null)
            {
                var allow = _edit.CanEdit;
                foreach (var s in root.GetComponentsInChildren<SymbolCardBehaviour>(true))
                    s.SetInteractionEnabled(allow);
                foreach (var d in root.GetComponentsInChildren<DirectionCardBehaviour>(true))
                    d.SetInteractionEnabled(allow);
            }
        }

        /// <summary>Register a block instantiated at runtime (e.g. from a block drawer) for edit/run lock and compilation.</summary>
        public void RegisterSpawnedBlock(GameObject root)
        {
            if (root == null)
                return;

            if (!_spawnedBlockRoots.Contains(root))
                _spawnedBlockRoots.Add(root);

            var reg = root.GetComponent<SpawnedBlockRegistry>();
            if (reg == null)
                reg = root.AddComponent<SpawnedBlockRegistry>();
            reg.Initialize(this, root);

            if (_edit != null)
            {
                var allow = _edit.CanEdit;
                foreach (var b in root.GetComponentsInChildren<ProgramBlockBehaviour>(true))
                    b.SetInteractionEnabled(allow);
            }

            RebuildConnectivityFromScene();
            MarkTopologyDirty();
        }

        void UntrackSpawnedCard(GameObject root)
        {
            _spawnedCardRoots.Remove(root);
        }

        void UntrackSpawnedBlock(GameObject root)
        {
            _spawnedBlockRoots.Remove(root);
            RebuildConnectivityFromScene();
            MarkTopologyDirty();
        }

        public void RebuildProgramFromScene()
        {
            if (_edit == null || !_edit.CanEdit)
                return;

            RebuildConnectivityFromScene();

            var resolvedEntryBlockId = ResolveEntryBlockId();
            if (string.IsNullOrWhiteSpace(resolvedEntryBlockId))
            {
                ApplyHaltIfChanged();
                return;
            }

            var compileBlocks = CollectReachableBlocksFromEntry(resolvedEntryBlockId);
            if (compileBlocks.Count == 0)
            {
                Debug.LogWarning(
                    "[ProgramWorkbench] Start is wired but the entry block is not registered. Applying halt.");
                ApplyHaltIfChanged();
                return;
            }

            var nodes = new List<ProgramGraphNodeData>(compileBlocks.Count);
            foreach (var b in compileBlocks)
            {
                if (b != null)
                    nodes.Add(b.BuildNodeData());
            }

            var compileBlockSet = new HashSet<ProgramBlockBehaviour>(compileBlocks);
            var edges = new List<ProgramGraphEdgeData>();
            foreach (var b in compileBlocks)
            {
                if (b == null)
                    continue;

                foreach (var o in b.EnumerateOutputSockets())
                {
                    if (o == null || o.ConnectedPeer == null)
                        continue;

                    var peer = o.ConnectedPeer;
                    if (peer.Owner == null || !compileBlockSet.Contains(peer.Owner))
                        continue;

                    edges.Add(new ProgramGraphEdgeData(b.BlockId, o.PortIndex, peer.Owner.BlockId));
                }
            }

            var snap = new ProgramGraphSnapshot(nodes, edges, resolvedEntryBlockId);
            var fingerprint = ProgramGraphFingerprint.Compute(snap);
            if (string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
                return;

            if (!GraphToProgramCompiler.TryCompile(snap, out var builder, out var blockIdByState, out var err))
            {
                Debug.LogWarning(
                    $"[ProgramWorkbench] Compile failed (keeping previous program): {err}");
                return;
            }

            _edit.ReplaceProgramBuilder(builder);
            _blockIdByState = blockIdByState;
            _lastFingerprint = fingerprint;
        }

        public void HighlightStartWire()
        {
            if (startOutputPort == null || startOutputPort.ConnectedPeer == null)
            {
                ClearExecutionHighlight();
                return;
            }

            SetExecutionSockets(startOutputPort);
        }

        public void HighlightTransition(int previousState, int nextState)
        {
            if (!ExecutionWireHighlight.TryGetTransitionBlocks(
                    _blockIdByState,
                    previousState,
                    nextState,
                    out var fromBlockId,
                    out var toBlockId))
            {
                ClearExecutionHighlight();
                return;
            }

            var sockets = CollectTransitionSockets(fromBlockId, toBlockId);
            if (sockets.Count == 0)
            {
                ClearExecutionHighlight();
                return;
            }

            SetExecutionSockets(sockets);
        }

        public void ClearExecutionHighlight()
        {
            for (var i = 0; i < _executionActiveSockets.Count; i++)
            {
                if (_executionActiveSockets[i] != null)
                    _executionActiveSockets[i].SetExecutionActive(false);
            }

            _executionActiveSockets.Clear();
        }

        void SetExecutionSockets(params WireSocketBehaviour[] sockets)
        {
            SetExecutionSockets((IReadOnlyList<WireSocketBehaviour>)sockets);
        }

        void SetExecutionSockets(IReadOnlyList<WireSocketBehaviour> sockets)
        {
            ClearExecutionHighlight();
            for (var i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];
                if (socket == null)
                    continue;
                socket.SetExecutionActive(true);
                _executionActiveSockets.Add(socket);
            }
        }

        List<WireSocketBehaviour> CollectTransitionSockets(string fromBlockId, string toBlockId)
        {
            var result = new List<WireSocketBehaviour>();
            var allBlocks = CollectAllBlocks();
            for (var i = 0; i < allBlocks.Count; i++)
            {
                var block = allBlocks[i];
                if (block == null || !string.Equals(block.BlockId, fromBlockId, StringComparison.Ordinal))
                    continue;

                foreach (var socket in block.EnumerateOutputSockets())
                {
                    var peerOwner = socket?.ConnectedPeer?.Owner;
                    if (peerOwner == null)
                        continue;
                    if (!string.Equals(peerOwner.BlockId, toBlockId, StringComparison.Ordinal))
                        continue;
                    result.Add(socket);
                }
            }

            return result;
        }

        void ApplyHaltIfChanged()
        {
            if (string.Equals(_lastFingerprint, ProgramGraphFingerprint.HaltFingerprint, StringComparison.Ordinal))
                return;

            _edit.Clear();
            _lastFingerprint = ProgramGraphFingerprint.HaltFingerprint;
            _blockIdByState = null;
            ClearExecutionHighlight();
            Debug.Log(
                "[ProgramWorkbench] Start port unwired (or entry missing). Program set to halt.");
        }

        void EnsureConnectivityInitialized()
        {
            if (_connectivityInitialized)
                return;
            RebuildConnectivityFromScene();
        }

        void RebuildConnectivityFromScene()
        {
            var allBlocks = CollectAllBlocks();
            var nodeIds = new List<string>(allBlocks.Count + 1) { _connectivity.StartNodeId };
            for (var i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i] != null)
                    nodeIds.Add(allBlocks[i].BlockId);
            }

            var undirected = new List<(string A, string B)>();
            var entryId = ResolveEntryBlockIdFromPortsOnly();
            if (!string.IsNullOrEmpty(entryId))
                undirected.Add((_connectivity.StartNodeId, entryId));

            for (var i = 0; i < allBlocks.Count; i++)
            {
                var block = allBlocks[i];
                if (block == null)
                    continue;

                foreach (var o in block.EnumerateOutputSockets())
                {
                    var peerOwner = o?.ConnectedPeer?.Owner;
                    if (peerOwner == null)
                        continue;
                    undirected.Add((block.BlockId, peerOwner.BlockId));
                }
            }

            _connectivity.Rebuild(nodeIds, undirected);
            _connectivityInitialized = true;
        }

        List<ProgramBlockBehaviour> CollectReachableBlocksFromEntry(string entryBlockId)
        {
            var allBlocks = CollectAllBlocks();
            if (allBlocks.Count == 0)
                return allBlocks;

            ProgramBlockBehaviour entry = null;
            for (var i = 0; i < allBlocks.Count; i++)
            {
                if (string.Equals(allBlocks[i].BlockId, entryBlockId, StringComparison.Ordinal))
                {
                    entry = allBlocks[i];
                    break;
                }
            }

            if (entry == null)
                return new List<ProgramBlockBehaviour>();

            var allSet = new HashSet<ProgramBlockBehaviour>(allBlocks);
            var reachable = new List<ProgramBlockBehaviour>();
            var reachableSet = new HashSet<ProgramBlockBehaviour>();
            var queue = new Queue<ProgramBlockBehaviour>();
            queue.Enqueue(entry);
            reachableSet.Add(entry);

            while (queue.Count > 0)
            {
                var block = queue.Dequeue();
                reachable.Add(block);

                foreach (var socket in block.EnumerateOutputSockets())
                {
                    var next = socket?.ConnectedPeer?.Owner;
                    if (next == null || !allSet.Contains(next))
                        continue;
                    if (!reachableSet.Add(next))
                        continue;

                    queue.Enqueue(next);
                }
            }

            return reachable;
        }

        List<ProgramBlockBehaviour> CollectAllBlocks()
        {
            var result = new List<ProgramBlockBehaviour>();

            if (blocks != null)
            {
                for (var i = 0; i < blocks.Length; i++)
                {
                    var block = blocks[i];
                    if (block != null && !result.Contains(block))
                        result.Add(block);
                }
            }

            for (var i = 0; i < _spawnedBlockRoots.Count; i++)
            {
                var root = _spawnedBlockRoots[i];
                if (root == null)
                    continue;

                var spawnedBlocks = root.GetComponentsInChildren<ProgramBlockBehaviour>(true);
                for (var j = 0; j < spawnedBlocks.Length; j++)
                {
                    var block = spawnedBlocks[j];
                    if (block != null && !result.Contains(block))
                        result.Add(block);
                }
            }

            return result;
        }

        /// <summary>
        /// Entry from start port when assigned; otherwise legacy <see cref="entryBlockId"/>.
        /// Assigned but unwired start port yields empty (halt) — legacy id is ignored.
        /// </summary>
        string ResolveEntryBlockId()
        {
            if (startOutputPort != null)
                return ResolveEntryBlockIdFromPortsOnly();

            return entryBlockId ?? string.Empty;
        }

        string ResolveEntryBlockIdFromPortsOnly()
        {
            if (startOutputPort == null ||
                startOutputPort.ConnectedPeer == null ||
                startOutputPort.ConnectedPeer.Owner == null)
                return string.Empty;

            if (startOutputPort.ConnectedPeer.PortIndex != -1)
            {
                Debug.LogWarning(
                    "[ProgramWorkbench] Start output port should connect to a block input port.");
            }

            return startOutputPort.ConnectedPeer.Owner.BlockId;
        }

        internal static string ResolveConnectivityNodeId(WireSocketBehaviour socket)
        {
            if (socket == null)
                return null;
            if (socket.Owner != null)
                return socket.Owner.BlockId;
            return ProgramBlockConnectivity.StartId;
        }

        sealed class SpawnedCardRegistry : MonoBehaviour
        {
            ProgramWorkbench _owner;
            GameObject _root;

            public void Initialize(ProgramWorkbench owner, GameObject root)
            {
                _owner = owner;
                _root = root;
            }

            void OnDestroy()
            {
                _owner?.UntrackSpawnedCard(_root);
            }
        }

        sealed class SpawnedBlockRegistry : MonoBehaviour
        {
            ProgramWorkbench _owner;
            GameObject _root;

            public void Initialize(ProgramWorkbench owner, GameObject root)
            {
                _owner = owner;
                _root = root;
            }

            void OnDestroy()
            {
                _owner?.UntrackSpawnedBlock(_root);
            }
        }
    }
}
