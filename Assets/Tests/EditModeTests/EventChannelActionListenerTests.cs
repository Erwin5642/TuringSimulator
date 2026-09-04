using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TuringSimulator.GameFlow.Events;
using UnityEngine;
using UnityEngine.Events;

namespace EditModeTests
{
    public class EventChannelActionListenerTests
    {
        GameObject _go;
        LevelOutcomeEventChannel _channel;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
            if (_channel != null)
                Object.DestroyImmediate(_channel);
        }

        [Test]
        public void Invoke_OnlyWhenFilterMatches()
        {
            var matched = 0;
            CreateListener("Outcome", "Victory", () => matched++);

            _channel.Raise(Outcome(LevelOutcomeKind.Victory));
            _channel.Raise(Outcome(LevelOutcomeKind.Defeat));

            Assert.That(matched, Is.EqualTo(1));
        }

        [Test]
        public void EmptyFilter_InvokesOnEveryRaise()
        {
            var matched = 0;
            CreateListener(null, null, () => matched++);

            _channel.Raise(Outcome(LevelOutcomeKind.Victory));
            _channel.Raise(Outcome(LevelOutcomeKind.Defeat));

            Assert.That(matched, Is.EqualTo(2));
        }

        void CreateListener(string matchProperty, string matchValue, UnityAction onMatched)
        {
            _channel = ScriptableObject.CreateInstance<LevelOutcomeEventChannel>();
            _go = new GameObject(nameof(EventChannelActionListenerTests));
            _go.SetActive(false);

            var listener = _go.AddComponent<EventChannelActionListener>();
            var onMatchedEvent = new UnityEvent();
            onMatchedEvent.AddListener(onMatched);

            var bindings = new List<EventChannelActionListener.Binding>
            {
                new()
                {
                    Name = "Test",
                    SourceChannel = _channel,
                    MatchProperty = matchProperty,
                    MatchValue = matchValue,
                    OnMatched = onMatchedEvent,
                },
            };

            var field = typeof(EventChannelActionListener)
                .GetField("_bindings", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(listener, bindings);

            _go.SetActive(true);
        }

        static LevelOutcomeEventData Outcome(LevelOutcomeKind kind)
        {
            return new LevelOutcomeEventData(
                EventContextFactory.Create(nameof(EventChannelActionListenerTests), "test"),
                "level-1",
                kind);
        }
    }
}
