using System.Collections;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.View.Machine.Tape
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class TapeStepFeedback : MonoBehaviour, ITapeStepFeedback
    {
        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        [SerializeField]
        [Tooltip("Positive cue on read when the cell symbol equals the symbol that will be written.")]
        private AudioClip _readMatchClip;

        [SerializeField]
        [Tooltip("Negative cue on read when the cell symbol differs from the symbol that will be written.")]
        private AudioClip _readMismatchClip;

        [SerializeField, Min(0f)]
        [Tooltip("How long playback waits on the read beat so the cue can be heard.")]
        private float _readHoldSeconds = 0.4f;

        [Header("Particles")]
        [SerializeField]
        [Tooltip("Played at the head cell when a physical symbol is written or replaced.")]
        private ParticleSystem _writeParticles;

        [SerializeField]
        [Tooltip("Played at the head cell when a physical symbol is removed (written blank).")]
        private ParticleSystem _deleteParticles;

        [SerializeField, Min(0f)]
        [Tooltip("How long playback waits on write/delete so the particles can be seen.")]
        private float _writeEffectSeconds = 0.4f;

        private void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_audioSource != null)
                _audioSource.playOnAwake = false;
        }

        public IEnumerator PlayRead(Symbol readSymbol, Symbol writeSymbol, Vector3 worldPosition)
        {
            _ = worldPosition;
            var clip = TapeStepFeedbackRules.IsReadMatch(readSymbol, writeSymbol)
                ? _readMatchClip
                : _readMismatchClip;
            PlayClip(clip);

            if (_readHoldSeconds <= 0f)
                yield break;

            yield return new WaitForSeconds(_readHoldSeconds);
        }

        public IEnumerator PlayWrite(TapeWriteEffectKind kind, Vector3 worldPosition)
        {
            if (kind == TapeWriteEffectKind.None)
                yield break;

            var source = kind == TapeWriteEffectKind.Delete ? _deleteParticles : _writeParticles;
            PlayParticles(source, worldPosition);

            if (_writeEffectSeconds <= 0f)
                yield break;

            yield return new WaitForSeconds(_writeEffectSeconds);
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource == null || clip == null)
                return;

            _audioSource.PlayOneShot(clip);
        }

        private void PlayParticles(ParticleSystem source, Vector3 worldPosition)
        {
            if (source == null)
                return;

            if (source.gameObject == gameObject)
            {
                source.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                source.Play(true);
                return;
            }

            var instance = Instantiate(source, worldPosition, source.transform.rotation);
            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.Play(true);

            var main = instance.main;
            var lifetime = main.duration + main.startLifetime.constantMax;
            Destroy(instance.gameObject, Mathf.Max(_writeEffectSeconds, lifetime) + 0.1f);
        }

        private void OnValidate()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_writeParticles != null && _writeParticles.gameObject == gameObject)
                Debug.LogWarning($"{name}: assign Write Particles on a dedicated child or prefab, not the Tape root.", this);
            if (_deleteParticles != null && _deleteParticles.gameObject == gameObject)
                Debug.LogWarning($"{name}: assign Delete Particles on a dedicated child or prefab, not the Tape root.", this);
        }
    }
}
