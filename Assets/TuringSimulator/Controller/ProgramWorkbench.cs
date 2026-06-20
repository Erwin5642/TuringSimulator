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

        [Tooltip("Block id with zero incoming edges (must match ProgramBlockBehaviour.blockId).")]
        [SerializeField] string entryBlockId;

        [SerializeField] ProgramBlockBehaviour[] blocks;

        [SerializeField] SymbolCardBehaviour[] symbolCards;

        [SerializeField] DirectionCardBehaviour[] directionCards;

        readonly List<GameObject> _spawnedCardRoots = new();

        IProgramEditController _edit;

        float _debounceUntil = -1f;

        const float DebounceSeconds = 0.12f;

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

        void UntrackSpawnedCard(GameObject root)
        {
            _spawnedCardRoots.Remove(root);
        }

        public void RebuildProgramFromScene()
        {
            if (_edit == null || !_edit.CanEdit)
                return;

            if (blocks == null || blocks.Length == 0 || string.IsNullOrEmpty(entryBlockId))
            {
                Debug.LogWarning("[ProgramWorkbench] Missing blocks or entryBlockId — skipping compile.");
                return;
            }

            var nodes = new List<ProgramGraphNodeData>();
            foreach (var b in blocks)
            {
                if (b != null)
                    nodes.Add(b.BuildNodeData());
            }

            var edges = new List<ProgramGraphEdgeData>();
            foreach (var b in blocks)
            {
                if (b == null)
                    continue;

                foreach (var o in b.EnumerateOutputSockets())
                {
                    if (o == null || o.ConnectedPeer == null)
                        continue;

                    var peer = o.ConnectedPeer;
                    if (peer.Owner == null)
                        continue;

                    edges.Add(new ProgramGraphEdgeData(b.BlockId, o.PortIndex, peer.Owner.BlockId));
                }
            }

            var snap = new ProgramGraphSnapshot(nodes, edges, entryBlockId);
            if (!GraphToProgramCompiler.TryCompile(snap, out var builder, out var err))
            {
                Debug.LogWarning($"[ProgramWorkbench] Compile failed: {err}");
                return;
            }

            _edit.ReplaceProgramBuilder(builder);
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
    }
}
