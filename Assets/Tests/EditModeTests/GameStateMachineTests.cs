using NUnit.Framework;
using TuringSimulator.GameFlow;

namespace EditModeTests
{
    public class GameStateMachineTests
    {
        [Test]
        public void IsAllowed_DefeatToEditing_ReturnsTrue()
        {
            Assert.That(GameStateMachine.IsAllowed(GameState.Defeat, GameState.Editing), Is.True);
        }

        [Test]
        public void IsAllowed_DefeatToRunning_ReturnsFalse()
        {
            Assert.That(GameStateMachine.IsAllowed(GameState.Defeat, GameState.Running), Is.False);
        }

        [Test]
        public void IsAllowed_EditingToRunning_ReturnsTrue()
        {
            Assert.That(GameStateMachine.IsAllowed(GameState.Editing, GameState.Running), Is.True);
        }

        [Test]
        public void IsAllowed_DebuggingToEditing_ReturnsTrue()
        {
            Assert.That(GameStateMachine.IsAllowed(GameState.Debugging, GameState.Editing), Is.True);
        }
    }
}
