
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CardFactory : MonoBehaviour
{
     [SerializeField] private List<GameObject> cardSymbolPrefabs;
     [SerializeField] private List<GameObject> cardDirectionPrefabs;
     private Dictionary<SymbolType, GameObject> _cardSymbolPrefabsDictionary;
     private Dictionary<int, GameObject> _cardDirectionPrefabsDictionary;
     private List<GameObject> _cardInstances;
    
    public CommandScheduler commandScheduler;

    void Awake()
    {
        _cardInstances = new List<GameObject>();
        _cardSymbolPrefabsDictionary = new Dictionary<SymbolType, GameObject>();
        _cardDirectionPrefabsDictionary = new Dictionary<int, GameObject>();

        foreach (var card in cardSymbolPrefabs)
        {
            CardData cardData = card.GetComponent<CardData>();
            _cardSymbolPrefabsDictionary[cardData.symbol] = card;
        }
        foreach (var card in cardDirectionPrefabs)
        {
            CardData cardData = card.GetComponent<CardData>();
            _cardDirectionPrefabsDictionary[cardData.direction] = card;
        }
    }
    
    public GameObject CreateDirectionCard(int direction)
    {
        GameObject newCard = Instantiate(_cardDirectionPrefabsDictionary[direction], transform.position, Quaternion.identity);
        _cardInstances.Add(newCard);

        return newCard;
    }
    
    public GameObject CreateSymbolCard(SymbolType symbol)
    {
        GameObject newCard = Instantiate(_cardSymbolPrefabsDictionary[symbol], transform.position, Quaternion.identity);
        
        _cardInstances.Add(newCard);

        return newCard;
    }
}
