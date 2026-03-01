using System;
using System.Collections;
using System.Collections.Generic;
using MyLibrary.Scripts.OldSystems;
using UnityEngine;

public class Tape : MonoBehaviour
{
    public GameObject tapeCellPrefab;
    public float spacing = 1.5f;
    public int size = 11;
    private Vector3 _initialPosition;
    
    public GameObject gearPrefab;
    public GameObject nutPrefab;
    public GameObject boltPrefab;

    private List<TapeCell> _cells = new List<TapeCell>();
    private int _headPosition;

    private void Awake()
    {
        GenerateTape();
        HighlightHead();
        _initialPosition = transform.position;
    }

    void GenerateTape()
    {
        int center = size / 2;

        for (int i = 0; i < size; i++)
        {
            Vector3 pos = transform.position + Vector3.right * (i - center) * spacing;
            GameObject cellGo = Instantiate(tapeCellPrefab, pos, Quaternion.identity, transform);
            TapeCell cell = cellGo.GetComponent<TapeCell>();
            _cells.Add(cell);
        }

        _headPosition = center;
    }

    public void ResetPosition()
    {
        transform.position = _initialPosition;
        _headPosition = size / 2;
    }

    public void ClearTape()
    {
        int center = size / 2;

        for (int i = 0; i < size; i++)
        {
            _cells[i].ClearSymbol();
        }
        transform.position = _initialPosition;
        _headPosition = center;
        HighlightHead();
    }
    
    public SymbolType Read()
    {
        return _cells[_headPosition].GetSymbol();
    }

    public void Write(SymbolType symbol)
    {
        if(symbol == SymbolType.None) return;
        GameObject prefab = GetPrefab(symbol);
        _cells[_headPosition].SetSymbol(symbol, prefab);
    }
    
    public IEnumerator Move(int direction)
    {
        if (direction == 0)
        {
            yield break;
        }
        
        int newPos = _headPosition + direction;

        // Safety check
        if (newPos < 0 || newPos >= _cells.Count)
        {
            Debug.LogWarning("Head out of bounds");
            yield break;
        }

        // Get world positions for interpolation
        Vector3 start = transform.position;
        Vector3 end   = start + (_cells[_headPosition].transform.position - _cells[newPos].transform.position);

        float duration = 2.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);  // Moves the whole tape in the opposite direction
            yield return null;
        }

        transform.position = end;
        _headPosition = newPos;

        HighlightHead();
    }
    
    public IEnumerator MoveAndNotify(int direction, Action onComplete)
    {
         yield return Move(direction);
         onComplete?.Invoke();
    }
    
    void HighlightHead()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].SetHighlight(i == _headPosition);
        }
    }

    private GameObject GetPrefab(SymbolType symbol)
    {
        return symbol switch
        {
            SymbolType.Gear => gearPrefab,
            SymbolType.Nut => nutPrefab,
            SymbolType.Bolt => boltPrefab,
            _ => null
        };
    }
}