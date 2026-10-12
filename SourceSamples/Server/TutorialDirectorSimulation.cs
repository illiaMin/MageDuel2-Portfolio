using MageDuel.Server.Battles.Domain;
using MageDuel.Server.Configuration;
using Microsoft.Extensions.Options;

namespace MageDuel.Server.Training;

public sealed class TutorialDirectorSimulation
{
    private const int MovementStageCount = 4;

    private static readonly string[] MovementNames =
    [
        "Right", "Left", "Forward", "Backward"
    ];

    private readonly TutorialSettings _settings;
    private readonly ProjectileSettings _projectiles;
    private readonly TrainingRuntimeConfiguration _defaultLayout;

    public TutorialDirectorSimulation(
        IOptions<TutorialSettings> settings,
        IOptions<ProjectileSettings> projectiles)
    {
        _settings = settings.Value;
        _projectiles = projectiles.Value;
        _defaultLayout = TrainingRuntimeConfiguration.FromDefaults(_settings);
    }

    public void BeforeSimulation(
        BattleMatch battle,
        int elapsedMilliseconds)
    {
        TutorialSessionState? tutorial = battle.Tutorial;
        if (!battle.IsGuidedTutorial || tutorial == null ||
            !battle.HasStarted)
        {
            return;
        }

        if (tutorial.StepId == "boot")
        {
            PrepareLesson(battle, tutorial);
            return;
        }

        if (tutorial.LessonCompleted)
        {
            AdvanceCompletedLesson(
                battle,
                tutorial,
                elapsedMilliseconds);
            return;
        }

        if (tutorial.ConsumeContinueRequest())
        {
            BeginLessonTask(battle, tutorial);
            return;
        }

        if (tutorial.LessonId == TutorialLessons.Block &&
            tutorial.StepId == "releaseDelay" &&
            tutorial.AdvanceDelay(elapsedMilliseconds))
        {
            LaunchTeacherProjectile(battle, tutorial);
        }
    }

    public void AfterSimulation(BattleMatch battle)
    {
        TutorialSessionState? tutorial = battle.Tutorial;
        if (!battle.IsGuidedTutorial || tutorial == null ||
            tutorial.LessonCompleted || tutorial.AwaitingContinue)
        {
            return;
        }

        switch (tutorial.LessonId)
        {
            case TutorialLessons.Movement:
                AdvanceMovement(battle, tutorial);
                break;
            case TutorialLessons.Block:
                AdvanceBlock(battle, tutorial);
                break;
            case TutorialLessons.Roll:
                AdvanceRoll(battle, tutorial);
                break;
            case TutorialLessons.Cast:
                AdvanceCast(battle, tutorial);
                break;
            case TutorialLessons.Attack:
                AdvanceAttack(battle, tutorial);
                break;
        }
    }

    public bool ShouldRun(
        BattleMatch battle,
        TutorialAllowedActions action)
    {
        TutorialSessionState? tutorial = battle.Tutorial;
        return tutorial != null &&
               tutorial.AllowedActions.HasFlag(action);
    }

    public bool ShouldSimulateProjectiles(BattleMatch battle)
    {
        TutorialSessionState? tutorial = battle.Tutorial;
        return tutorial != null &&
               (tutorial.TrackedProjectileId != null ||
                tutorial.LessonId == TutorialLessons.Attack);
    }

    public bool ShouldResolvePlayerHits(BattleMatch battle)
    {
        TutorialSessionState? tutorial = battle.Tutorial;
        return tutorial != null &&
               tutorial.LessonId == TutorialLessons.Block &&
               tutorial.TrackedProjectileId != null;
    }

    private void PrepareLesson(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        TrainingRuntimeConfiguration layout = Layout(battle);
        battle.SetPhase(BattlePhase.RoundActive, -1);
        battle.ClearProjectiles();
        battle.ClearPendingTerritorialLosses();
        ResetActor(
            battle.BluePlayer,
            layout.PlayerStart.X,
            layout.PlayerStart.Z,
            0.0f,
            1.0f);
        ResetActor(
            battle.RedPlayer,
            layout.TeacherStart.X,
            layout.TeacherStart.Z,
            0.0f,
            -1.0f);
        tutorial.InitializeIntro();
        if (tutorial.LessonId != TutorialLessons.Movement)
        {
            BeginLessonTask(battle, tutorial);
        }
    }

