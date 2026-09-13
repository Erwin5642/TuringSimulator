using System;
using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Validation;
using UnityEngine;

namespace EditModeTests
{
    public class PlayTestSelectorTests
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
        public void Select_SingleTest_AlwaysReturnsThatTest()
        {
            var only = CreateTest("only");
            var selector = new PlayTestSelector(new Random(1));

            Assert.That(selector.Select(new[] { only }, null), Is.SameAs(only));
            Assert.That(selector.Select(new[] { only }, only), Is.SameAs(only));
        }

        [Test]
        public void Select_ExcludesPreviousWhenPoolHasMoreThanOne()
        {
            var first = CreateTest("a");
            var second = CreateTest("b");
            var selector = new PlayTestSelector(new Random(7));

            var chosen = selector.Select(new[] { first, second }, null);
            var next = selector.Select(new[] { first, second }, chosen);

            Assert.That(next, Is.Not.SameAs(chosen));
            Assert.That(new[] { first, second }, Does.Contain(next));
        }

        [Test]
        public void Select_SeededRng_StaysInPool()
        {
            var tests = new[] { CreateTest("a"), CreateTest("b"), CreateTest("c") };
            var selector = new PlayTestSelector(new Random(42));

            for (var i = 0; i < 20; i++)
            {
                var chosen = selector.Select(tests, i == 0 ? null : tests[i % tests.Length]);
                Assert.That(tests, Does.Contain(chosen));
            }
        }

        [Test]
        public void Select_EmptyOrAllNull_Throws()
        {
            var selector = new PlayTestSelector(new Random(1));
            Assert.That(() => selector.Select(Array.Empty<ValidationTest>(), null), Throws.ArgumentException);
            Assert.That(() => selector.Select(new ValidationTest[] { null, null }, null), Throws.ArgumentException);
        }

        [Test]
        public void RequirePool_SkipsNulls_AndThrowsWhenEmpty()
        {
            var a = CreateTest("a");
            var level = CreateLevel(new[] { null, a, null });

            var pool = LevelTestPool.Require(level);
            Assert.That(pool, Is.EqualTo(new[] { a }));

            var empty = CreateLevel(Array.Empty<ValidationTest>());
            Assert.That(() => LevelTestPool.Require(empty), Throws.InvalidOperationException);

            var allNull = CreateLevel(new ValidationTest[] { null });
            Assert.That(() => LevelTestPool.Require(allNull), Throws.InvalidOperationException);
        }

        [Test]
        public void ValidationScenarioCount_IsPoolLengthOnly()
        {
            var level = CreateLevel(new[] { CreateTest("a"), CreateTest("b"), CreateTest("c") });
            Assert.That(level.ValidationScenarioCount, Is.EqualTo(3));

            var empty = CreateLevel(null);
            Assert.That(empty.ValidationScenarioCount, Is.EqualTo(0));
        }

        ValidationTest CreateTest(string scenarioId)
        {
            var test = ScriptableObject.CreateInstance<ValidationTest>();
            test.scenarioId = scenarioId;
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
    }
}
