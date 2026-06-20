using TuringSimulator.Core.Types;
using UnityEngine;


namespace TuringSimulator.Controller
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public sealed class SymbolCardBehaviour : MonoBehaviour
    {
        [SerializeField]
        public Symbol Symbol = Symbol.Gear;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        public void Configure(Symbol value)
        {
            Symbol = value;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _grab.enabled = enabled;
            foreach (var c in GetComponents<Collider>())
                c.enabled = enabled;
        }
    }
}
