using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VirtualProgramNode
{
    public TuringOperation[] operations;
    public VirtualProgramNode leftNode = null;
    public VirtualProgramNode rightNode = null;

    public VirtualProgramNode(TuringOperation[] operations)
    {
        this.operations = operations;
        leftNode = null;
        rightNode = null;
    }
}
public class VirtualProgram
{
    private VirtualProgramNode _rootNode;
    public VirtualProgramNode rootNode => _rootNode;
    
    public void CreateProgram(WireConnector begin)
    {
        if (begin == null || !begin.isOutputPort || begin.GetConnectedWireCount() == 0)
        {
            _rootNode = null;
            return;
        }

        WireConnector input = begin.GetConnectedWires()[0].GetEndConnector();
        if (input == null)
        {
            _rootNode = null;
            return;
        }

        Module targetModule = input.GetModule();
        if (targetModule == null)
        {
            _rootNode = null;
            return;
        }

        var visited = new Dictionary<Module, VirtualProgramNode>();
        _rootNode = _CreateProgram(targetModule, visited);
    }

    private VirtualProgramNode _CreateProgram(Module module, Dictionary<Module, VirtualProgramNode> visited)
    {
        if (module == null) return null;

        if (visited.TryGetValue(module, out var program))
        {
            return program;
        }

        TuringOperation[] operations = module.GetCardValues();
        if(operations == null || operations.Length == 0) return null;
        
        VirtualProgramNode node = new VirtualProgramNode(operations);
        visited[module] = node; // Mark as visited BEFORE traversing children

        // Process the true branch
        WireConnector trueConnector = module.trueOutput;
        if (trueConnector != null && trueConnector.GetConnectedWireCount() > 0)
        {
            WireConnector trueInput = trueConnector.GetConnectedWires()[0].GetEndConnector();
            if (trueInput != null)
            {
                Module trueModule = trueInput.GetModule();
                node.leftNode = _CreateProgram(trueModule, visited);
            }
        }

        // Process the false branch
        WireConnector falseConnector = module.falseOutput;
        if (falseConnector != null && falseConnector.GetConnectedWireCount() > 0)
        {
            WireConnector falseInput = falseConnector.GetConnectedWires()[0].GetEndConnector();
            if (falseInput != null)
            {
                Module falseModule = falseInput.GetModule();
                node.rightNode = _CreateProgram(falseModule, visited);
            }
        }
        
        return node;
    }
    public VirtualProcess CreateProcess(SymbolType[][] tapeInputs)
    {
        VirtualProcess process = new VirtualProcess(_rootNode);
        process.Initialize(tapeInputs);
        return process;
    }
}
