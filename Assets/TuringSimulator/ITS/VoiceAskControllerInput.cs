// VoiceAskControllerInput.cs
// Toggles voice Ask listening from a VR controller button (default: right secondary / B).

using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-90)]
public class VoiceAskControllerInput : MonoBehaviour
{
    [Header("XR Input")]
    [Tooltip("Optional. If unset, binds right-hand secondaryButton at runtime.")]
    [SerializeField] InputActionReference _micToggleAction;

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
        if (AgentDialogue.Instance == null)
        {
            Debug.LogWarning("[VoiceAskControllerInput] AgentDialogue not ready.");
            return;
        }

        AgentDialogue.Instance.ToggleMicListening();
    }
}