    private static void ResetActor(
        BattlePlayerState player,
        float x,
        float z,
        float facingX,
        float facingZ)
    {
        player.ResetRoundActions();
        player.ClearInput();
        player.RestoreEnergyToCurrentMaximum();
        player.SetPosition(x, z);
        player.SetFacing(facingX, facingZ);
    }

    private void BeginLessonTask(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        switch (tutorial.LessonId)
        {
            case TutorialLessons.Movement:
                tutorial.SetProgress(0, MovementStageCount);
                BeginMovementPrompt(
                    battle,
                    battle.BluePlayer,
                    tutorial,
                    0);
                break;

            case TutorialLessons.Block:
                tutorial.SetStep(
                    "aimAndBlock",
                    "tutorial.block.intro",
                    TutorialAllowedActions.Block,
                    barrierActive: true);
                tutorial.SetTarget(false);
                break;

            case TutorialLessons.Roll:
                tutorial.SetStep(
                    "block",
                    "tutorial.roll.intro",
                    TutorialAllowedActions.Block,
                    barrierActive: true);
                tutorial.SetProgress(0, 3);
                break;

            case TutorialLessons.Cast:
                tutorial.SetStep(
                    "castPrompt",
                    "tutorial.cast.intro",
                    TutorialAllowedActions.Cast,
                    barrierActive: true);
                tutorial.SetProgress(0, 1);
                break;

            case TutorialLessons.Attack:
                tutorial.SetStep(
                    "castPrompt",
                    "tutorial.attack.intro",
                    TutorialAllowedActions.Cast,
                    barrierActive: true);
                tutorial.SetProgress(0, Layout(battle).AttackTargets.Length);
                break;
        }
    }

    private void AdvanceMovement(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        BattlePlayerState player = battle.BluePlayer;
        int stage = Math.Clamp(
            tutorial.ProgressCurrent,
            0,
            MovementStageCount - 1);
        string direction = MovementNames[stage];

        if (tutorial.StepId == $"prompt{direction}")
        {
            if (HasRequestedMovement(player.CurrentInput, stage))
            {
                tutorial.SetStep(
                    $"move{direction}",
                    $"tutorial.movement.move{direction}",
                    TutorialAllowedActions.Move,
                    barrierActive: false);
            }
            return;
        }

        if (tutorial.StepId != $"move{direction}" ||
            !ReachedTarget(player, tutorial, battle))
        {
            return;
        }

        int next = tutorial.ProgressCurrent + 1;
        tutorial.SetProgress(next, MovementStageCount);
        if (next >= MovementStageCount)
        {
            CompleteLesson(battle, tutorial);
            return;
        }

        BeginMovementPrompt(battle, player, tutorial, next);
    }

    private void BeginMovementPrompt(
        BattleMatch battle,
        BattlePlayerState player,
        TutorialSessionState tutorial,
        int stage)
    {
        // Do not carry the previous stage's held direction into the next
        // teaching barrier. The client must send fresh input for the newly
        // requested direction before movement resumes.
        player.ClearInput();

        string direction = MovementNames[stage];
        tutorial.SetStep(
            $"prompt{direction}",
            $"tutorial.movement.prompt{direction}",
            TutorialAllowedActions.Move,
            barrierActive: true);

        TrainingPoint target = Layout(battle).MovementTargets[stage];
        tutorial.SetTarget(true, target.X, target.Z);
    }

    private bool HasRequestedMovement(
        BattleInput? input,
        int stage)
    {
        if (input == null)
        {
            return false;
        }

        float threshold = _settings.MovementDirectionThreshold;
        return stage switch
        {
            0 => input.MoveX >= threshold,
            1 => input.MoveX <= -threshold,
            2 => input.MoveZ >= threshold,
            3 => input.MoveZ <= -threshold,
            _ => false
        };
    }

