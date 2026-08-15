using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace TuringSimulator.Controller
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SymbolCardBehaviour : MonoBehaviour
    {
        [SerializeField]
        public Symbol Symbol = Symbol.Gear;

        XRGrabInteractable _grab;

        public XRGrabInteractable Grab => _grab;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }

        public void Configure(Symbol value)
        {
            Symbol = value;
        }

        /// <summary>
        /// Free cards stay grabbable while program edit is locked.
        /// Occupied-slot lock is handled by <see cref="CardSlotBehaviour"/>.
        /// </summary>
        public void SetInteractionEnabled(bool _)
        {
        }
    }
}
