using TuringSimulator.Core.Types;
using UnityEngine;


namespace TuringSimulator.Controller
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public sealed class DirectionCardBehaviour : MonoBehaviour
    {
        [SerializeField]
        public MoveDirection Direction = MoveDirection.Right;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        public void Configure(MoveDirection value)
        {
            Direction = value;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _grab.enabled = enabled;
            foreach (var c in GetComponents<Collider>())
                c.enabled = enabled;
        }
    }
}
