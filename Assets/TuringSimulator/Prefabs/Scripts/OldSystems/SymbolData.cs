using UnityEngine;

public enum SymbolType
{
    None = 0,
    Blank = 1,
    Gear = 2,
    Nut = 3,
    Bolt = 4
}

public class SymbolData : MonoBehaviour
{
    public SymbolType materialType;

    public static int SymbolToInt(SymbolType symbol)
    {
        return (int)symbol;
    }

    public static SymbolType IntToSymbolType(int symbol)
    {
        if (System.Enum.IsDefined(typeof(SymbolType), symbol))
        {
            return (SymbolType)symbol;
        }
        Debug.LogWarning("Invalid integer for SymbolType. Returning None.");
        return SymbolType.None;
    }
}
