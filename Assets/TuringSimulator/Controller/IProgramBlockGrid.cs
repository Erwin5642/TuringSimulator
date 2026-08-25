namespace TuringSimulator.Controller
{
    /// <summary>Square workbench grid that hosts XR sockets for program blocks.</summary>
    public interface IProgramBlockGrid
    {
        /// <summary>World-space side length of the full square grid.</summary>
        float Size { get; }

        /// <summary>Number of cells along each side (total cells = value²).</summary>
        int CellsPerSide { get; }

        /// <summary>Destroy and recreate all grid socket cells from the current settings.</summary>
        void Rebuild();
    }
}
