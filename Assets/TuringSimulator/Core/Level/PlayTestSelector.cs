using System;
using System.Collections.Generic;
using TuringSimulator.Core.Validation;

namespace TuringSimulator.Core.Level
{
    public sealed class PlayTestSelector : IPlayTestSelector
    {
        readonly Random _random;

        public PlayTestSelector()
            : this(new Random())
        {
        }

        public PlayTestSelector(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public ValidationTest Select(IReadOnlyList<ValidationTest> tests, ValidationTest previous)
        {
            if (tests == null)
                throw new ArgumentNullException(nameof(tests));

            var nonNullCount = 0;
            ValidationTest only = null;
            var previousInPool = false;
            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                if (test == null)
                    continue;

                nonNullCount++;
                only = test;
                if (ReferenceEquals(test, previous))
                    previousInPool = true;
            }

            if (nonNullCount == 0)
                throw new ArgumentException("Play test pool must contain at least one test.", nameof(tests));

            if (nonNullCount == 1)
                return only;

            var excludePrevious = previousInPool;
            var eligible = excludePrevious ? nonNullCount - 1 : nonNullCount;
            var pick = _random.Next(eligible);
            var seen = 0;
            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                if (test == null)
                    continue;
                if (excludePrevious && ReferenceEquals(test, previous))
                    continue;
                if (seen == pick)
                    return test;
                seen++;
            }

            throw new InvalidOperationException("Failed to select a play test.");
        }
    }
}
