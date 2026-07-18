using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TuringSimulator.View
{
    public class LevelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshPro levelTitle;
        [SerializeField] private TextMeshPro levelDescription;
        [SerializeField] private TextMeshPro validationSummary;

        public void SetLevelTitle(string title)
        {
            levelTitle.text = title;
        }

        public void SetLevelDescription(string description)
        {
             levelDescription.text = description;
        }

        public void SetValidationSummary(
            IReadOnlyList<TuringSimulator.Core.Validation.ValidationResult> results)
        {
            if (validationSummary == null || results == null)
                return;

            var passed = results.Count(result => result.Passed);
            var lines = results.Select(result =>
                $"{(result.Passed ? "PASS" : "FAIL")} {result.ScenarioId}");
            validationSummary.text =
                $"Validation: {passed}/{results.Count}\n{string.Join("\n", lines)}";
        }
    }
}
