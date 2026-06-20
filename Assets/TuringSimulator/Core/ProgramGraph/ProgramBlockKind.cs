namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>Visual instruction block kinds for XR flow-graph authoring.</summary>
    public enum ProgramBlockKind
    {
        Write,
        Move,
        Condition,
        Accept,
        Reject
    }
}
