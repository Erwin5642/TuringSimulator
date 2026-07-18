using System.Collections.Generic;
using TuringSimulator.Controller;
using TuringSimulator.Core.Level;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Inspector-visible checklist for the editor-first MVP scene setup.
    /// Attach this to the Systems root and use Validate Scene from the context menu.
    /// </summary>
    public sealed class MvpSceneWiringValidator : MonoBehaviour, IMvpSceneWiringValidator
    {
        [Header("Core scene wiring")]
        [SerializeField] TuringBootstrap bootstrap;
        [SerializeField] LevelDatabase levelDatabase;
        [SerializeField] ProgramWorkbench programWorkbench;
        [SerializeField] CardDrawerBehaviour cardDrawer;

        [Header("Tutor wiring")]
        [SerializeField] ITSClient itsClient;
        [SerializeField] SkillTracker skillTracker;
        [SerializeField] AgentDialogue agentDialogue;
        [SerializeField] LiveTutorSocket liveTutorSocket;

        [Header("Validation")]
        [SerializeField] bool logWarningsOnStart = true;
        [SerializeField] int requiredScenarioCount = 10;

        void Start()
        {
            if (logWarningsOnStart)
                LogValidation();
        }

        [ContextMenu("Validate Scene")]
        public void LogValidation()
        {
            var issues = ValidateScene();
            if (issues.Count == 0)
            {
                Debug.Log("[MVP Wiring] Scene wiring is complete.", this);
                return;
            }

            foreach (var issue in issues)
                Debug.LogWarning($"[MVP Wiring] {issue}", this);
        }

        public IReadOnlyList<string> ValidateScene()
        {
            var issues = new List<string>();

            Require(bootstrap, nameof(bootstrap), issues);
            Require(levelDatabase, nameof(levelDatabase), issues);
            Require(programWorkbench, nameof(programWorkbench), issues);
            Require(cardDrawer, nameof(cardDrawer), issues);
            Require(itsClient, nameof(itsClient), issues);
            Require(skillTracker, nameof(skillTracker), issues);
            Require(agentDialogue, nameof(agentDialogue), issues);

            if (liveTutorSocket == null)
                issues.Add("LiveTutorSocket is not assigned; live advisory telemetry is disabled.");

            if (levelDatabase != null &&
                levelDatabase.ValidationScenarioCount < requiredScenarioCount)
            {
                issues.Add(
                    $"LevelDatabase has {levelDatabase.ValidationScenarioCount} validation "
                    $"scenarios; the MVP target is {requiredScenarioCount}.");
            }

            if (cardDrawer != null)
            {
                if (cardDrawer.SymbolCardPrefab == null)
                    issues.Add("CardDrawer symbolCardPrefab is not assigned.");
                if (cardDrawer.DirectionCardPrefab == null)
                    issues.Add("CardDrawer directionCardPrefab is not assigned.");
            }

            return issues;
        }

        static void Require(Object value, string fieldName, ICollection<string> issues)
        {
            if (value == null)
                issues.Add($"{fieldName} is not assigned in the Inspector.");
        }
    }
}
