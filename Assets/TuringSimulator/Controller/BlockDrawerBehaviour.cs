using TuringSimulator.Core.ProgramGraph;
using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Holds prefabs used by child <see cref="BlockDrawSlotBehaviour"/> slots when spawning grabbed blocks.
    /// Place on the BlockDrawer root object (parent of all draw slots).
    /// </summary>
    public sealed class BlockDrawerBehaviour : MonoBehaviour
    {
        [Header("Block Prefabs")]
        [SerializeField] GameObject moveBlockPrefab;
        [SerializeField] GameObject writeBlockPrefab;
        [SerializeField] GameObject conditionBlockPrefab;
        [SerializeField] GameObject acceptBlockPrefab;
        [SerializeField] GameObject rejectBlockPrefab;

        public GameObject MoveBlockPrefab => moveBlockPrefab;
        public GameObject WriteBlockPrefab => writeBlockPrefab;
        public GameObject ConditionBlockPrefab => conditionBlockPrefab;
        public GameObject AcceptBlockPrefab => acceptBlockPrefab;
        public GameObject RejectBlockPrefab => rejectBlockPrefab;

        public GameObject GetBlockPrefab(ProgramBlockKind kind)
        {
            return kind switch
            {
                ProgramBlockKind.Move => moveBlockPrefab,
                ProgramBlockKind.Write => writeBlockPrefab,
                ProgramBlockKind.Condition => conditionBlockPrefab,
                ProgramBlockKind.Accept => acceptBlockPrefab,
                ProgramBlockKind.Reject => rejectBlockPrefab,
                _ => null
            };
        }
    }
}
