using System.Collections;
using TuringSimulator.Core.ProgramGraph;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// XR block drawer slot: when the player grabs this interactable,
    /// spawns the configured block and transfers the grab to it.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public sealed class BlockDrawSlotBehaviour : MonoBehaviour
    {
        [SerializeField] ProgramBlockKind blockKind = ProgramBlockKind.Move;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _slotGrab;
        BlockDrawerBehaviour _drawer;
        static int _spawnSequence;
        bool _busy;

        void Awake()
        {
            _slotGrab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            // Cache before grab: XRGrabInteractable unparents on select, so GetComponentInParent fails in OnSelectEntered.
            _drawer = GetComponentInParent<BlockDrawerBehaviour>();
            if (_drawer == null)
                Debug.LogError($"[BlockDrawSlot] No {nameof(BlockDrawerBehaviour)} on parents of '{name}'.");
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

            var prefab = _drawer.GetBlockPrefab(blockKind);
            if (prefab == null)
            {
                Debug.LogWarning($"[BlockDrawSlot] Missing prefab for slot '{name}' ({blockKind}).");
                return;
            }

            StartCoroutine(SpawnAndTransferGrab(args, prefab));
        }

        IEnumerator SpawnAndTransferGrab(SelectEnterEventArgs args, GameObject prefab)
        {
            _busy = true;

            var blockGo = Instantiate(prefab, transform.position, transform.rotation);
            var block = blockGo.GetComponentInChildren<ProgramBlockBehaviour>();
            if (block != null)
                block.AssignRuntimeBlockId(BuildRuntimeBlockId(blockKind));
            else
                Debug.LogWarning($"[BlockDrawSlot] Spawned prefab '{prefab.name}' has no {nameof(ProgramBlockBehaviour)}.");

            var blockGrab = block != null
                ? block.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()
                : blockGo.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var manager = _slotGrab.interactionManager;
            var interactorObj = args.interactorObject;
            var slotInteractable = args.interactableObject;

            if (blockGrab != null && manager != null)
                blockGrab.interactionManager = manager;

            // Let the spawned interactable register with the manager.
            yield return null;

            // Prevent the infinite drawer slot from being re-selected while grip is still held.
            _slotGrab.enabled = false;

            if (manager != null &&
                interactorObj is IXRSelectInteractor interactor &&
                slotInteractable is IXRSelectInteractable slotIx &&
                blockGrab != null)
            {
                if (slotIx.isSelected)
                    manager.SelectExit(interactor, slotIx);

                manager.SelectEnterUnconditionally(interactor, blockGrab);

                ProgramWorkbench.Instance?.RegisterSpawnedBlock(blockGo);

                if (!blockGrab.isSelected)
                    Debug.LogError($"[BlockDrawSlot] Grab transfer failed for slot '{name}'.");

                while (interactor.isSelectActive)
                    yield return null;
            }
            else
            {
                ProgramWorkbench.Instance?.RegisterSpawnedBlock(blockGo);
                Debug.LogError($"[BlockDrawSlot] Cannot transfer grab for slot '{name}' (missing manager/interactor/block grab).");
            }

            _slotGrab.enabled = true;
            _busy = false;
        }

        static string BuildRuntimeBlockId(ProgramBlockKind kind)
        {
            _spawnSequence++;
            return $"spawned-{kind}-{_spawnSequence}";
        }
    }
}
