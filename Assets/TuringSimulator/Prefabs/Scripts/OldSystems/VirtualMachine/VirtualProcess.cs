using UnityEngine;

public class VirtualProcess
{
    private VirtualTuringMachine _machine;
    private VirtualProgramNode _programRoot;
    
    public VirtualProcess(VirtualProgramNode root)
    {
        _programRoot = root;
        _machine = new VirtualTuringMachine();
    }

    public void Initialize(SymbolType[][] tapeInputs)
    {
        _machine.CreateTapes(tapeInputs.Length);
        for (int i = 0; i < tapeInputs.Length; i++)
        {
            _machine.SetInput(tapeInputs[i], i);
        }
    }

    public bool Execute()
    {
        return ExecuteNode(_programRoot);
    }

    private bool ExecuteNode(VirtualProgramNode node, int depth = 0)
    {
        if (node == null)
            return true;
        if(depth == 500) return false;
        
        bool result = _machine.ExecuteAction(node.operations);
        
        if (result)
        {
            return ExecuteNode(node.leftNode, depth + 1);
        }
        else
        {
            return ExecuteNode(node.rightNode, depth + 1);
        }
    }

    public SymbolType[] GetTapeOutput(int tapeIndex)
    {
        return _machine.GetTapeOutput(tapeIndex);
    }
}