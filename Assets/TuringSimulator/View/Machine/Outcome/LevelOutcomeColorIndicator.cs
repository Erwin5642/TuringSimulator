using System.Collections;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.View.Machine.Outcome
{
    public sealed class LevelOutcomeColorIndicator : MonoBehaviour, ILevelOutcomeIndicator
    {
        [Header("Event Channel")]
        [SerializeField] private LevelOutcomeEventChannel _levelOutcomeChannel;

        [Header("Visual")]
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField, Min(0f)] private float _transitionDuration = 0.3f;
        [SerializeField] private Color _idleColor = Color.gray;
        [SerializeField] private Color _victoryColor = Color.green;
        [SerializeField] private Color _defeatColor = Color.red;

        Material _targetMaterial;
        Coroutine _transitionRoutine;

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            if (_levelOutcomeChannel != null)
                _levelOutcomeChannel.OnRaised += HandleLevelOutcome;
        }

        void OnDisable()
        {
            if (_levelOutcomeChannel != null)
                _levelOutcomeChannel.OnRaised -= HandleLevelOutcome;
        }

        public void Initialize()
        {
            if (_targetRenderer == null)
            {
                Debug.LogWarning("[LevelOutcomeColorIndicator] Target renderer is not assigned.", this);
                return;
            }

            _targetMaterial = _targetRenderer.material;
            Reset();
        }

        public void Reset()
        {
            if (_targetMaterial == null)
                return;

            StopTransition();
            _targetMaterial.color = _idleColor;
        }

        public IEnumerator Show(LevelOutcomeKind outcome)
        {
            if (_targetMaterial == null)
                yield break;

            var startColor = _targetMaterial.color;
            var targetColor = GetColor(outcome);

            var elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                _targetMaterial.color = Color.Lerp(
                    startColor,
                    targetColor,
                    elapsed / _transitionDuration);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _targetMaterial.color = targetColor;
        }

        void HandleLevelOutcome(LevelOutcomeEventData eventData)
        {
            StopTransition();
            _transitionRoutine = StartCoroutine(Show(eventData.Outcome));
        }

        void StopTransition()
        {
            if (_transitionRoutine == null)
                return;

            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        Color GetColor(LevelOutcomeKind outcome)
        {
            return outcome switch
            {
                LevelOutcomeKind.Victory => _victoryColor,
                LevelOutcomeKind.Defeat => _defeatColor,
                _ => _idleColor,
            };
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_targetRenderer == null)
                _targetRenderer = GetComponent<Renderer>();
        }
#endif
    }
}
