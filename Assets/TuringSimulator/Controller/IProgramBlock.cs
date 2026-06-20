using TuringSimulator.Core.ProgramGraph;

namespace TuringSimulator.Controller
{
    /// <summary>XR flow-graph instruction block contract.</summary>
    public interface IProgramBlock
    {
        string BlockId { get; }
        ProgramBlockKind Kind { get; }
        ProgramGraphNodeData BuildNodeData();
    }
}
