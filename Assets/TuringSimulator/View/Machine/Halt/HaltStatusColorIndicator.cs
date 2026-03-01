using System.Collections;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Halt
{
    public class HaltStatusColorIndicator : MonoBehaviour, IHaltStatusIndicator
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Min(0f)] private float transitionDuration = 0.3f;

        private Material _targetMaterial;

        public void Initialize()
        {
            _targetMaterial = targetRenderer.material;
            Reset();
        }

        public void Reset()
        {
            _targetMaterial.color = Color.gray;
        }

        public IEnumerator Show(HaltStatus status)
        {
            var startColor = _targetMaterial.color;
            var targetColor = GetColor(status);

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                _targetMaterial.color =
                    Color.Lerp(startColor, targetColor, elapsed / transitionDuration);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _targetMaterial.color = targetColor;
        }

        private static Color GetColor(HaltStatus status)
        {
            return status switch
            {
                HaltStatus.Accept => Color.green,
                HaltStatus.Reject => Color.red,
                HaltStatus.StepLimitExceeded => Color.blue,
                _ => Color.gray
            };
        }
    }
}