    private void AdvanceBlock(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        BattlePlayerState player = battle.BluePlayer;
        if (tutorial.StepId == "aimAndBlock")
        {
            BattleInput? input = player.CurrentInput;
            bool blockHeld = input?.SpaceHeld == true;
            if (blockHeld &&
                !player.IsBlockRaising &&
                !player.IsBlocking)
            {
                player.BeginBlock(
                    input!.AimX,
                    input.AimZ,
                    automatic: false);
            }

            if (blockHeld || player.IsBlockRaising || player.IsBlocking)
            {
                tutorial.SetStep(
                    "releaseDelay",
                    "tutorial.block.hold",
                    TutorialAllowedActions.Block,
                    barrierActive: false);
                tutorial.BeginDelay(_settings.ResumeDelayMs);
            }
            return;
        }

        if (tutorial.StepId != "blockIncoming" ||
            tutorial.TrackedProjectileId == null ||
            battle.Projectiles.Any(projectile =>
                projectile.ProjectileId == tutorial.TrackedProjectileId))
        {
            return;
        }

        if (player.HitCount == tutorial.HitCountAtAttemptStart)
        {
            CompleteLesson(battle, tutorial);
            return;
        }

        battle.ClearPendingTerritorialLosses();
        TrainingRuntimeConfiguration layout = Layout(battle);
        ResetActor(
            player,
            layout.PlayerStart.X,
            layout.PlayerStart.Z,
            0.0f,
            1.0f);
        tutorial.SetStep(
            "aimAndBlock",
            "tutorial.block.tryAgain",
            TutorialAllowedActions.Block,
            barrierActive: true);
    }

    private void AdvanceRoll(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        BattlePlayerState player = battle.BluePlayer;
        TrainingPoint[] targets = Layout(battle).RollTargets;
        if (tutorial.StepId == "block" &&
            (player.IsBlockRaising || player.IsBlocking))
        {
            player.CancelCurrentAction();
            tutorial.SetProgress(1, 3);
            tutorial.SetStep(
                "run",
                "tutorial.roll.runWithoutSpace",
                TutorialAllowedActions.Move,
                barrierActive: true);
            tutorial.SetTarget(
                true,
                targets[0].X,
                targets[0].Z);
            return;
        }

        if (tutorial.StepId == "run" &&
            ReachedTarget(player, tutorial, battle))
        {
            player.CancelCurrentAction();
            tutorial.SetProgress(2, 3);
            tutorial.SetStep(
                "roll",
                "tutorial.roll.moveAndSpace",
                TutorialAllowedActions.Move |
                TutorialAllowedActions.Block |
                TutorialAllowedActions.Roll,
                barrierActive: true);
            tutorial.SetTarget(
                true,
                targets[1].X,
                targets[1].Z);
            return;
        }

        if (tutorial.StepId == "roll" &&
            player.ActionState == PlayerActionStates.Rolling)
        {
            tutorial.SetProgress(3, 3);
            tutorial.SetStep(
                "rollInProgress",
                "tutorial.roll.moveAndSpace",
                TutorialAllowedActions.Roll,
                barrierActive: false);
            tutorial.SetTarget(false);
            return;
        }

        if (tutorial.StepId == "rollInProgress" &&
            player.ActionState != PlayerActionStates.Rolling &&
            player.ActionState != PlayerActionStates.RollRecovery)
        {
            CompleteLesson(battle, tutorial);
        }
    }

    private void AdvanceCast(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        if (tutorial.StepId == "castPrompt" &&
            battle.BluePlayer.CurrentInput?.CastHeld == true)
        {
            tutorial.SetStep(
                "cast",
                "tutorial.cast.holdCast",
                TutorialAllowedActions.Cast,
                barrierActive: false);
            return;
        }

        if (tutorial.StepId == "cast" &&
            battle.BluePlayer.ProjectileCount > 0)
        {
            tutorial.SetProgress(1, 1);
            CompleteLesson(battle, tutorial);
        }
    }

