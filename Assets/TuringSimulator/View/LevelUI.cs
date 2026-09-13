using System.Collections.Generic;
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
            if (validationSummary == null)
                return;

            validationSummary.text = ValidationSummaryText.Format(results);
        }
    }
}
