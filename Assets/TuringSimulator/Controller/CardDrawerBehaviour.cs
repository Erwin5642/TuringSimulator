using UnityEngine;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// Holds prefabs used by child <see cref="CardDrawSlotBehaviour"/> slots when spawning grabbed cards.
    /// Place on the CardDrawer root object (parent of all draw slots).
    /// </summary>
    public sealed class CardDrawerBehaviour : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Prefab with SymbolCardBehaviour + XRGrabInteractable (payload set when spawning).")]
        [SerializeField] GameObject symbolCardPrefab;

        [Tooltip("Prefab with DirectionCardBehaviour + XRGrabInteractable.")]
        [SerializeField] GameObject directionCardPrefab;

        public GameObject SymbolCardPrefab => symbolCardPrefab;

        public GameObject DirectionCardPrefab => directionCardPrefab;
    }
}
