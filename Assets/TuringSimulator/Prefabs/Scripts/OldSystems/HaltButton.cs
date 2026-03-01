using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HaltButton : MonoBehaviour
{
    private XRSimpleInteractable _interactable;
    public CommandScheduler scheduler;
    private bool _isPressed;

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
            scheduler.Halt();
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