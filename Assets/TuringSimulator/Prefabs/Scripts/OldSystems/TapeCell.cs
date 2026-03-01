using UnityEngine;

namespace MyLibrary.Scripts.OldSystems
{
    public class TapeCell : MonoBehaviour
    {
        public Transform symbolSpawnPoint;
        private GameObject _currentSymbolObject;
        private SymbolType _currentSymbol = SymbolType.Blank;
        public Renderer highlightRenderer;

        public void SetSymbol(SymbolType symbol, GameObject prefab)
        {
            if (symbol == SymbolType.None) return;
            ClearSymbol();

            if (symbol == SymbolType.Blank || prefab == null)
            {
                _currentSymbol = SymbolType.Blank;
                return;
            }

            _currentSymbolObject =
                Instantiate(prefab, symbolSpawnPoint.position, Quaternion.identity, symbolSpawnPoint);
            _currentSymbol = symbol;
        }

        public SymbolType GetSymbol()
        {
            return _currentSymbol;
        }

        public void ClearSymbol()
        {
            if (_currentSymbolObject != null)
                Destroy(_currentSymbolObject);

            _currentSymbolObject = null;
            _currentSymbol = SymbolType.Blank;
        }

        public void SetHighlight(bool highlight)
        {
            if (highlightRenderer != null)
                highlightRenderer.material.color = highlight ? Color.green : Color.red;
        }
    }
}