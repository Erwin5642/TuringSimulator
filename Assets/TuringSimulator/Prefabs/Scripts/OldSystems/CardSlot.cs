using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CardSlot : MonoBehaviour
{
    public CardType acceptedType;
    public CardData cardData; // Stores data of the card in the slot
    private XRSocketInteractor _socketInteractor; // The socket interactor for the slot
    private bool _blockInteractions = false; // Flag to control blocking interactions

    void Awake()
    {
        _socketInteractor = GetComponent<XRSocketInteractor>();
        _socketInteractor.selectEntered.AddListener(OnSelectEntered);
        _socketInteractor.selectExited.AddListener(OnSelectExited);
    }

    // This method will block or unblock interactions based on the flag
    public void BlockInteraction(bool block)
    {
        _blockInteractions = block;
        
        if (_blockInteractions)
        {
            _socketInteractor.selectEntered.RemoveListener(OnSelectEntered); 
            _socketInteractor.selectExited.RemoveListener(OnSelectExited);   
        }
        else
        {
            _socketInteractor.selectEntered.AddListener(OnSelectEntered); 
            _socketInteractor.selectExited.AddListener(OnSelectExited);   
        }
    }

    // Get the current card's value, if any
    public int GetCurrentCardValue()
    {
        return cardData != null ? 
            (acceptedType == CardType.Material ? (int)cardData.symbol : cardData.direction) : 0;
    }

    // When a card is inserted into the socket, update card data
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_blockInteractions)
            return; // Prevent inserting if interactions are blocked

        cardData = args.interactableObject.transform.GetComponent<CardData>();
    }

    // When a card is removed from the socket, reset card data
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (_blockInteractions)
            return; // Prevent removing if interactions are blocked

        cardData = null;
    }
}
