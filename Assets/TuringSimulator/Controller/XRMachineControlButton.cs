using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// XR button adapter that triggers existing machine control requests.
    /// Wire one component per button and select the command in Inspector.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
    public sealed class XRMachineControlButton : MonoBehaviour
    {
        public enum MachineControlCommand
        {
            Start = 0,
            PlayResume = 1,
            Pause = 2,
            StepForward = 3,
            StepBackward = 4,
            Next = 5,
            Menu = 6,
            Abort = 7,
        }

        [Header("Command")]
        [SerializeField] private MachineControlCommand _command = MachineControlCommand.PlayResume;

        [Header("Target")]
        [Tooltip("Optional. If empty, resolves the first PlayerInputCatcher in scene.")]
        [SerializeField] private PlayerInputCatcher _playerInput;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;

        void Awake()
        {
            _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (_playerInput == null)
                _playerInput = FindAnyObjectByType<PlayerInputCatcher>();
        }

        void OnEnable()
        {
            if (_interactable != null)
                _interactable.selectEntered.AddListener(HandleSelectEntered);
        }

        void OnDisable()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(HandleSelectEntered);
        }

        void HandleSelectEntered(SelectEnterEventArgs _)
        {
            var input = ResolveInput();
            if (input == null)
            {
                Debug.LogWarning($"[XRMachineControlButton] Missing {nameof(PlayerInputCatcher)} for command '{_command}'.", this);
                return;
            }

            switch (_command)
            {
                case MachineControlCommand.Start:
                    input.RequestStart();
                    break;
                case MachineControlCommand.PlayResume:
                    input.RequestPlay();
                    break;
                case MachineControlCommand.Pause:
                    input.RequestPause();
                    break;
                case MachineControlCommand.StepForward:
                    input.RequestStepForward();
                    break;
                case MachineControlCommand.StepBackward:
                    input.RequestStepBackward();
                    break;
                case MachineControlCommand.Next:
                    input.RequestNext();
                    break;
                case MachineControlCommand.Menu:
                    input.RequestMenu();
                    break;
                case MachineControlCommand.Abort:
                    input.RequestAbort();
                    break;
                default:
                    Debug.LogWarning($"[XRMachineControlButton] Unsupported command '{_command}'.", this);
                    break;
            }
        }

        PlayerInputCatcher ResolveInput()
        {
            if (_playerInput != null)
                return _playerInput;

            _playerInput = FindAnyObjectByType<PlayerInputCatcher>();
            return _playerInput;
        }
    }
}
