using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CardDrawer : MonoBehaviour
{
    public CardFactory factory; // A reference to the CardFactory to create cards
    public CardData cardData;
    private GameObject _currentCard;// The data for the card to be drawn
    private XRGrabInteractable _grabInteractable;
    private XRSimpleInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _interactable.selectEntered.AddListener(OnGrabTopCard);
    }

    private void CreateNewTopCard()
    {
        _currentCard = cardData.type == CardType.Direction
            ? factory.CreateDirectionCard(cardData.direction)
            : factory.CreateSymbolCard(cardData.symbol);
        _grabInteractable = _currentCard.GetComponent<XRGrabInteractable>();
        _currentCard.transform.position = transform.position;
    }
    
    private void OnGrabTopCard(SelectEnterEventArgs args)
    {
        CreateNewTopCard();
        _grabInteractable.interactionManager.SelectEnter(args.interactorObject, _grabInteractable);
    }
}
