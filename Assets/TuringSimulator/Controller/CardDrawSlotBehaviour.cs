using System.Collections;
using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// XR drawer slot: when the player grabs this interactable, spawns the configured card and transfers the grab to it.
    /// Serialized layout matches legacy CardDrawer slots (type / symbol / direction).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public sealed class CardDrawSlotBehaviour : MonoBehaviour
    {
        public enum CardDrawSlotKind
        {
            Symbol = 0,
            Direction = 1,
        }

        [SerializeField] CardDrawSlotKind type;

        [SerializeField] Symbol symbol;

        [SerializeField] MoveDirection direction;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _slotGrab;

        bool _busy;

        void Awake()
        {
            _slotGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        void OnEnable()
        {
            _slotGrab.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            _slotGrab.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_busy)
                return;

            var drawer = GetComponentInParent<CardDrawerBehaviour>();
            if (drawer == null)
            {
                Debug.LogWarning($"[CardDrawSlot] No {nameof(CardDrawerBehaviour)} on parents of '{name}'.");
                return;
            }

            GameObject prefab = type == CardDrawSlotKind.Symbol
                ? drawer.SymbolCardPrefab
                : drawer.DirectionCardPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[CardDrawSlot] Missing prefab for slot '{name}' ({type}).");
                return;
            }

            StartCoroutine(SpawnAndTransferGrab(args, prefab));
        }

        IEnumerator SpawnAndTransferGrab(SelectEnterEventArgs args, GameObject prefab)
        {
            _busy = true;

            var cardGo = Instantiate(prefab, transform.position, transform.rotation);
            if (type == CardDrawSlotKind.Symbol &&
                cardGo.TryGetComponent<SymbolCardBehaviour>(out var sym))
                sym.Configure(symbol);
            else if (type == CardDrawSlotKind.Direction &&
                     cardGo.TryGetComponent<DirectionCardBehaviour>(out var dir))
                dir.Configure(direction);

            ProgramWorkbench.Instance?.RegisterSpawnedCard(cardGo);

            var cardGrab = cardGo.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var manager = _slotGrab.interactionManager;
            var interactorObj = args.interactorObject;
            var slotInteractable = args.interactableObject;

            yield return null;

            if (manager != null &&
                interactorObj is IXRSelectInteractor interactor &&
                slotInteractable is IXRSelectInteractable slotIx &&
                cardGrab != null)
            {
                manager.SelectExit(interactor, slotIx);
                manager.SelectEnter(interactor, cardGrab);
            }

            _busy = false;
        }
    }
}
