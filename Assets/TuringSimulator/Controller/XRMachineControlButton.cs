using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TuringSimulator.Controller
{
    /// <summary>
    /// XR button adapter for machine playback controls.
    /// Wire one component per button: Start/Abort, Pause/Resume, Step Forward, Step Backward.
    /// Menu and Next remain UI-only controls.
    /// Button labels are driven by MachineControlButtonLabel (Começar/Recomeçar, Pausar/Rodar).
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class XRMachineControlButton : MonoBehaviour
    {
        public enum MachineControlCommand
        {
            StartOrAbort = 0,
            PauseOrResume = 1,
            StepForward = 2,
            StepBackward = 3,
        }

        [Header("Command")]
        [SerializeField] private MachineControlCommand _command = MachineControlCommand.PauseOrResume;

        [Header("Target")]
        [Tooltip("Optional. If empty, resolves the first PlayerInputCatcher in scene.")]
        [SerializeField] private PlayerInputCatcher _playerInput;

        XRSimpleInteractable _interactable;

        void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
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
                case MachineControlCommand.StartOrAbort:
                    input.RequestStartOrAbort();
                    break;
                case MachineControlCommand.PauseOrResume:
                    input.RequestPlayOrPause();
                    break;
                case MachineControlCommand.StepForward:
                    input.RequestStepForward();
                    break;
                case MachineControlCommand.StepBackward:
                    input.RequestStepBackward();
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
