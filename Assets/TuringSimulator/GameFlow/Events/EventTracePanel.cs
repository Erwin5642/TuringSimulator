using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TuringSimulator.GameFlow.Events
{
    /// <summary>
    /// Displays recent entries from <see cref="EventTraceLog"/> on a world-space or screen UI panel.
    /// </summary>
    public sealed class EventTracePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _logText;
        [Tooltip("Optional status line showing entry count and last sequence.")]
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Refresh")]
        [SerializeField] private float _refreshIntervalSeconds = 0.25f;
        [SerializeField] private int _maxVisibleLines = 24;
        [SerializeField] private int _maxPayloadLength = 240;
        [SerializeField] private string _eventNameFilter = string.Empty;

        [Header("Startup")]
        [SerializeField] private bool _visibleOnStart = true;
        [SerializeField] private bool _autoCreateUiIfMissing = true;

        [Header("Auto-Create Layout")]
        [SerializeField] private Vector3 _autoCreateLocalPosition = new Vector3(0f, 1.5f, 1.8f);
        [SerializeField] private Vector3 _autoCreateLocalScale = new Vector3(0.002f, 0.002f, 0.002f);
        [SerializeField] private Vector2 _autoCreateCanvasSize = new Vector2(900f, 520f);

        readonly StringBuilder _builder = new StringBuilder(4096);
        int _lastSnapshotCount = -1;
        long _lastSnapshotMaxSequence = -1;
        Coroutine _refreshRoutine;

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        void Awake()
        {
            if (_autoCreateUiIfMissing && (_panelRoot == null || _logText == null))
                EnsureUiExists();

            SetVisible(_visibleOnStart);
        }

        void OnEnable()
        {
            ResetTracking();
            RefreshDisplay(force: true);
            _refreshRoutine = StartCoroutine(RefreshLoop());
        }

        void OnDisable()
        {
            if (_refreshRoutine != null)
            {
                StopCoroutine(_refreshRoutine);
                _refreshRoutine = null;
            }
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot == null)
                return;

            _panelRoot.SetActive(visible);
        }

        public void ToggleVisible()
        {
            SetVisible(!IsVisible);
        }

        [ContextMenu("Clear Event Trace")]
        public void ClearLog()
        {
            EventTraceLog.Clear();
            ResetTracking();
            RefreshDisplay(force: true);
        }

        IEnumerator RefreshLoop()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.05f, _refreshIntervalSeconds));
            while (true)
            {
                yield return wait;
                RefreshDisplay(force: false);
            }
        }

        void RefreshDisplay(bool force)
        {
            if (_logText == null)
                return;

            var snapshot = EventTraceLog.Snapshot();
            if (!force && !HasSnapshotChanged(snapshot))
                return;

            _builder.Clear();

            if (snapshot.Count == 0)
            {
                _builder.AppendLine("No events recorded yet.");
            }
            else
            {
                var filter = string.IsNullOrWhiteSpace(_eventNameFilter)
                    ? null
                    : _eventNameFilter.Trim();
                var visibleCount = 0;

                for (var i = snapshot.Count - 1; i >= 0 && visibleCount < _maxVisibleLines; i--)
                {
                    var entry = snapshot[i];
                    if (filter != null &&
                        entry.EventName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    AppendEntryLine(entry);
                    visibleCount++;
                }

                if (visibleCount == 0)
                    _builder.AppendLine($"No events match filter \"{filter}\".");
            }

            _logText.text = _builder.ToString();

            if (_statusText != null)
            {
                if (snapshot.Count == 0)
                {
                    _statusText.text = "Event trace: empty";
                }
                else
                {
                    var last = snapshot[snapshot.Count - 1];
                    _statusText.text = $"Event trace: {snapshot.Count} buffered | last #{last.Sequence}";
                }
            }

            _lastSnapshotCount = snapshot.Count;
            _lastSnapshotMaxSequence = snapshot.Count == 0 ? -1 : snapshot[snapshot.Count - 1].Sequence;
        }

        bool HasSnapshotChanged(IReadOnlyList<EventTraceEntry> snapshot)
        {
            if (snapshot.Count == 0)
                return _lastSnapshotCount != 0;

            var maxSequence = snapshot[snapshot.Count - 1].Sequence;
            return snapshot.Count != _lastSnapshotCount || maxSequence != _lastSnapshotMaxSequence;
        }

        void AppendEntryLine(EventTraceEntry entry)
        {
            var payload = entry.PayloadSummary ?? string.Empty;
            if (_maxPayloadLength > 0 && payload.Length > _maxPayloadLength)
                payload = payload.Substring(0, _maxPayloadLength) + "...";

            _builder.Append('#');
            _builder.Append(entry.Sequence);
            _builder.Append(' ');
            _builder.Append(entry.EventName);
            _builder.Append(" | ");
            _builder.Append(entry.SourceName);
            _builder.Append(" | ");
            _builder.AppendLine(payload);
        }

        void ResetTracking()
        {
            _lastSnapshotCount = -1;
            _lastSnapshotMaxSequence = -1;
        }

        void EnsureUiExists()
        {
            var canvasGo = new GameObject("EventTraceCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = _autoCreateCanvasSize;
            canvasRect.localPosition = _autoCreateLocalPosition;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = _autoCreateLocalScale;

            var backgroundGo = new GameObject("Background");
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var background = backgroundGo.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);

            var backgroundRect = backgroundGo.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(canvasGo.transform, false);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.fontSize = 22f;
            statusTmp.alignment = TextAlignmentOptions.TopLeft;
            statusTmp.color = new Color(0.75f, 0.9f, 1f, 1f);
            statusTmp.margin = new Vector4(16f, 12f, 16f, 0f);

            var statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.sizeDelta = new Vector2(0f, 40f);
            statusRect.anchoredPosition = Vector2.zero;

            var logGo = new GameObject("LogText");
            logGo.transform.SetParent(canvasGo.transform, false);
            var logTmp = logGo.AddComponent<TextMeshProUGUI>();
            logTmp.fontSize = 20f;
            logTmp.alignment = TextAlignmentOptions.TopLeft;
            logTmp.color = Color.white;
            logTmp.textWrappingMode = TextWrappingModes.Normal;
            logTmp.overflowMode = TextOverflowModes.Truncate;
            logTmp.margin = new Vector4(16f, 8f, 16f, 16f);

            var logRect = logGo.GetComponent<RectTransform>();
            logRect.anchorMin = Vector2.zero;
            logRect.anchorMax = Vector2.one;
            logRect.offsetMin = new Vector2(0f, 16f);
            logRect.offsetMax = new Vector2(0f, -48f);

            _panelRoot = canvasGo;
            _logText = logTmp;
            _statusText = statusTmp;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_refreshIntervalSeconds < 0.05f)
                _refreshIntervalSeconds = 0.05f;

            if (_maxVisibleLines < 1)
                _maxVisibleLines = 1;
        }
#endif
    }
}
