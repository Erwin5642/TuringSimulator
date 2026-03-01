using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StartButton : MonoBehaviour
{
    private XRSimpleInteractable _interactable;
    public WireConnector signalOutput;
    public CommandScheduler scheduler;
    private bool _isPressed = false;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
    }
    
    public XRSimpleInteractable GetInteractable() => _interactable;
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!_isPressed)
        {
            _isPressed = true;
            if (signalOutput != null)
            {
                scheduler.StartScheduler();
                signalOutput.PropagateSignal();
            }
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (_isPressed)
        {
            _isPressed = false;
        }
    }
}
