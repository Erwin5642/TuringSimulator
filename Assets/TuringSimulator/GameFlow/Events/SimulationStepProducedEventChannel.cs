using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    [CreateAssetMenu(menuName = "TuringSimulator/Events/Simulation Step Produced", fileName = "SimulationStepProducedChannel")]
    public sealed class SimulationStepProducedEventChannel : EventChannelSO<SimulationStepProducedEventData>
    {
    }
}
