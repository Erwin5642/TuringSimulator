using System.Collections.Generic;

public class VirtualTuringMachine
{
    private List<VirtualTape> _tapes = new List<VirtualTape>();

    public void CreateTapes(int numberOfTapes)
    {
        _tapes.Clear();
        for (int i = 0; i < numberOfTapes; i++)
            _tapes.Add(new VirtualTape());
    }
    
    public void SetInput(SymbolType[] symbols, int tapeIndex)
    {
        _tapes[tapeIndex].SetTape(symbols);
    }
    
    public bool ExecuteAction(TuringOperation[] actions)
    {
        int tapesCount = _tapes.Count;
        for (int i = 0; i < tapesCount; i++)
        {
            if(actions[i].condSymbol != SymbolType.None && _tapes[i].Read() != actions[i].condSymbol)
                return false;
        }
        for (int i = 0; i < tapesCount; i++)
        {
            _tapes[i].Write(actions[i].actionSymbol);
            _tapes[i].Move(actions[i].direction);
        }
        
        return true;
    }
    
    public SymbolType[] GetTapeOutput(int tapeIndex)
    {
        if (tapeIndex < 0 || tapeIndex >= _tapes.Count)
            return null;

        return _tapes[tapeIndex].GetFullTape();
    }
}
