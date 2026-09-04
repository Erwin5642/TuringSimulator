using System.Collections.Generic;

namespace TuringSimulator.Core.ProgramGraph
{
    /// <summary>Resolves a TM state pair to the authored blocks whose connecting wire should light up.</summary>
    public static class ExecutionWireHighlight
    {
        public static bool TryGetTransitionBlocks(
            IReadOnlyDictionary<int, string> blockIdByState,
            int previousState,
            int nextState,
            out string fromBlockId,
            out string toBlockId)
        {
            fromBlockId = null;
            toBlockId = null;
            if (blockIdByState == null)
                return false;
            if (!blockIdByState.TryGetValue(previousState, out fromBlockId))
                return false;
            if (!blockIdByState.TryGetValue(nextState, out toBlockId))
                return false;
            return !string.IsNullOrEmpty(fromBlockId) && !string.IsNullOrEmpty(toBlockId);
        }
    }
}
