using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Builds an N×N square grid of trigger box colliders with <see cref="XRSocketInteractor"/>s
    /// for program blocks. Edit Size and Cells Per Side in the Inspector; cells regenerate automatically.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class ProgramBlockGrid : MonoBehaviour, IProgramBlockGrid
    {
        const string CellsRootName = "Cells";
        const string ProgramBlockInteractionLayerName = "Program Block";
        /// <summary>
        /// Physics layer used by program block bodies. Wire ports live on Default and exclude this layer,
        /// so socket triggers here never contact ports (avoids red reject hover meshes).
        /// </summary>
        const string ProgramBlockPhysicsLayerName = "ProgramBlock";

        [Header("Grid")]
        [SerializeField, Min(0.01f)]
        [Tooltip("World-space side length of the full square grid (X and Z).")]
        float size = 1f;

        [SerializeField, Min(1)]
        [Tooltip("Number of cells along each side. Total sockets = this value squared.")]
        int cellsPerSide = 3;

        [Header("Cell Collider")]
        [SerializeField, Min(0.001f)]
        [Tooltip("Box collider height (Y).")]
        float cellHeight = 0.05f;

        [SerializeField, Range(0.05f, 1f)]
        [Tooltip("Fraction of each cell's XZ pitch used by the trigger box (1 = no gap).")]
        float cellFill = 0.4f;

        [Header("XR Socket")]
        [SerializeField]
        InteractionLayerMask interactionLayers;

        [SerializeField]
        XRInteractionManager interactionManager;

        [SerializeField, Min(0f)]
        float recycleDelayTime = 1f;

        Transform _cellsRoot;
        bool _rebuildQueued;
        bool _isRebuilding;

        public float Size => size;
        public int CellsPerSide => cellsPerSide;

        void Reset()
        {
            size = 1f;
            cellsPerSide = 3;
            cellHeight = 0.05f;
            cellFill = 0.4f;
            recycleDelayTime = 1f;
            interactionLayers = InteractionLayerMask.GetMask(ProgramBlockInteractionLayerName);
        }

        void OnEnable()
        {
            if (interactionLayers == 0)
                interactionLayers = InteractionLayerMask.GetMask(ProgramBlockInteractionLayerName);

            // Play Mode always rebuilds. Edit Mode only fills a missing/incomplete grid so we do not
            // DestroyImmediate objects the Inspector may still be drawing.
            if (Application.isPlaying || !HasExpectedCellCount())
                Rebuild();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            _rebuildQueued = false;
#endif
        }

        void OnValidate()
        {
            size = Mathf.Max(0.01f, size);
            cellsPerSide = Mathf.Max(1, cellsPerSide);
            cellHeight = Mathf.Max(0.001f, cellHeight);
            cellFill = Mathf.Clamp(cellFill, 0.05f, 1f);
            recycleDelayTime = Mathf.Max(0f, recycleDelayTime);

#if UNITY_EDITOR
            if (_rebuildQueued || _isRebuilding)
                return;

            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this == null || !isActiveAndEnabled)
                    return;
                Rebuild();
            };
#endif
        }

        [ContextMenu("Rebuild Grid")]
        public void Rebuild()
        {
            if (_isRebuilding)
                return;

            _isRebuilding = true;
            try
            {
                EnsureCellsRoot();
                ClearCells();

                var pitch = size / cellsPerSide;
                var colliderXZ = pitch * cellFill;
                var origin = -size * 0.5f + pitch * 0.5f;

                for (var z = 0; z < cellsPerSide; z++)
                {
                    for (var x = 0; x < cellsPerSide; x++)
                        CreateCell(x, z, origin, pitch, colliderXZ);
                }
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        bool HasExpectedCellCount()
        {
            var root = transform.Find(CellsRootName);
            return root != null && root.childCount == cellsPerSide * cellsPerSide;
        }

        void EnsureCellsRoot()
        {
            if (_cellsRoot == null)
            {
                var existing = transform.Find(CellsRootName);
                if (existing != null)
                    _cellsRoot = existing;
                else
                {
                    var rootGo = new GameObject(CellsRootName);
                    rootGo.transform.SetParent(transform, false);
                    _cellsRoot = rootGo.transform;
                }
            }

            _cellsRoot.gameObject.layer = ResolveProgramBlockPhysicsLayer();
        }

        void ClearCells()
        {
            if (_cellsRoot == null)
                return;

            RepointSelectionAwayFromCells();

            for (var i = _cellsRoot.childCount - 1; i >= 0; i--)
            {
                var child = _cellsRoot.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child);
                else
                    Destroy(child);
#else
                Destroy(child);
#endif
            }
        }

        void CreateCell(int x, int z, float origin, float pitch, float colliderXZ)
        {
            var cellGo = new GameObject(BuildCellName(x, z));
            cellGo.transform.SetParent(_cellsRoot, false);
            cellGo.transform.localPosition = new Vector3(
                origin + x * pitch,
                0f,
                origin + z * pitch);
            cellGo.transform.localRotation = Quaternion.identity;
            cellGo.transform.localScale = Vector3.one;
            cellGo.layer = ResolveProgramBlockPhysicsLayer();

            var box = cellGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(colliderXZ, cellHeight, colliderXZ);
            box.center = Vector3.zero;

            var socket = cellGo.AddComponent<XRSocketInteractor>();
            socket.interactionLayers = interactionLayers;
            socket.recycleDelayTime = recycleDelayTime;
            socket.showInteractableHoverMeshes = true;
            socket.socketActive = true;
            if (interactionManager != null)
                socket.interactionManager = interactionManager;
        }

        void RepointSelectionAwayFromCells()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || _cellsRoot == null)
                return;

            var selected = UnityEditor.Selection.objects;
            if (selected == null || selected.Length == 0)
                return;

            for (var i = 0; i < selected.Length; i++)
            {
                var obj = selected[i];
                if (obj == null)
                    continue;

                GameObject go = null;
                if (obj is GameObject asGo)
                    go = asGo;
                else if (obj is Component component)
                    go = component.gameObject;

                if (go == null)
                    continue;

                var t = go.transform;
                if (t == _cellsRoot || t.IsChildOf(_cellsRoot))
                {
                    UnityEditor.Selection.activeGameObject = gameObject;
                    return;
                }
            }
#endif
        }

        static int ResolveProgramBlockPhysicsLayer()
        {
            var layer = LayerMask.NameToLayer(ProgramBlockPhysicsLayerName);
            if (layer >= 0)
                return layer;

            Debug.LogError(
                $"[ProgramBlockGrid] Physics layer '{ProgramBlockPhysicsLayerName}' is missing. " +
                "Grid sockets will use Default and may detect wire ports.");
            return 0;
        }

        static string BuildCellName(int x, int z)
        {
            if (x < 26 && z < 26)
                return $"{(char)('A' + x)}{(char)('A' + z)}";
            return $"Cell_{x}_{z}";
        }
    }
}
