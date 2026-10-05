using NUnit.Framework;
using TuringSimulator.Core.Validation;
using TuringSimulator.View;

namespace EditModeTests
{
    public class ValidationSummaryTextTests
    {
        [Test]
        public void Format_NullOrEmpty_IsPending()
        {
            Assert.That(ValidationSummaryText.Format(null), Is.EqualTo(ValidationSummaryText.Pending));
            Assert.That(
                ValidationSummaryText.Format(System.Array.Empty<ValidationResult>()),
                Is.EqualTo(ValidationSummaryText.Pending));
        }

        [Test]
        public void Format_AllPassed_DoesNotNameScenario()
        {
            var text = ValidationSummaryText.Format(new[]
            {
                new ValidationResult { ScenarioId = "hidden_gear", Passed = true },
                new ValidationResult { ScenarioId = "hidden_nut", Passed = true },
            });

            Assert.That(text, Does.StartWith(ValidationSummaryText.AcceptedPrefix));
            Assert.That(text, Does.Contain("Lotes: 2/2"));
            Assert.That(text, Does.Not.Contain("hidden_gear"));
            Assert.That(text, Does.Not.Contain("FAIL"));
        }

        [Test]
        public void Format_PartialFailure_DoesNotNameScenario()
        {
            var text = ValidationSummaryText.Format(new[]
            {
                new ValidationResult { ScenarioId = "hidden_gear", Passed = true },
                new ValidationResult { ScenarioId = "hidden_nut", Passed = false },
            });

            Assert.That(text, Does.StartWith(ValidationSummaryText.RefusedPrefix));
            Assert.That(text, Does.Contain("Lotes: 1/2"));
            Assert.That(text, Does.Not.Contain("hidden_nut"));
            Assert.That(text, Does.Not.Contain("PASS"));
            Assert.That(text, Does.Not.Contain("FAIL"));
        }
    }
}
