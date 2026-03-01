using System.Collections.Generic;
using MyLibrary.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

public class ModuleFactory : MonoBehaviour
{
    [SerializeField] private List<GameObject> modulePrefabs; // Reference to the module prefab
    private List<GameObject> _moduleInstances; // List to store created modules
    
    public CommandScheduler commandScheduler;
    [FormerlySerializedAs("turingMachine")] public TM tm;

    void Awake()
    {
        _moduleInstances = new List<GameObject>();
    }

    // Method to create and add a new module
    public GameObject CreateModule(int numberOfTapes)
    {
        GameObject newModule = Instantiate(modulePrefabs[numberOfTapes - 1], transform.position, Quaternion.identity);
        Module mod = newModule.GetComponent<Module>();
        mod.tm = tm;
        mod.scheduler = commandScheduler;
        _moduleInstances.Add(newModule);
        
        return newModule;
    }
}
