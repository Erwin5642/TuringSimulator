using System.Collections;
using TuringSimulator.Core.Types;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TuringSimulator.View.Machine.Tape
{
    [RequireComponent(typeof(ConveyorTapeVisual))]
    public sealed class TapeDebugHotkeys : MonoBehaviour, ITapeDebugHotkeys
    {
        [Header("Enable")]
        [SerializeField] private bool enableInEditor = true;
        [SerializeField] private bool enableInDevelopmentBuilds;

        [Header("References")]
        [SerializeField]
        [Tooltip("Scene object implementing ITapeVisual. Assign the ConveyorTapeVisual on this Tape.")]
        private MonoBehaviour tapeVisual;

        private ITapeVisual _tape;
        private bool _awaitingWrite;
        private bool _busy;
        private string _overlayStatus = string.Empty;
        private float _overlayUntil;

        public bool IsAwaitingWriteSymbol => _awaitingWrite;

        private bool IsHotkeyEnabled =>
            (enableInEditor && Application.isEditor) ||
            (enableInDevelopmentBuilds && Debug.isDebugBuild);

        private void Awake()
        {
            _tape = tapeVisual as ITapeVisual;
        }

        private void Update()
        {
            if (!IsHotkeyEnabled || _busy)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var mapped = ReadPressedKey(keyboard);
            if (mapped == TapeDebugKey.None)
                return;

            if (_tape == null)
            {
                ShowOverlay("Tape debug: ITapeVisual is not assigned.");
                return;
            }

            var outcome = TapeDebugHotkeyMapping.Reduce(_awaitingWrite, mapped);
            _awaitingWrite = outcome.AwaitingWrite;

            if (outcome.Move.HasValue)
            {
                StartCoroutine(RunTape(_tape.MoveHead(outcome.Move.Value)));
                ShowOverlay($"Tape debug: move {outcome.Move.Value}");
                return;
            }

            if (outcome.Write.HasValue)
            {
                StartCoroutine(RunTape(_tape.ShowWrite(outcome.Write.Value)));
                ShowOverlay($"Tape debug: write {Describe(outcome.Write.Value)}");
                return;
            }

            if (_awaitingWrite)
                ShowOverlay("Tape debug: press 0 blank, 1 gear, 2 nut, 3 screw (W/Esc cancel)");
            else if (mapped == TapeDebugKey.Cancel || mapped == TapeDebugKey.ArmWrite)
                ShowOverlay("Tape debug: write cancelled");
        }

        private void OnGUI()
        {
            if (!IsHotkeyEnabled)
                return;

            var show = _awaitingWrite || Time.unscaledTime <= _overlayUntil;
            if (!show)
                return;

            const int width = 560;
            const int height = 48;
            GUI.Box(new Rect(12, 140, width, height), GUIContent.none);
            GUI.Label(new Rect(20, 148, width - 16, height - 16), OverlayText());
        }

        private void OnValidate()
        {
            if (tapeVisual == null)
                Debug.LogWarning($"{name}: missing ITapeVisual reference.", this);
            else if (tapeVisual is not ITapeVisual)
                Debug.LogWarning($"{name}: tapeVisual does not implement ITapeVisual.", this);
        }

        private IEnumerator RunTape(IEnumerator routine)
        {
            if (_tape == null)
            {
                ShowOverlay("Tape debug: ITapeVisual is not assigned.");
                yield break;
            }

            _busy = true;
            yield return routine;
            _busy = false;
        }

        private static TapeDebugKey ReadPressedKey(Keyboard keyboard)
        {
            if (keyboard[Key.LeftArrow].wasPressedThisFrame)
                return TapeDebugKey.MoveLeft;
            if (keyboard[Key.RightArrow].wasPressedThisFrame)
                return TapeDebugKey.MoveRight;
            if (keyboard[Key.W].wasPressedThisFrame)
                return TapeDebugKey.ArmWrite;
            if (keyboard[Key.Escape].wasPressedThisFrame)
                return TapeDebugKey.Cancel;
            if (keyboard[Key.Digit0].wasPressedThisFrame || keyboard[Key.Numpad0].wasPressedThisFrame)
                return TapeDebugKey.WriteBlank;
            if (keyboard[Key.Digit1].wasPressedThisFrame || keyboard[Key.Numpad1].wasPressedThisFrame)
                return TapeDebugKey.WriteGear;
            if (keyboard[Key.Digit2].wasPressedThisFrame || keyboard[Key.Numpad2].wasPressedThisFrame)
                return TapeDebugKey.WriteNut;
            if (keyboard[Key.Digit3].wasPressedThisFrame || keyboard[Key.Numpad3].wasPressedThisFrame)
                return TapeDebugKey.WriteScrew;
            return TapeDebugKey.None;
        }

        private static string Describe(Symbol symbol) => symbol switch
        {
            Symbol.Blank => "blank (0)",
            Symbol.Gear => "gear (1)",
            Symbol.Nut => "nut (2)",
            Symbol.Screw => "screw (3)",
            _ => symbol.ToString()
        };

        private void ShowOverlay(string status)
        {
            _overlayStatus = status ?? string.Empty;
            _overlayUntil = Time.unscaledTime + 4f;
        }

        private string OverlayText()
        {
            const string hint = "Arrows: move tape    W then 0/1/2/3: write blank/gear/nut/screw";
            if (_awaitingWrite)
                return "Write: 0 blank, 1 gear, 2 nut, 3 screw    W/Esc cancel";
            return string.IsNullOrEmpty(_overlayStatus) ? hint : _overlayStatus;
        }
    }
}
