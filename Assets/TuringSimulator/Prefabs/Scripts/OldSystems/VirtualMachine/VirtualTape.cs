using System;
using System.Collections.Generic;

public class VirtualTape
{
    private Dictionary<int, SymbolType> _symbols = new Dictionary<int, SymbolType>();
    private int _currentHeadPosition = 0;

    public void SetTape(SymbolType[] symbols)
    {
        _symbols.Clear();
        _currentHeadPosition = 0;
        for (int i = 0; i < symbols.Length; i++)
        {
            if (symbols[i] != SymbolType.None && symbols[i] != SymbolType.Blank)
                _symbols[i] = symbols[i];
        }
    }

    public SymbolType Read()
    {
        return _symbols.GetValueOrDefault(_currentHeadPosition, SymbolType.Blank);
    }

    public void Write(SymbolType symbol)
    {
        if (symbol == SymbolType.None) return;
        if (symbol == SymbolType.Blank)
        {
            _symbols.Remove(_currentHeadPosition);
        }
        else
        {
            _symbols[_currentHeadPosition] = symbol;
        }
    }

    public void Move(int direction)
    {
        _currentHeadPosition += direction;
    }
    
    public SymbolType[] GetFullTape()
    {
        if (_symbols.Count == 0)
            return Array.Empty<SymbolType>();

        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (var index in _symbols.Keys)
        {
            if (index < min) min = index;
            if (index > max) max = index;
        }

        int length = max - min + 1;
        SymbolType[] tapeArray = new SymbolType[length];

        for (int i = 0; i < length; i++)
        {
            int position = min + i;
            tapeArray[i] = _symbols.GetValueOrDefault(position, SymbolType.Blank);
        }

        return tapeArray;
    }
}