using System;
using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace TuringSimulator.Controller
{
    /// <summary>XR socket that accepts a symbol or direction card.</summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
    public sealed class CardSlotBehaviour : MonoBehaviour
    {
        public enum SlotKind
        {
            Symbol,
            Direction
        }

        [SerializeField] SlotKind kind;

        UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor _socket;

        SymbolCardBehaviour _symbolCard;
        DirectionCardBehaviour _directionCard;

        public event Action OccupancyChanged;

        void Awake()
        {
            _socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        }

        void OnEnable()
        {
            _socket.selectEntered.AddListener(OnSelectEntered);
            _socket.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            _socket.selectEntered.RemoveListener(OnSelectEntered);
            _socket.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            var t = (args.interactableObject as Component)?.transform;
            if (t == null)
                return;

            _symbolCard = kind == SlotKind.Symbol ? t.GetComponentInChildren<SymbolCardBehaviour>() : null;
            _directionCard = kind == SlotKind.Direction ? t.GetComponentInChildren<DirectionCardBehaviour>() : null;
            OccupancyChanged?.Invoke();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            _symbolCard = null;
            _directionCard = null;
            OccupancyChanged?.Invoke();
        }

        public Symbol? GetSymbolValue()
        {
            if (kind != SlotKind.Symbol)
                return null;
            return _symbolCard != null ? _symbolCard.Symbol : null;
        }

        public MoveDirection? GetDirectionValue()
        {
            if (kind != SlotKind.Direction)
                return null;
            return _directionCard != null ? _directionCard.Direction : null;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _socket.enabled = enabled;
            foreach (var c in GetComponents<Collider>())
                c.enabled = enabled;
        }
    }
}
