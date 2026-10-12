using System;
using MageDuel.Battle.Presentation;
using MageDuel.Core;
using MageDuel.Networking;
using MageDuel.Networking.Contracts;
using UnityEngine;

namespace MageDuel.Training
{
    public sealed class TrainingSceneController : MonoBehaviour
    {
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private TutorialOverlayView _overlay;
        [SerializeField] private TrainingLessonPanelView _lessonPanel;
        [SerializeField] private TrainingPausePanelView _pausePanel;
        [SerializeField] private BattleMessagePresenter _battleMessagePresenter;
        [SerializeField] private TrainingLessonConfiguration _lessonConfiguration = new();

        private NetworkSession _session;
        private bool _exitPending;
        private bool _exitDialogPending;
        private bool _battleConcluded;

        private void Awake()
        {
            _session = NetworkSession.Instance;
            _overlay?.Initialize(
                _lessonPanel,
                ContinueTutorial,
                CompleteGuidedTraining);

            if (_pausePanel != null)
            {
                _pausePanel.Initialize(
                    CancelExitConfirmation,
                    ConfirmExitTraining);
            }
            else
            {
                Debug.LogError(
                    "Training pause panel prefab is not assigned.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_session == null)
                return;
            _session.TutorialStateChanged += HandleTutorialState;
            _session.TrainingExited += HandleTrainingExited;
            _session.BattleEnded += HandleBattleEnded;
            if (_battleMessagePresenter != null)
            {
                _battleMessagePresenter.TrainingResultExitRequested +=
                    ExitCompletedTraining;
            }
        }

        private async void Start()
        {
            if (_session?.CurrentBattleAssignment == null ||
                !_session.CurrentBattleAssignment.IsTraining)
            {
                _overlay?.BeginSceneEntrance(null);
                return;
            }

            try
            {
                await _session.SetTrainingPausedAsync(true);
                if (_session.CurrentBattleAssignment.Mode ==
                    TrainingModes.GuidedLesson)
                {
                    await _session.ConfigureTrainingAsync(
                        _lessonConfiguration.CreatePayload());
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to prepare training presentation: {exception.Message}",
                    this);
            }

            if (_overlay != null)
                _overlay.BeginSceneEntrance(ResumeAfterEntrance);
            else
                ResumeAfterEntrance();
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(KeyCode.Escape) || _exitPending)
                return;

            if (_pausePanel != null && _pausePanel.IsVisible)
                CancelExitConfirmation();
            else if (_overlay == null || !_overlay.IsTransitioning)
                RequestExitConfirmation();
        }

        private void OnDisable()
        {
            if (_session == null)
                return;
            _session.TutorialStateChanged -= HandleTutorialState;
            _session.TrainingExited -= HandleTrainingExited;
            _session.BattleEnded -= HandleBattleEnded;
            if (_battleMessagePresenter != null)
            {
                _battleMessagePresenter.TrainingResultExitRequested -=
                    ExitCompletedTraining;
            }
        }

        private void HandleTutorialState(TutorialStatePayload state)
        {
            _overlay?.Show(state);
        }

        private async void ContinueTutorial()
        {
            if (_session == null)
                return;
            try
            {
                await _session.ContinueTutorialAsync();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to continue tutorial: {exception.Message}",
                    this);
            }
        }

        private async void ResumeAfterEntrance()
        {
            if (_session == null || _exitPending)
                return;

            try
            {
                await _session.SetTrainingPausedAsync(false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to resume training: {exception.Message}", this);
            }
        }

        private async void RequestExitConfirmation()
        {
            if (_exitPending || _exitDialogPending ||
                (_overlay != null && _overlay.IsTransitioning))
            {
                return;
            }

            _exitDialogPending = true;
            try
            {
                if (_session != null && !_battleConcluded)
                    await _session.SetTrainingPausedAsync(true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Failed to pause training for exit confirmation: {exception.Message}",
                    this);
            }
            finally
            {
                _exitDialogPending = false;
            }

            if (_pausePanel != null)
            {
                _overlay?.SetPauseVisible(true);
                _pausePanel.Show();
            }
            else
            {
                CancelExitConfirmation();
            }
        }

        private async void CancelExitConfirmation()
        {
            if (_exitPending)
                return;

            _pausePanel?.Hide();
            _overlay?.SetPauseVisible(false);
            try
            {
                if (_session != null && !_battleConcluded)
                    await _session.SetTrainingPausedAsync(false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to resume training: {exception.Message}", this);
            }
        }

        private void ConfirmExitTraining()
        {
            if (_exitPending)
                return;

            _exitPending = true;
            _pausePanel?.Hide();
            if (_overlay != null)
            {
                _overlay.FadeToBlack(
                    _battleConcluded
                        ? CompleteFinishedTrainingExit
                        : SendExitTraining);
            }
            else
            {
                if (_battleConcluded)
                    CompleteFinishedTrainingExit();
                else
                    SendExitTraining();
            }
        }

        private void CompleteGuidedTraining()
        {
            if (_exitPending)
                return;

            _exitPending = true;
            SendExitTraining();
        }

        private void ExitCompletedTraining()
        {
            if (_exitPending)
                return;

            _battleConcluded = true;
            _exitPending = true;
            if (_overlay != null)
                _overlay.FadeToBlack(CompleteFinishedTrainingExit);
            else
                CompleteFinishedTrainingExit();
        }

        private void HandleBattleEnded(BattleEndedPayload payload)
        {
            _battleConcluded = true;
        }

        private void CompleteFinishedTrainingExit()
        {
            _session?.ClearBattleAssignment();
            ReturnToMenu();
        }

        private async void SendExitTraining()
        {
            try
            {
                if (_session != null)
                    await _session.ExitTrainingAsync();
                else
                    ReturnToMenu();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to exit training: {exception.Message}", this);
                _session?.ClearBattleAssignment();
                ReturnToMenu();
            }
        }

        private void HandleTrainingExited()
        {
            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            if (_sceneLoader != null)
                _sceneLoader.LoadMainMenu();
        }
    }
}
