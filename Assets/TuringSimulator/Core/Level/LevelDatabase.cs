using System;
using JetBrains.Annotations;
using TuringSimulator.Core.Simulation;
using UnityEngine;

namespace TuringSimulator.Core.Level
{
    [CreateAssetMenu(
        menuName = "Turing Simulator/Level Database",
        fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private LevelDefinition[] levels;

        public int Count => levels.Length;

        public int ValidationScenarioCount
        {
            get
            {
                var count = 0;
                foreach (var level in levels)
                {
                    if (level != null)
                        count += level.ValidationScenarioCount;
                }

                return count;
            }
        }

        public LevelDefinition Get(int index)
        {
            if (index < 0 || index >= levels.Length)
                throw new IndexOutOfRangeException($"Level {index} does not exist.");

            return levels[index];
        }
    }
}