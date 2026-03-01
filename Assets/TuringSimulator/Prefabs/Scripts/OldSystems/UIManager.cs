using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Text Displays")]
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI expectedOutputText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI specialText;
    public Button nextLevelButton;
    
    // Dictionary to map SymbolType to Portuguese names
    private Dictionary<SymbolType, string> symbolTranslations = new Dictionary<SymbolType, string>()
    {
        { SymbolType.Gear, "Engrenagem" },
        { SymbolType.Nut, "Porca" },  
        { SymbolType.Bolt, "Parafuso" },
        { SymbolType.Blank, "Nada"}
    };
    
    // Called when the level loads
    public void ShowPrompt(string prompt)
    {
        promptText.text = prompt;
    }
    
    public void ShowExpectedOutput(SymbolType[] output)
    {
        expectedOutputText.text = "Esperado:\n" + SymbolsToString(output);
    }

    public void ShowResult(bool success, string message)
    {
        resultText.text = message;
        resultText.color = success ? Color.green: Color.red;
    }

    public void ShowEndOfGame()
    {
        resultText.text = "Todos os desafios foram concluídos!";
        resultText.color = Color.cyan;
    }
    
    public void OnStartPressed()
    {
        specialText.text = "Programa em execução";
    }

    public void OnPausePressed()
    {
        specialText.text = "Pressione novamente para continuar";
    }

    public void OnResumePressed()
    {
        specialText.text = "Programa em execução";
    }

    public void OnHaltPressed()
    {
        specialText.text = "Programa abortado";
    }

    public void OnResetPressed()
    {
        ClearAll();
    }
    
    public void OnProgramEnded()
    {
        specialText.text = "Programa terminado";
    }

    private string SymbolsToString(SymbolType[] symbols)
    {
        if (symbols == null || symbols.Length == 0)
            return "[Vazio]";
        
        string s = "";
        foreach (var sym in symbols)
        {
            if (symbolTranslations.ContainsKey(sym))
            {
                s += symbolTranslations[sym] + " ";
            }
            else
            {
                s += sym.ToString() + " ";
            }
        }

        return s.Trim();
    }
    
    private void ClearAll()
    {
        resultText.text = "";
        specialText.text = "";
        expectedOutputText.text = "";
        promptText.text = "";
    }
}
