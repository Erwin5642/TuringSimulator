/*using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine;
using TuringSimulator.View.Machine.Halt;
using TuringSimulator.View.Machine.Tape;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayModeTests
{
    public class PlaybackIntegrationTests
    {
        // A container to clean up GameObjects after each test
        private GameObject _testContext;

        [TearDown]
        public void Teardown()
        {
            if (_testContext != null)
                Object.Destroy(_testContext);
        }

        [UnityTest]
        public IEnumerator Play_ShouldAdvanceSteps_AndTriggerVisuals()
        {
            // --- 1. SETUP ---
            _testContext = new GameObject("IntegrationTestContext");
            _testContext.SetActive(false); 

            // Components
            var machineViewer = _testContext.AddComponent<MachineViewer>();
            var tapeMock = _testContext.AddComponent<MockTapeVisual>();
            var haltMock = _testContext.AddComponent<MockHaltIndicator>();

            // Injection
            InjectDependency(machineViewer, "tape", tapeMock);
            InjectDependency(machineViewer, "halt", haltMock);

            _testContext.SetActive(true);

            // Data & Controller
            var bufferMock = new MockSimulationBuffer();
            var stepApplier = new StepViewApplier(bufferMock, machineViewer);
            var playbackController = new PlaybackController(stepApplier);

            // --- 2. ACT ---
            Task playTask = playbackController.Play();

            // Wait with timeout
            float timeout = 2.0f;
            float timer = 0.0f;
            while (!playTask.IsCompleted && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // --- 3. ASSERT ---
            Assert.IsTrue(playTask.IsCompleted, "Playback task timed out - Coroutines likely stuck.");
    
            // Logic Assertions
            Assert.AreEqual(2, stepApplier.CurrentStepIndex, "StepApplier did not increment index correctly.");
    
            // Visual Assertions
            Assert.AreEqual(2, tapeMock.writeCallCount, "Tape.ShowWrite should be called once per step.");
            Assert.AreEqual(2, tapeMock.moveCallCount, "Tape.MoveHead should be called once per step.");
        }
        // --- HELPER METHODS ---

        private void InjectDependency(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogError($"Could not find field {fieldName} on {target.GetType()}");
            }
        }

        // --- MOCK IMPLEMENTATIONS ---

        // Mock for the Data Buffer
        // Mock for the Data Buffer
        public class MockSimulationBuffer : ISimulationBuffer
        {
            public HaltStatus Status => HaltStatus.None;
            public bool IsRunning => true;
            public bool IsHalted => false;

            // Returns a step for index 0 and 1, then stops.
            public bool TryGetStep(int index, out StepResult stepResult)
            {
                if (index < 2)
                {
                    // Create a valid Diff result using your new struct definition
                    stepResult = CreateDummyDiffStep(index);
                    return true;
                }

                stepResult = default; // Invalid/Empty result
                return false;
            }

            public void AddStepDiff(StepDiff stepDiff) { }
            public void Complete(HaltStatus status) { }
            public void Clear() { }

            // Helper to construct your specific StepResult
            private StepResult CreateDummyDiffStep(int index)
            {
                // specific values to ensure DirectionMoved = Right (1 - 0 = 1)
                var diff = new StepDiff(
                    symbolBefore: Symbol.Zero,
                    symbolAfter: Symbol.One,
                    headIndexBefore: 0,
                    headIndexAfter: 1,      // Implies MoveDirection.Right
                    previousState: 0,
                    nextState: 1,
                    stepIndex: index
                );

                return new StepResult(diff);
            }
        }
        
        // Mock for Tape Visuals
        public class MockTapeVisual : MonoBehaviour, ITapeVisual
        {
            public int writeCallCount;
            public int moveCallCount;

            public int HeadIndex { get; } = 0;
            public void Initialize(IReadOnlyList<Symbol> symbols, int headIndex) {  }
    
            public IEnumerator MoveHead(MoveDirection direction) 
            { 
                moveCallCount++;
                yield return null; 
            }
    
            public IEnumerator ShowRead() { yield return null; }
    
            public IEnumerator ShowWrite(Symbol symbol) 
            { 
                writeCallCount++;
                yield return null; 
            }
        }
        
        // Mock for Halt Indicator
        public class MockHaltIndicator : MonoBehaviour, IHaltStatusIndicator
        {
            public void Initialize()
            {
                throw new System.NotImplementedException();
            }

            public IEnumerator Show(HaltStatus status) { yield break; }
            public void Reset()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}*/