using System;
using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TuringSimulator.Controller
{
    /// <summary>XR socket that accepts a symbol or direction card.</summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class CardSlotBehaviour : MonoBehaviour
    {
        public enum SlotKind
        {
            Symbol,
            Direction
        }

        [SerializeField] SlotKind kind;

        XRSocketInteractor _socket;

        SymbolCardBehaviour _symbolCard;
        DirectionCardBehaviour _directionCard;

        XRGrabInteractable _lockedCardGrab;
        XRSelectFilterDelegate _socketOnlySelectFilter;
        XRHoverFilterDelegate _socketOnlyHoverFilter;

        public event Action OccupancyChanged;

        void Awake()
        {
            _socket = GetComponent<XRSocketInteractor>();
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
            ClearCardRemovalLock();
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
            ClearCardRemovalLock();
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

        /// <summary>
        /// When edit is locked: occupied sockets keep their card (hands cannot pull it out);
        /// empty sockets refuse new cards.
        /// </summary>
        public void SetInteractionEnabled(bool allowEditing)
        {
            if (allowEditing)
            {
                ClearCardRemovalLock();
                _socket.enabled = true;
                return;
            }

            if (_socket.hasSelection)
            {
                _socket.enabled = true;
                ApplyCardRemovalLock(ResolveHeldCardGrab());
            }
            else
            {
                ClearCardRemovalLock();
                _socket.enabled = false;
            }
        }

        XRGrabInteractable ResolveHeldCardGrab()
        {
            if (_symbolCard != null && _symbolCard.Grab != null)
                return _symbolCard.Grab;
            if (_directionCard != null && _directionCard.Grab != null)
                return _directionCard.Grab;

            if (_socket.interactablesSelected.Count > 0 &&
                _socket.interactablesSelected[0] is XRGrabInteractable grab)
                return grab;

            return null;
        }

        void ApplyCardRemovalLock(XRGrabInteractable cardGrab)
        {
            if (cardGrab == null)
                return;

            if (_lockedCardGrab != null && _lockedCardGrab != cardGrab)
                ClearCardRemovalLock();

            _lockedCardGrab = cardGrab;

            if (_socketOnlySelectFilter == null)
            {
                _socketOnlySelectFilter = new XRSelectFilterDelegate((interactor, _) =>
                    interactor is XRSocketInteractor);
            }

            if (_socketOnlyHoverFilter == null)
            {
                _socketOnlyHoverFilter = new XRHoverFilterDelegate((interactor, _) =>
                    interactor is XRSocketInteractor);
            }

            // Remove+Add keeps a single instance even if Apply is called twice.
            _lockedCardGrab.selectFilters.Remove(_socketOnlySelectFilter);
            _lockedCardGrab.hoverFilters.Remove(_socketOnlyHoverFilter);
            _lockedCardGrab.selectFilters.Add(_socketOnlySelectFilter);
            _lockedCardGrab.hoverFilters.Add(_socketOnlyHoverFilter);

            _socketOnlySelectFilter.canProcess = true;
            _socketOnlyHoverFilter.canProcess = true;
        }

        void ClearCardRemovalLock()
        {
            if (_socketOnlySelectFilter != null)
                _socketOnlySelectFilter.canProcess = false;
            if (_socketOnlyHoverFilter != null)
                _socketOnlyHoverFilter.canProcess = false;

            if (_lockedCardGrab != null)
            {
                if (_socketOnlySelectFilter != null)
                    _lockedCardGrab.selectFilters.Remove(_socketOnlySelectFilter);
                if (_socketOnlyHoverFilter != null)
                    _lockedCardGrab.hoverFilters.Remove(_socketOnlyHoverFilter);
                _lockedCardGrab = null;
            }
        }
    }
}
