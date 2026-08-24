using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
        IXRSelectInteractor _dragInteractor;
        WireSocketBehaviour _peerBeforeDrag;
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

                NotifyWorkbenchWireChanged(this, previous, connectedPeer);
                RefreshVisualImmediate();
                previous?.RefreshVisualImmediate();
                connectedPeer?.RefreshVisualImmediate();
            }
        }

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            ConfigureGrabForWireTip();
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
            ClearDragState();
            if (wireRenderer != null)
                wireRenderer.enabled = false;
        }

        void LateUpdate()
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
            // Disabling grab while dragging ends the select; OnSelectExited restores prior wiring.
            RefreshInteractionState();
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

        void ConfigureGrabForWireTip()
        {
            if (_grab == null)
                return;

            // Far select uses a near-style attach tip (at the hand), not a tip left at the ray hit.
            _grab.farAttachMode = InteractableFarAttachMode.Near;
            // Keep the port fixed on the block; only the LineRenderer tip follows the attach point.
            _grab.trackPosition = false;
            _grab.trackRotation = false;
            _grab.trackScale = false;
            _grab.throwOnDetach = false;
            _grab.addDefaultGrabTransformers = false;
        }

        static void NotifyWorkbenchTopologyDirty()
        {
            if (ProgramWorkbench.Instance != null)
                ProgramWorkbench.Instance.MarkTopologyDirty();
        }

        static void NotifyWorkbenchWireChanged(
            WireSocketBehaviour self,
            WireSocketBehaviour previous,
            WireSocketBehaviour next)
        {
            var workbench = ProgramWorkbench.Instance;
            if (workbench == null)
                return;

            var selfId = ProgramWorkbench.ResolveConnectivityNodeId(self);
            if (previous != null)
            {
                var prevId = ProgramWorkbench.ResolveConnectivityNodeId(previous);
                if (!string.IsNullOrEmpty(selfId) && !string.IsNullOrEmpty(prevId))
                    workbench.MarkWireChanged(selfId, prevId, connected: false);
                else
                    workbench.MarkTopologyDirty();
            }

            if (next != null)
            {
                var nextId = ProgramWorkbench.ResolveConnectivityNodeId(next);
                if (!string.IsNullOrEmpty(selfId) && !string.IsNullOrEmpty(nextId))
                    workbench.MarkWireChanged(selfId, nextId, connected: true);
                else
                    workbench.MarkTopologyDirty();
            }

            if (previous == null && next == null)
                workbench.MarkTopologyDirty();
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
            _peerBeforeDrag = connectedPeer;
            if (connectedPeer != null)
                ConnectedPeer = null;

            _isDragging = true;
            _dragInteractor = args.interactorObject;
            SnapAttachTipToHand();
            RefreshVisualImmediate();
        }

        void OnSelectExited(SelectExitEventArgs _)
        {
            if (!_isDragging)
                return;

            if (_interactionEnabled)
            {
                var target = FindClosestCompatibleTarget();
                if (target != null)
                    ConnectedPeer = target;
            }
            else
            {
                // Program edit locked mid-drag: restore the wire that was detached on grab.
                ConnectedPeer = _peerBeforeDrag;
            }

            _peerBeforeDrag = null;
            ClearDragState();
            RefreshVisualImmediate();
        }

        void ClearDragState()
        {
            _isDragging = false;
            _dragInteractor = null;
        }

        /// <summary>
        /// Clears far-select attach offset so the tip sits at the interactor origin (hand),
        /// matching the approach in VRTemplate <c>RayAttachModifier</c>.
        /// </summary>
        void SnapAttachTipToHand()
        {
            if (_dragInteractor == null || _grab == null)
                return;

            var attachTransform = _dragInteractor.GetAttachTransform(_grab);
            if (attachTransform == null)
                return;

            var localPose = _dragInteractor.GetLocalAttachPoseOnSelect(_grab);
            attachTransform.localPosition = localPose.position;
            attachTransform.localRotation = localPose.rotation;
        }

        /// <summary>
        /// Live wire tip: XRI attach transform (updated by NearFar InteractionAttachController).
        /// </summary>
        Vector3 ResolveDragEndpoint()
        {
            if (_dragInteractor == null)
                return transform.position;

            if (_grab != null)
            {
                var attachTransform = _dragInteractor.GetAttachTransform(_grab);
                if (attachTransform != null)
                    return attachTransform.position;
            }

            if (_dragInteractor is Component interactorComponent)
            {
                var nearFar = interactorComponent as NearFarInteractor
                              ?? interactorComponent.GetComponent<NearFarInteractor>();
                if (nearFar != null &&
                    nearFar.interactionAttachController is Component attachController)
                {
                    return attachController.transform.position;
                }

                return interactorComponent.transform.position;
            }

            return transform.position;
        }

        WireSocketBehaviour FindClosestCompatibleTarget()
        {
            var center = _isDragging ? ResolveDragEndpoint() : transform.position;
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
            if (_isDragging)
            {
                RenderWire(start, ResolveDragEndpoint(), previewColor);
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
