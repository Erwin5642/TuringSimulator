using TMPro;
using TuringSimulator.Controller.Syncronizer;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Shows the action the wired machine control will perform next.
    /// Start/Abort: Começar while idle, Recomeçar while a run is active.
    /// Pause/Resume: Pausar while playing, Rodar while paused or idle.
    /// </summary>
    public sealed class MachineControlButtonLabel : MonoBehaviour
    {
        public enum LabelKind
        {
            StartOrAbort = 0,
            PauseOrResume = 1,
        }

        const string StartLabel = "Começar";
        const string RestartLabel = "Recomeçar";
        const string PauseLabel = "Pausar";
        const string PlayLabel = "Rodar";

        [SerializeField] private LabelKind _kind = LabelKind.StartOrAbort;
        [SerializeField] private TMP_Text _label;

        void Awake()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            GameStateMachine.Instance.OnStateChanged += HandleStateChanged;
            PlaybackController.PlayingChanged += HandlePlayingChanged;
            Refresh();
        }

        void OnDisable()
        {
            GameStateMachine.Instance.OnStateChanged -= HandleStateChanged;
            PlaybackController.PlayingChanged -= HandlePlayingChanged;
        }

        void HandleStateChanged(GameState _, GameState __) => Refresh();

        void HandlePlayingChanged(bool _) => Refresh();

        void Refresh()
        {
            if (_label == null)
                return;

            _label.text = _kind == LabelKind.StartOrAbort
                ? StartOrAbortText()
                : PauseOrResumeText();
        }

        static string StartOrAbortText()
        {
            return GameStateMachine.Instance.CurrentState == GameState.Running
                ? RestartLabel
                : StartLabel;
        }

        static string PauseOrResumeText()
        {
            return PlaybackController.PlayRequested ? PauseLabel : PlayLabel;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
            if (_label == null)
                Debug.LogWarning($"{name}: assign a TMP_Text for the machine control label.", this);
        }
#endif
    }
}
