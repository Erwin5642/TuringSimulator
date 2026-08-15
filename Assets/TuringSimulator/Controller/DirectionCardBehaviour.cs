using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace TuringSimulator.Controller
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class DirectionCardBehaviour : MonoBehaviour
    {
        [SerializeField]
        public MoveDirection Direction = MoveDirection.Right;

        XRGrabInteractable _grab;

        public XRGrabInteractable Grab => _grab;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }

        public void Configure(MoveDirection value)
        {
            Direction = value;
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
