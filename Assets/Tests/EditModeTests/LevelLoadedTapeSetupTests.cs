using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Types;
using TuringSimulator.Core.Validation;
using TuringSimulator.GameFlow;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace EditModeTests
{
    public class LevelLoadedTapeSetupTests
    {
        readonly List<ScriptableObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void TapeSetup_LeavesTapeEmpty_AndStoresChosenPlayTest()
        {
            var chosen = CreateTest("chosen", Symbol.Gear);
            var other = CreateTest("other", Symbol.Nut);
            var level = CreateLevel(new[] { chosen, other });
            var database = ScriptableObject.CreateInstance<LevelDatabase>();
            _created.Add(database);
            var model = new ModelInstaller(database, new FixedPlayTestSelector(chosen));

            new LevelModelTapeSetupActionHandler().Apply(new LevelLoadedActionContext(level, model, null));

            Assert.That(model.ActivePlayTest, Is.SameAs(chosen));
            Assert.That(model.CurrentTape, Is.Not.Null);
            Assert.That(model.CurrentTape.HeadIndex, Is.EqualTo(0));
            Assert.That(model.CurrentTape.CurrentSymbol, Is.EqualTo(Symbol.Blank));
            Assert.That(model.CurrentTape.Snapshot().Cells, Is.Empty);
        }

        [Test]
        public void ValidationSetup_UsesEntirePool()
        {
            var first = CreateTest("first", Symbol.Gear);
            var second = CreateTest("second", Symbol.Nut);
            var level = CreateLevel(new[] { first, second });
            var database = ScriptableObject.CreateInstance<LevelDatabase>();
            _created.Add(database);
            var model = new ModelInstaller(database, new FixedPlayTestSelector(first));

            new LevelValidationTestsSetupActionHandler().Apply(new LevelLoadedActionContext(level, model, null));

            Assert.That(model.Validation.Results.Count, Is.EqualTo(2));
            Assert.That(model.Validation.Results[0].ScenarioId, Is.EqualTo("first"));
            Assert.That(model.Validation.Results[1].ScenarioId, Is.EqualTo("second"));
        }

        ValidationTest CreateTest(string scenarioId, Symbol symbol)
        {
            var test = ScriptableObject.CreateInstance<ValidationTest>();
            test.scenarioId = scenarioId;
            test.headIndex = 2;
            test.initialSymbols = new[] { symbol };
            test.expectedSymbols = new[] { symbol };
            _created.Add(test);
            return test;
        }

        LevelDefinition CreateLevel(ValidationTest[] tests)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.validationTests = tests;
            _created.Add(level);
            return level;
        }

        sealed class FixedPlayTestSelector : IPlayTestSelector
        {
            readonly ValidationTest _chosen;

            public FixedPlayTestSelector(ValidationTest chosen)
            {
                _chosen = chosen;
            }

            public ValidationTest Select(IReadOnlyList<ValidationTest> tests, ValidationTest previous)
            {
                return _chosen;
            }
        }
    }
}
