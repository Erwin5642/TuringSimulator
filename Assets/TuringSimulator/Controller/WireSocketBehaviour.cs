using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TuringSimulator.Controller
{
    /// <summary>Logical wire endpoint: connect an output socket to another block's input socket.</summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public sealed class WireSocketBehaviour : MonoBehaviour
    {
        enum UnboundPortDirection
        {
            Output = 0,
            Input = 1
        }

        [SerializeField] WireSocketBehaviour connectedPeer;
        [Header("Direction")]
        [Tooltip("Used only when this socket is not initialized by a ProgramBlockBehaviour (e.g., workbench start port).")]
        [SerializeField] UnboundPortDirection unboundDirection = UnboundPortDirection.Output;

        [Header("Linking")]
        [SerializeField, Min(0.01f)] float connectRadius = 0.12f;

        [Header("Wire Visual")]
        [Tooltip("Optional. If empty, a LineRenderer is auto-added.")]
        [SerializeField] LineRenderer wireRenderer;
        [SerializeField] Color connectedColor = new Color(0.2f, 0.9f, 1f, 1f);
        [SerializeField] Color previewColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField, Min(0.0005f)] float lineWidth = 0.01f;
        [SerializeField, Range(2, 32)] int curveResolution = 16;
        [SerializeField, Min(0f)] float bendDistance = 0.12f;
        [SerializeField] LayerMask curveCollisionMask = ~0;

        ProgramBlockBehaviour _owner;
        int _portIndex;
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
        bool _isDragging;
        Transform _dragTarget;
        bool _interactionEnabled = true;
        readonly Collider[] _targetBuffer = new Collider[16];

        public ProgramBlockBehaviour Owner => _owner;

        /// <summary>0 = single/default output; 1 = condition true; 2 = condition false.</summary>
        public int PortIndex => _portIndex;

        public bool IsInputPort =>
            _owner != null ? _portIndex == -1 : unboundDirection == UnboundPortDirection.Input;

        public bool IsOutputPort => !IsInputPort;

        public WireSocketBehaviour ConnectedPeer
        {
            get => connectedPeer;
            set
            {
                if (connectedPeer == value)
                    return;

                if (value != null && !AreCompatible(this, value))
                {
                    Debug.LogWarning("[WireSocket] Only output->input links are allowed.");
                    return;
                }

                var previous = connectedPeer;
                connectedPeer = value;
                if (previous != null && previous.connectedPeer == this)
                    previous.connectedPeer = null;

                if (connectedPeer != null && connectedPeer.connectedPeer != this)
                    connectedPeer.connectedPeer = this;

                NotifyWorkbenchTopologyDirty();
                RefreshVisualImmediate();
                previous?.RefreshVisualImmediate();
                connectedPeer?.RefreshVisualImmediate();
            }
        }

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            EnsureCollider();
            EnsureWireRenderer();
        }

        void OnEnable()
        {
            if (_grab != null)
            {
                _grab.selectEntered.AddListener(OnSelectEntered);
                _grab.selectExited.AddListener(OnSelectExited);
            }
            RefreshInteractionState();
            RefreshVisualImmediate();
        }

        void OnDisable()
        {
            if (_grab != null)
            {
                _grab.selectEntered.RemoveListener(OnSelectEntered);
                _grab.selectExited.RemoveListener(OnSelectExited);
            }
            _isDragging = false;
            _dragTarget = null;
            if (wireRenderer != null)
                wireRenderer.enabled = false;
        }

        void Update()
        {
            if (!IsOutputPort)
                return;
            if (!_isDragging && connectedPeer == null && (wireRenderer == null || !wireRenderer.enabled))
                return;
            RefreshVisualImmediate();
        }

        public void Initialize(ProgramBlockBehaviour owner, int portIndex)
        {
            _owner = owner;
            _portIndex = portIndex;
            RefreshInteractionState();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            RefreshInteractionState();
            var colliders = GetComponents<Collider>();
            for (var i = 0; i < colliders.Length; i++)
                colliders[i].enabled = enabled;
        }

        void OnValidate()
        {
            NotifyWorkbenchTopologyDirty();
            if (curveResolution < 2)
                curveResolution = 2;
            if (connectRadius < 0.01f)
                connectRadius = 0.01f;
            if (lineWidth < 0.0005f)
                lineWidth = 0.0005f;
            if (wireRenderer != null)
            {
                wireRenderer.startWidth = lineWidth;
                wireRenderer.endWidth = lineWidth;
            }
        }

        static void NotifyWorkbenchTopologyDirty()
        {
            if (ProgramWorkbench.Instance != null)
                ProgramWorkbench.Instance.MarkTopologyDirty();
        }

        static bool AreCompatible(WireSocketBehaviour a, WireSocketBehaviour b)
        {
            if (a == null || b == null || a == b)
                return false;

            return a.IsOutputPort != b.IsOutputPort;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (!_interactionEnabled || !IsOutputPort)
                return;

            // Dragging a connected wire detaches it first, so the player can re-route it.
            if (connectedPeer != null)
                ConnectedPeer = null;

            _isDragging = true;
            _dragTarget = (args.interactorObject as Component)?.transform;
        }

        void OnSelectExited(SelectExitEventArgs _)
        {
            if (!_isDragging)
                return;

            var target = FindClosestCompatibleTarget();
            if (target != null)
                ConnectedPeer = target;

            _isDragging = false;
            _dragTarget = null;
            RefreshVisualImmediate();
        }

        WireSocketBehaviour FindClosestCompatibleTarget()
        {
            var center = _dragTarget != null ? _dragTarget.position : transform.position;
            var count = Physics.OverlapSphereNonAlloc(
                center,
                connectRadius,
                _targetBuffer,
                ~0,
                QueryTriggerInteraction.Collide);

            WireSocketBehaviour best = null;
            var bestSqrDist = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var candidateCollider = _targetBuffer[i];
                if (candidateCollider == null)
                    continue;

                var candidate = candidateCollider.GetComponentInParent<WireSocketBehaviour>();
                if (!AreCompatible(this, candidate))
                    continue;
                if (candidate == connectedPeer)
                    continue;

                var sqrDist = (candidate.transform.position - center).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = candidate;
                }
            }

            return best;
        }

        void RefreshInteractionState()
        {
            if (_grab == null)
                return;

            // Inputs are passive targets; only outputs are draggable wire sources.
            _grab.enabled = _interactionEnabled && IsOutputPort;
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
                return;

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.02f;
            sphere.isTrigger = true;
        }

        void EnsureWireRenderer()
        {
            if (wireRenderer == null)
                wireRenderer = GetComponent<LineRenderer>();
            if (wireRenderer == null)
                wireRenderer = gameObject.AddComponent<LineRenderer>();

            wireRenderer.useWorldSpace = true;
            wireRenderer.loop = false;
            wireRenderer.positionCount = 0;
            wireRenderer.startWidth = lineWidth;
            wireRenderer.endWidth = lineWidth;
            wireRenderer.enabled = false;
        }

        void RefreshVisualImmediate()
        {
            if (wireRenderer == null || !IsOutputPort)
            {
                if (wireRenderer != null)
                    wireRenderer.enabled = false;
                return;
            }

            var start = transform.position;
            if (_isDragging && _dragTarget != null)
            {
                RenderWire(start, _dragTarget.position, previewColor);
                return;
            }

            if (connectedPeer == null)
            {
                wireRenderer.enabled = false;
                wireRenderer.positionCount = 0;
                return;
            }

            RenderWire(start, connectedPeer.transform.position, connectedColor);
        }

        void RenderWire(Vector3 start, Vector3 end, Color color)
        {
            wireRenderer.enabled = true;
            wireRenderer.startColor = color;
            wireRenderer.endColor = color;

            var dir = end - start;
            var len = dir.magnitude;
            if (len < 0.0001f)
            {
                wireRenderer.positionCount = 2;
                wireRenderer.SetPosition(0, start);
                wireRenderer.SetPosition(1, end);
                return;
            }

            var offset = Mathf.Min(0.02f, len * 0.25f);
            var from = start + dir.normalized * offset;
            var to = end - dir.normalized * offset;
            if (Physics.Linecast(from, to, out var hit, curveCollisionMask, QueryTriggerInteraction.Ignore))
            {
                var control = hit.point + hit.normal * bendDistance + Vector3.up * bendDistance;
                wireRenderer.positionCount = curveResolution;
                for (var i = 0; i < curveResolution; i++)
                {
                    var t = i / (float)(curveResolution - 1);
                    wireRenderer.SetPosition(i, EvaluateQuadraticBezier(start, control, end, t));
                }

                return;
            }

            wireRenderer.positionCount = 2;
            wireRenderer.SetPosition(0, start);
            wireRenderer.SetPosition(1, end);
        }

        static Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }
    }
}
