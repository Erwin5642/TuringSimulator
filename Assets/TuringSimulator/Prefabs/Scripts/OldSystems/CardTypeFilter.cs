using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CardTypeFilter : MonoBehaviour, IXRSelectFilter
{
    public CardSlot slot;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        var card = interactable.transform.GetComponent<CardData>();
        if (card == null)
        {
            return false;
        }

        bool isValid = card.type == slot.acceptedType;

        return isValid;
    }
    
    public bool canProcess => enabled;
}   