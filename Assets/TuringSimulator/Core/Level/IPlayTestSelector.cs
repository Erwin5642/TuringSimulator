using System.Collections.Generic;
using TuringSimulator.Core.Validation;

namespace TuringSimulator.Core.Level
{
    public interface IPlayTestSelector
    {
        ValidationTest Select(IReadOnlyList<ValidationTest> tests, ValidationTest previous);
    }
}
