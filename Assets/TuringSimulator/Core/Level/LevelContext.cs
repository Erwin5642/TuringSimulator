using System;

namespace TuringSimulator.Core.Level
{
    public class LevelContext
    {
        public LevelDefinition Current { get; private set; }

        public event Action<LevelDefinition> OnLevelChanged;

        public void Set(LevelDefinition level)
        {
            Current = level;
            OnLevelChanged?.Invoke(level);
        }
    }
}