using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyLibrary.Scripts;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class TuringOperation
{
    public SymbolType condSymbol;
    public SymbolType actionSymbol;
    public int direction;

    public TuringOperation(SymbolType condSymbol, SymbolType actionSymbol, int direction)
    {
        this.condSymbol = condSymbol;
        this.actionSymbol = actionSymbol;
        this.direction = direction;
    }
}

public class Module : MonoBehaviour
{
    public List<CardSlot> cardSlots = new List<CardSlot>();
    public Light lampLight;

    public WireConnector trueOutput;
    public WireConnector falseOutput;

    [FormerlySerializedAs("turingMachine")] public TM tm;
    
    public CommandScheduler scheduler;
    
    public void ReceiveSignal()
    {
        int[] inputs = cardSlots.Select(x => x.GetCurrentCardValue()).ToArray();
        
        var cmd = new TuringCommand(tm, inputs, result => {
            lampLight.color = result ? Color.green : Color.red;
            if (!(result ? trueOutput : falseOutput).PropagateSignal()) scheduler.EndProgram();
        });
        
        scheduler.Enqueue(cmd);
    }

    public TuringOperation[] GetCardValues()
    {
        List<int> inputs = cardSlots.Select(x => x.GetCurrentCardValue()).ToList();
        int inputCount = inputs.Count;
        
        if(inputCount == 0) return null;
        while (inputCount % 3 != 0)
        {
            inputs.Add(0);
            inputCount++;
        }
        
        TuringOperation[] operations = new TuringOperation[inputCount];
        for (int i = 0; i < inputCount / 3; i++)
        {
            int baseIndex = i * 3;
            operations[i] = new TuringOperation(SymbolData.IntToSymbolType(inputs[baseIndex]),
                                                SymbolData.IntToSymbolType(inputs[baseIndex + 1]),
                                                inputs[baseIndex + 2]);
        }
        return operations;
    }
    
}