    private void AdvanceAttack(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        BattlePlayerState player = battle.BluePlayer;
        TrainingPoint[] targets = Layout(battle).AttackTargets;
        if (tutorial.StepId == "castPrompt" &&
            player.CurrentInput?.CastHeld == true)
        {
            tutorial.SetStep(
                "cast",
                "tutorial.attack.castProjectile",
                TutorialAllowedActions.Cast,
                barrierActive: false);
            return;
        }

        if (tutorial.StepId == "cast" &&
            player.ProjectileCount > 0)
        {
            tutorial.SetStep(
                "hitTarget",
                "tutorial.attack.hitTarget",
                TutorialAllowedActions.Cast |
                TutorialAllowedActions.Attack,
                barrierActive: false);
            SetTarget(tutorial, targets[0]);
            return;
        }

        if (tutorial.StepId != "hitTarget")
        {
            return;
        }

        BattleProjectileState? hit = battle.Projectiles.FirstOrDefault(
            projectile => IsAtTarget(projectile, tutorial, battle));
        if (hit == null)
        {
            return;
        }

        battle.RemoveProjectile(hit);
        int next = tutorial.ProgressCurrent + 1;
        tutorial.SetProgress(next, targets.Length);
        if (next >= targets.Length)
        {
            CompleteLesson(battle, tutorial);
            return;
        }

        SetTarget(tutorial, targets[next]);
    }

    private void LaunchTeacherProjectile(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        BattlePlayerState teacher = battle.RedPlayer;
        BattlePlayerState player = battle.BluePlayer;
        float directionX = player.X - teacher.X;
        float directionZ = player.Z - teacher.Z;
        float length = MathF.Sqrt(
            directionX * directionX + directionZ * directionZ);
        if (length <= 0.0001f)
        {
            directionZ = -1.0f;
            length = 1.0f;
        }

        directionX /= length;
        directionZ /= length;
        string projectileId = Guid.NewGuid().ToString();
        battle.AddProjectile(new BattleProjectileState(
            projectileId,
            teacher.PlayerId,
            teacher.X,
            teacher.Z,
            directionX * _projectiles.InitialSpeed,
            directionZ * _projectiles.InitialSpeed,
            _projectiles.InitialSpeed,
            isTerritoryEligible: false));
        tutorial.SetStep(
            "blockIncoming",
            "tutorial.block.incoming",
            TutorialAllowedActions.Block,
            barrierActive: false);
        tutorial.TrackProjectile(projectileId, player.HitCount);
    }

    private void AdvanceCompletedLesson(
        BattleMatch battle,
        TutorialSessionState tutorial,
        int elapsedMilliseconds)
    {
        if (!tutorial.CompletionPersisted ||
            !tutorial.AdvanceDelay(elapsedMilliseconds))
        {
            return;
        }

        string? next = TutorialLessons.Next(tutorial.LessonId);
        if (next == null)
        {
            return;
        }

        battle.BeginNextTutorialLesson(next);
        PrepareLesson(battle, battle.Tutorial!);
    }

    private void CompleteLesson(
        BattleMatch battle,
        TutorialSessionState tutorial)
    {
        battle.BluePlayer.CancelCurrentAction();
        battle.RedPlayer.CancelCurrentAction();
        battle.ClearProjectiles();
        battle.ClearPendingTerritorialLosses();
        int transitionMilliseconds =
            tutorial.LessonId == TutorialLessons.Movement
                ? _settings.MovementExitFadeMs + 150
                : _settings.LessonTransitionMs;
        tutorial.CompleteLesson(transitionMilliseconds);
    }

    private bool ReachedTarget(
        BattlePlayerState player,
        TutorialSessionState tutorial,
        BattleMatch? battle = null)
    {
        float x = player.X - tutorial.TargetX;
        float z = player.Z - tutorial.TargetZ;
        float radius = battle == null
            ? _defaultLayout.ReachRadius
            : Layout(battle).ReachRadius;
        return x * x + z * z <=
               radius * radius;
    }

    private bool IsAtTarget(
        BattleProjectileState projectile,
        TutorialSessionState tutorial,
        BattleMatch? battle = null)
    {
        float x = projectile.X - tutorial.TargetX;
        float z = projectile.Z - tutorial.TargetZ;
        float radius = battle == null
            ? _defaultLayout.TargetRadius
            : Layout(battle).TargetRadius;
        return x * x + z * z <=
               radius * radius;
    }

    private static void SetTarget(
        TutorialSessionState tutorial,
        TrainingPoint target)
    {
        tutorial.SetTarget(true, target.X, target.Z);
    }

    private TrainingRuntimeConfiguration Layout(BattleMatch battle) =>
        battle.TrainingConfiguration ?? _defaultLayout;
}
