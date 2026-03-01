using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ModuleDrawer : MonoBehaviour
{
    private XRSimpleInteractable _interactable;
    public ModuleFactory factory;
    public int numberOfTapes;
    
    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _interactable.selectEntered.AddListener(OnGrab);
    }
    
    private void OnGrab(SelectEnterEventArgs args)
    {
        GameObject module = factory.CreateModule(numberOfTapes);
        XRGrabInteractable interactable = module.GetComponent<XRGrabInteractable>();
        module.transform.position = transform.position;
        interactable.interactionManager.SelectEnter(args.interactorObject, interactable);
    }
}
