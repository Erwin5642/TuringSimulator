namespace TuringSimulator.Controller
{
    /// <summary>
    /// Switches the live energy path to <c>previewColor</c> while a run is playing back.
    /// Idle connected wires stay on <c>connectedColor</c>.
    /// </summary>
    public interface IProgramExecutionHighlight
    {
        /// <summary>Preview-color the start/power wire as energy enters the first block.</summary>
        void HighlightStartWire();

        /// <summary>
        /// Preview-color the wire taken from <paramref name="previousState"/> to <paramref name="nextState"/>.
        /// </summary>
        void HighlightTransition(int previousState, int nextState);

        /// <summary>Restore connected (idle) wire colors.</summary>
        void ClearExecutionHighlight();
    }
}
