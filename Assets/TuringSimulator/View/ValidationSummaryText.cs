using System.Collections.Generic;
using TuringSimulator.Core.Validation;

namespace TuringSimulator.View
{
    public static class ValidationSummaryText
    {
        public const string Pending = "A fábrica ainda não julgou o circuito.";
        public const string AcceptedPrefix = "A fábrica aceitou o circuito.";
        public const string RefusedPrefix = "A fábrica recusou o circuito.";

        public static string Format(IReadOnlyList<ValidationResult> results)
        {
            if (results == null || results.Count == 0)
                return Pending;

            var passed = 0;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i] != null && results[i].Passed)
                    passed++;
            }

            var prefix = passed == results.Count ? AcceptedPrefix : RefusedPrefix;
            return $"{prefix} Lotes: {passed}/{results.Count}.";
        }
    }
}
