using System;
using TuringSimulator.Core.Validation;

namespace TuringSimulator.Core.Level
{
    public static class LevelTestPool
    {
        public static ValidationTest[] Require(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            var source = level.validationTests;
            if (source == null || source.Length == 0)
                throw new InvalidOperationException("LevelDefinition.validationTests must contain at least one test.");

            var count = 0;
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    count++;
            }

            if (count == 0)
                throw new InvalidOperationException("LevelDefinition.validationTests must contain at least one test.");

            if (count == source.Length)
                return source;

            var tests = new ValidationTest[count];
            var write = 0;
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    tests[write++] = source[i];
            }

            return tests;
        }
    }
}
