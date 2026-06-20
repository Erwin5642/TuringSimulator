namespace TuringSimulator.Controller
{
    /// <summary>Scene registry that owns XR references and mirrors edit/run lock.</summary>
    public interface IProgramEditingUi
    {
        void Initialize(IProgramEditController editController);

        /// <summary>Enable or disable XR grabs/sockets (false while running).</summary>
        void SetEditingEnabled(bool allowEditing);

        /// <summary>Rebuild transition table from serialized wires/slots.</summary>
        void RebuildProgramFromScene();
    }
}
