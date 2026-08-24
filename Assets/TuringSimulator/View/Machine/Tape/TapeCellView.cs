using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    public class TapeCellView : MonoBehaviour, ITapeCellView
    {
        [Header("Symbols")]
        [SerializeField] private TapeSymbolPrefabs symbolPrefabs;
        [SerializeField, Tooltip("Where symbol prefabs spawn. Defaults to this cell.")]
        private Transform instanceRoot;

        private GameObject _instance;

        public void SetSymbol(Symbol symbol)
        {
            if (symbolPrefabs == null)
                throw new System.InvalidOperationException(
                    $"{nameof(TapeCellView)} on '{name}' is missing {nameof(TapeSymbolPrefabs)}.");

            var prefab = TapeCellSymbolBinding.ResolvePrefab(symbol, symbolPrefabs);
            DestroyCurrentInstance();

            if (prefab == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var parent = TapeCellSymbolBinding.ResolveInstanceParent(transform, instanceRoot);
            _instance = Instantiate(prefab, parent);
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
        }

        private void OnValidate()
        {
            if (symbolPrefabs == null)
                Debug.LogWarning($"{name}: missing {nameof(TapeSymbolPrefabs)}.", this);
            if (instanceRoot != null && !instanceRoot.IsChildOf(transform))
                Debug.LogWarning(
                    $"{name}: {nameof(instanceRoot)} must be this cell or a child of it. Symbols will spawn on the cell.",
                    this);
        }

        private void OnDestroy()
        {
            DestroyCurrentInstance();
        }

        private void DestroyCurrentInstance()
        {
            if (_instance == null)
                return;

            if (Application.isPlaying)
                Destroy(_instance);
            else
                DestroyImmediate(_instance);

            _instance = null;
        }
    }
}
