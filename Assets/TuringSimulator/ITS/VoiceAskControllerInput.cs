// VoiceAskControllerInput.cs
// Toggles voice Ask listening from a VR controller button (default: right secondary / B).

using UnityEngine;
using UnityEngine.InputSystem;
using TuringSimulator.GameFlow.Events;

[DefaultExecutionOrder(-90)]
public class VoiceAskControllerInput : MonoBehaviour
{
    [Header("XR Input")]
    [Tooltip("Optional. If unset, binds right-hand secondaryButton at runtime.")]
    [SerializeField] InputActionReference _micToggleAction;

    [Header("Event Channel")]
    [Tooltip("Preferred path: publish mic toggles through this channel.")]
    [SerializeField] MicToggleRequestedEventChannel _micToggleRequestedChannel;

    InputAction _runtimeAction;
    InputAction _boundAction;

    void OnEnable()
    {
        _boundAction = ResolveAction();
        if (_boundAction == null)
        {
            Debug.LogWarning("[VoiceAskControllerInput] No mic toggle action available.");
            return;
        }

        _boundAction.performed += OnMicTogglePerformed;
        if (!_boundAction.enabled)
            _boundAction.Enable();
    }

    void OnDisable()
    {
        if (_boundAction != null)
            _boundAction.performed -= OnMicTogglePerformed;

        if (_runtimeAction != null)
        {
            _runtimeAction.Disable();
            _runtimeAction.Dispose();
            _runtimeAction = null;
        }

        _boundAction = null;
    }

    InputAction ResolveAction()
    {
        if (_micToggleAction != null && _micToggleAction.action != null)
            return _micToggleAction.action;

        _runtimeAction = new InputAction(
            name: "VoiceAskMicToggle",
            type: InputActionType.Button,
            binding: "<XRController>{RightHand}/{SecondaryButton}");
        return _runtimeAction;
    }

    void OnMicTogglePerformed(InputAction.CallbackContext _)
    {
        var eventData = new MicToggleRequestedEventData(
            EventContextFactory.Create(nameof(VoiceAskControllerInput), "controller-mic-toggle"));
        if (_micToggleRequestedChannel != null)
        {
            EventTraceLog.Record(nameof(MicToggleRequestedEventData), eventData.ToString(), this);
            _micToggleRequestedChannel.Raise(eventData, this);
            return;
        }

        if (AgentDialogue.Instance == null)
        {
            Debug.LogWarning("[VoiceAskControllerInput] AgentDialogue not ready.");
            return;
        }

        AgentDialogue.Instance.ToggleMicListening();
    }
}
