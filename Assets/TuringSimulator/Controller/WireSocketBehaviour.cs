using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>Logical wire endpoint: connect an output socket to another block's input socket.</summary>
    public sealed class WireSocketBehaviour : MonoBehaviour
    {
        [SerializeField] WireSocketBehaviour connectedPeer;

        ProgramBlockBehaviour _owner;
        int _portIndex;

        public ProgramBlockBehaviour Owner => _owner;

        /// <summary>0 = single/default output; 1 = condition true; 2 = condition false.</summary>
        public int PortIndex => _portIndex;

        public WireSocketBehaviour ConnectedPeer
        {
            get => connectedPeer;
            set => connectedPeer = value;
        }

        public void Initialize(ProgramBlockBehaviour owner, int portIndex)
        {
            _owner = owner;
            _portIndex = portIndex;
        }
    }
}
