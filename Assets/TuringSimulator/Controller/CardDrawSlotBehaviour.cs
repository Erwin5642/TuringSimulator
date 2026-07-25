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

        CardDrawerBehaviour _drawer;

        bool _busy;

        void Awake()
        {
            _slotGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            // Cache before grab: XRGrabInteractable unparents on select, so GetComponentInParent fails in OnSelectEntered.
            _drawer = GetComponentInParent<CardDrawerBehaviour>();
            if (_drawer == null)
                Debug.LogError($"[CardDrawSlot] No {nameof(CardDrawerBehaviour)} on parents of '{name}'.");
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
            if (_busy || _drawer == null)
                return;

            GameObject prefab = type == CardDrawSlotKind.Symbol
                ? _drawer.SymbolCardPrefab
                : _drawer.DirectionCardPrefab;

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

            var cardGrab = cardGo.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var manager = _slotGrab.interactionManager;
            var interactorObj = args.interactorObject;
            var slotInteractable = args.interactableObject;

            if (cardGrab != null && manager != null)
                cardGrab.interactionManager = manager;

            // Let the spawned interactable register with the manager.
            yield return null;

            // Prevent the infinite drawer slot from being re-selected while grip is still held.
            _slotGrab.enabled = false;

            if (manager != null &&
                interactorObj is IXRSelectInteractor interactor &&
                slotInteractable is IXRSelectInteractable slotIx &&
                cardGrab != null)
            {
                if (slotIx.isSelected)
                    manager.SelectExit(interactor, slotIx);

                manager.SelectEnterUnconditionally(interactor, cardGrab);

                ProgramWorkbench.Instance?.RegisterSpawnedCard(cardGo);

                if (!cardGrab.isSelected)
                    Debug.LogError($"[CardDrawSlot] Grab transfer failed for slot '{name}'.");

                while (interactor.isSelectActive)
                    yield return null;
            }
            else
            {
                ProgramWorkbench.Instance?.RegisterSpawnedCard(cardGo);
                Debug.LogError($"[CardDrawSlot] Cannot transfer grab for slot '{name}' (missing manager/interactor/card grab).");
            }

            _slotGrab.enabled = true;
            _busy = false;
        }
    }
}
