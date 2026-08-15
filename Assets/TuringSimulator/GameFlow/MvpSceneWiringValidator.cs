using System.Collections.Generic;
using TuringSimulator.Controller;
using TuringSimulator.Core.Level;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    /// <summary>
    /// Inspector-visible checklist for the editor-first MVP scene setup.
    /// </summary>
    public sealed class MvpSceneWiringValidator : MonoBehaviour, IMvpSceneWiringValidator
    {
        [Header("Core scene wiring")]
        [SerializeField] TuringBootstrap bootstrap;
        [SerializeField] LevelDatabase levelDatabase;
        [SerializeField] ProgramWorkbench programWorkbench;
        [SerializeField] CardDrawerBehaviour cardDrawer;
        [SerializeField] BlockDrawerBehaviour blockDrawer;

        [Header("Tutor wiring")]
        [SerializeField] ITSClient itsClient;
        [SerializeField] SkillTracker skillTracker;
        [SerializeField] AgentDialogue agentDialogue;
        [SerializeField] AgentActionMapper agentActionMapper;
        [SerializeField] AgentActionExecutor agentActionExecutor;
        [SerializeField] AgentVoiceFeedbackListener agentVoiceFeedbackListener;
        [SerializeField] AgentAnimator agentAnimator;
        [SerializeField] VoiceInputHandler voiceInputHandler;
        [SerializeField] VoiceAskControllerInput voiceAskControllerInput;
        [SerializeField] EventChannelWiringValidator eventChannelWiringValidator;

        [Header("Validation")]
        [SerializeField] bool logWarningsOnStart = true;
        [SerializeField] int requiredScenarioCount = 40;

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
            Require(blockDrawer, nameof(blockDrawer), issues);
            Require(itsClient, nameof(itsClient), issues);
            Require(skillTracker, nameof(skillTracker), issues);
            Require(agentDialogue, nameof(agentDialogue), issues);
            Require(agentActionMapper, nameof(agentActionMapper), issues);
            Require(agentActionExecutor, nameof(agentActionExecutor), issues);
            Require(agentVoiceFeedbackListener, nameof(agentVoiceFeedbackListener), issues);
            Require(agentAnimator, nameof(agentAnimator), issues);
            Require(voiceInputHandler, nameof(voiceInputHandler), issues);
            Require(voiceAskControllerInput, nameof(voiceAskControllerInput), issues);
            Require(eventChannelWiringValidator, nameof(eventChannelWiringValidator), issues);

            if (levelDatabase != null &&
                levelDatabase.ValidationScenarioCount < requiredScenarioCount)
            {
                issues.Add(
                    $"LevelDatabase has {levelDatabase.ValidationScenarioCount} validation " +
                    $"scenarios; the MVP target is {requiredScenarioCount}.");
            }

            if (cardDrawer != null)
            {
                if (cardDrawer.SymbolCardPrefab == null)
                    issues.Add("CardDrawer symbolCardPrefab is not assigned.");
                if (cardDrawer.DirectionCardPrefab == null)
                    issues.Add("CardDrawer directionCardPrefab is not assigned.");
            }

            if (blockDrawer != null)
            {
                if (blockDrawer.MoveBlockPrefab == null)
                    issues.Add("BlockDrawer moveBlockPrefab is not assigned.");
                if (blockDrawer.WriteBlockPrefab == null)
                    issues.Add("BlockDrawer writeBlockPrefab is not assigned.");
                if (blockDrawer.ConditionBlockPrefab == null)
                    issues.Add("BlockDrawer conditionBlockPrefab is not assigned.");
                if (blockDrawer.AcceptBlockPrefab == null)
                    issues.Add("BlockDrawer acceptBlockPrefab is not assigned.");
                if (blockDrawer.RejectBlockPrefab == null)
                    issues.Add("BlockDrawer rejectBlockPrefab is not assigned.");
            }

            if (programWorkbench != null && !programWorkbench.HasStartOutputPortAssigned)
            {
                issues.Add("ProgramWorkbench startOutputPort is not assigned. Connect a start/power output port.");
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
