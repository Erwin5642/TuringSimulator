using System.Collections.Generic;

namespace TuringSimulator.GameFlow
{
    public interface IMvpSceneWiringValidator
    {
        IReadOnlyList<string> ValidateScene();
    }
}
