using UnityEngine;

public enum CardType { Material, Direction }

public class CardData : MonoBehaviour {
    public CardType type;
    public SymbolType symbol = SymbolType.Blank;
    public int direction;
}
