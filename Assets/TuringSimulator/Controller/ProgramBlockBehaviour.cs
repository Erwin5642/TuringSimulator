using System.Collections.Generic;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;
using UnityEngine;


namespace TuringSimulator.Controller
{
    /// <summary>XR instruction block: wires define edges; slots supply symbol/direction cards.</summary>
    public sealed class ProgramBlockBehaviour : MonoBehaviour, IProgramBlock
    {
        [SerializeField] string blockId;
        [SerializeField] ProgramBlockKind kind;

        [Header("Ports")]
        [SerializeField] WireSocketBehaviour inputPort;
        [SerializeField] WireSocketBehaviour outputPort;
        [SerializeField] WireSocketBehaviour outputTruePort;
        [SerializeField] WireSocketBehaviour outputFalsePort;

        [Header("Slots")]
        [SerializeField] CardSlotBehaviour symbolSlot;
        [SerializeField] CardSlotBehaviour directionSlot;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

        public string BlockId => string.IsNullOrEmpty(blockId) ? name : blockId;

        public ProgramBlockKind Kind => kind;

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (_grab == null)
                _grab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            if (inputPort != null)
                inputPort.Initialize(this, -1);
            if (outputPort != null)
                outputPort.Initialize(this, 0);
            if (outputTruePort != null)
                outputTruePort.Initialize(this, 1);
            if (outputFalsePort != null)
                outputFalsePort.Initialize(this, 2);
        }

        void OnEnable()
        {
            if (symbolSlot != null)
                symbolSlot.OccupancyChanged += NotifyWorkbench;
            if (directionSlot != null)
                directionSlot.OccupancyChanged += NotifyWorkbench;
        }

        void OnDisable()
        {
            if (symbolSlot != null)
                symbolSlot.OccupancyChanged -= NotifyWorkbench;
            if (directionSlot != null)
                directionSlot.OccupancyChanged -= NotifyWorkbench;
        }

        void NotifyWorkbench()
        {
            if (ProgramWorkbench.Instance != null)
                ProgramWorkbench.Instance.MarkTopologyDirty();
        }

        public ProgramGraphNodeData BuildNodeData()
        {
            Symbol? sym = null;
            MoveDirection? dir = null;

            switch (kind)
            {
                case ProgramBlockKind.Write:
                case ProgramBlockKind.Condition:
                    sym = symbolSlot != null ? symbolSlot.GetSymbolValue() : null;
                    break;
                case ProgramBlockKind.Move:
                    dir = directionSlot != null ? directionSlot.GetDirectionValue() : null;
                    break;
            }

            return new ProgramGraphNodeData(BlockId, kind, sym, dir);
        }

        public IEnumerable<WireSocketBehaviour> EnumerateOutputSockets()
        {
            switch (kind)
            {
                case ProgramBlockKind.Write:
                case ProgramBlockKind.Move:
                    if (outputPort != null)
                        yield return outputPort;
                    yield break;
                case ProgramBlockKind.Condition:
                    if (outputTruePort != null)
                        yield return outputTruePort;
                    if (outputFalsePort != null)
                        yield return outputFalsePort;
                    yield break;
                case ProgramBlockKind.Accept:
                case ProgramBlockKind.Reject:
                    yield break;
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (_grab != null)
                _grab.enabled = enabled;
            foreach (var c in GetComponents<Collider>())
                c.enabled = enabled;

            symbolSlot?.SetInteractionEnabled(enabled);
            directionSlot?.SetInteractionEnabled(enabled);
        }
    }
}
