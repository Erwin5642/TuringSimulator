using System;
using System.Collections.Generic;
using TuringSimulator.Core.ProgramGraph;
using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Holds serialized references to all XR blocks/cards and mirrors <see cref="IProgramEditController"/> lock state.
    /// </summary>
    public sealed class ProgramWorkbench : MonoBehaviour, IProgramEditingUi
    {
        public static ProgramWorkbench Instance { get; private set; }

        [Header("Program Start")]
        [Tooltip("Workbench start/power output port. Its connected peer defines the program entry block.")]
        [SerializeField] WireSocketBehaviour startOutputPort;
        [Tooltip("Legacy fallback when no start output port is wired. Must match ProgramBlockBehaviour.blockId.")]
        [SerializeField] string entryBlockId;

        [SerializeField] ProgramBlockBehaviour[] blocks;

        [SerializeField] SymbolCardBehaviour[] symbolCards;

        [SerializeField] DirectionCardBehaviour[] directionCards;

        readonly List<GameObject> _spawnedCardRoots = new();
        readonly List<GameObject> _spawnedBlockRoots = new();

        IProgramEditController _edit;

        float _debounceUntil = -1f;

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

        /// <summary>Debounced graph rebuild after socket/wire changes.</summary>
        public void MarkTopologyDirty()
        {
            _debounceUntil = Time.unscaledTime + DebounceSeconds;
        }

        public void Initialize(IProgramEditController editController)
        {
            _edit = editController ?? throw new ArgumentNullException(nameof(editController));
            _edit.EditingAvailabilityChanged += OnEditingAvailabilityChanged;
            SetEditingEnabled(_edit.CanEdit);
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

            MarkTopologyDirty();
        }

        void UntrackSpawnedCard(GameObject root)
        {
            _spawnedCardRoots.Remove(root);
        }

        void UntrackSpawnedBlock(GameObject root)
        {
            _spawnedBlockRoots.Remove(root);
            MarkTopologyDirty();
        }

        public void RebuildProgramFromScene()
        {
            if (_edit == null || !_edit.CanEdit)
                return;

            var resolvedEntryBlockId = ResolveEntryBlockId();
            if (string.IsNullOrWhiteSpace(resolvedEntryBlockId))
            {
                Debug.LogWarning(
                    "[ProgramWorkbench] Missing start wiring. Connect the workbench start output port to a block input.");
                return;
            }

            var compileBlocks = CollectReachableBlocksFromEntry(resolvedEntryBlockId);
            if (compileBlocks.Count == 0)
            {
                Debug.LogWarning(
                    "[ProgramWorkbench] No reachable blocks found from the current start connection.");
                return;
            }

            var nodes = new List<ProgramGraphNodeData>();
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
            if (!GraphToProgramCompiler.TryCompile(snap, out var builder, out var err))
            {
                Debug.LogWarning($"[ProgramWorkbench] Compile failed: {err}");
                return;
            }

            _edit.ReplaceProgramBuilder(builder);
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

        string ResolveEntryBlockId()
        {
            if (startOutputPort != null &&
                startOutputPort.ConnectedPeer != null &&
                startOutputPort.ConnectedPeer.Owner != null)
            {
                if (startOutputPort.ConnectedPeer.PortIndex != -1)
                {
                    Debug.LogWarning(
                        "[ProgramWorkbench] Start output port should connect to a block input port.");
                }
                return startOutputPort.ConnectedPeer.Owner.BlockId;
            }

            return entryBlockId ?? string.Empty;
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
