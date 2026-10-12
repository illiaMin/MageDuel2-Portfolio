using MageDuel.Server.Battles.Domain;
using MageDuel.Server.Configuration;
using MageDuel.Server.Training;
using Microsoft.Extensions.Options;
using Xunit;

namespace MageDuel.Server.Tests.Training;

public sealed class TutorialDirectorSimulationTests
{
    [Fact]
    public void MovementIntroAdvancesWhenClientSignalsSceneReady()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Movement);
        TutorialDirectorSimulation director = CreateDirector();

        director.BeforeSimulation(battle, 33);

        Assert.Equal(BattlePhase.RoundActive, battle.State);
        Assert.True(battle.Tutorial!.BarrierActive);
        Assert.True(battle.Tutorial.AwaitingContinue);
        Assert.Equal(TutorialAllowedActions.Aim, battle.Tutorial.AllowedActions);

        Assert.True(battle.Tutorial.RequestContinue());
        director.BeforeSimulation(battle, 33);

        Assert.True(battle.Tutorial.BarrierActive);
        Assert.True(battle.Tutorial.TargetActive);
        Assert.Equal("promptRight", battle.Tutorial.StepId);
        Assert.True(battle.Tutorial.AllowedActions.HasFlag(
            TutorialAllowedActions.Move));
    }

    [Fact]
    public void MovementLessonRequiresRightLeftForwardBackwardInOrder()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Movement);
        TutorialDirectorSimulation director = CreateDirector();
        director.BeforeSimulation(battle, 33);
        battle.Tutorial!.RequestContinue();
        director.BeforeSimulation(battle, 33);

        Assert.Equal("promptRight", battle.Tutorial.StepId);
        Assert.True(battle.Tutorial.BarrierActive);
        Assert.Equal(6, battle.Tutorial.TargetX);
        Assert.Equal(-1, battle.PhaseTimeRemainingMs);

        AcceptMovement(battle.BluePlayer, -1, 1);
        director.AfterSimulation(battle);
        Assert.Equal("promptRight", battle.Tutorial.StepId);

        CompleteMovementStage(battle, director, 1, 0, "moveRight", "promptLeft");
        CompleteMovementStage(battle, director, -1, 0, "moveLeft", "promptForward");
        CompleteMovementStage(battle, director, 0, 1, "moveForward", "promptBackward");
        CompleteMovementStage(battle, director, 0, -1, "moveBackward", "complete");

        Assert.True(battle.Tutorial.LessonCompleted);
        Assert.Equal(4, battle.Tutorial.ProgressCurrent);
    }

    [Fact]
    public void InspectorSuppliedTrainingLayoutControlsActorsTargetsAndRadii()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Movement);
        battle.ConfigureTraining(new TrainingRuntimeConfiguration
        {
            PlayerStart = new TrainingPoint(3, -17),
            TeacherStart = new TrainingPoint(-4, 19),
            MovementTargets =
            [
                new TrainingPoint(11, -17),
                new TrainingPoint(3, -17),
                new TrainingPoint(3, -9),
                new TrainingPoint(3, -17)
            ],
            RollTargets =
            [
                new TrainingPoint(13, -17),
                new TrainingPoint(-7, -17)
            ],
            AttackTargets = [new TrainingPoint(1, 9)],
            ReachRadius = 2.5f,
            TargetRadius = 1.25f
        });
        TutorialDirectorSimulation director = CreateDirector();

        director.BeforeSimulation(battle, 33);
        Assert.Equal(3, battle.BluePlayer.X);
        Assert.Equal(-17, battle.BluePlayer.Z);
        Assert.Equal(-4, battle.RedPlayer.X);
        Assert.Equal(19, battle.RedPlayer.Z);

        battle.Tutorial!.RequestContinue();
        director.BeforeSimulation(battle, 33);
        Assert.Equal(11, battle.Tutorial.TargetX);
        Assert.Equal(-17, battle.Tutorial.TargetZ);

        battle.BluePlayer.SetPosition(8.6f, -17);
        AcceptMovement(battle.BluePlayer, 1, 0);
        director.AfterSimulation(battle);
        director.AfterSimulation(battle);
        Assert.Equal("promptLeft", battle.Tutorial.StepId);
    }

    [Fact]
    public void BlockBarrierReleasesProjectileOnlyAfterDelay()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Block);
        TutorialDirectorSimulation director = CreateDirector();
        director.BeforeSimulation(battle, 33);

        Assert.False(battle.Tutorial!.AwaitingContinue);
        Assert.Equal("aimAndBlock", battle.Tutorial.StepId);
        Assert.Equal("tutorial.block.intro", battle.Tutorial.InstructionKey);

        // The hold state is authoritative for the tutorial prompt. This also
        // covers a press pulse lost between client and simulation ticks.
        battle.BluePlayer.TryAcceptInput(new BattleInput(
            1,
            0,
            0,
            1,
            0,
            attackHeld: false,
            castHeld: false,
            rollPressed: false,
            spaceHeld: true,
            receivedAtMs: 123));
        director.AfterSimulation(battle);
        Assert.Equal("releaseDelay", battle.Tutorial.StepId);
        Assert.False(battle.Tutorial.BarrierActive);
        Assert.True(battle.BluePlayer.IsBlockRaising);
        Assert.Empty(battle.Projectiles);

        director.BeforeSimulation(battle, 399);
        Assert.Empty(battle.Projectiles);
        director.BeforeSimulation(battle, 1);

        Assert.Single(battle.Projectiles);
        Assert.Equal("blockIncoming", battle.Tutorial.StepId);
        Assert.False(battle.Tutorial.BarrierActive);
    }

    [Theory]
    [InlineData(TutorialLessons.Cast, "tutorial.cast.holdCast")]
    [InlineData(TutorialLessons.Attack, "tutorial.attack.castProjectile")]
    public void CastHoldDismissesInstructionPanelWithoutContinue(
        string lessonId,
        string nextInstruction)
    {
        BattleMatch battle = CreateBattle(lessonId);
        TutorialDirectorSimulation director = CreateDirector();
        director.BeforeSimulation(battle, 33);

        Assert.False(battle.Tutorial!.AwaitingContinue);
        Assert.Equal("castPrompt", battle.Tutorial.StepId);
        Assert.True(battle.Tutorial.BarrierActive);

        battle.BluePlayer.TryAcceptInput(new BattleInput(
            1,
            0,
            0,
            0,
            1,
            attackHeld: false,
            castHeld: true,
            rollPressed: false,
            spaceHeld: false,
            receivedAtMs: 123));

        director.AfterSimulation(battle);

        Assert.Equal("cast", battle.Tutorial.StepId);
        Assert.Equal(nextInstruction, battle.Tutorial.InstructionKey);
        Assert.False(battle.Tutorial.BarrierActive);
    }

    [Fact]
    public void RollLessonShowsTheThreeActionPromptsInOrder()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Roll);
        TutorialDirectorSimulation director = CreateDirector();
        director.BeforeSimulation(battle, 33);

        Assert.Equal("tutorial.roll.intro", battle.Tutorial!.InstructionKey);
        Assert.True(battle.Tutorial.BarrierActive);

        battle.BluePlayer.BeginBlock(0, 1, automatic: false);
        director.AfterSimulation(battle);

        Assert.Equal("run", battle.Tutorial.StepId);
        Assert.Equal("tutorial.roll.runWithoutSpace", battle.Tutorial.InstructionKey);
        Assert.True(battle.Tutorial.BarrierActive);

        battle.BluePlayer.SetPosition(
            battle.Tutorial.TargetX,
            battle.Tutorial.TargetZ);
        director.AfterSimulation(battle);

        Assert.Equal("roll", battle.Tutorial.StepId);
        Assert.Equal("tutorial.roll.moveAndSpace", battle.Tutorial.InstructionKey);
        Assert.True(battle.Tutorial.BarrierActive);
    }

    [Fact]
    public void RollLessonCompletesOnlyAfterRollAndRecoveryFinish()
    {
        BattleMatch battle = CreateBattle(TutorialLessons.Roll);
        TutorialDirectorSimulation director = CreateDirector();
        director.BeforeSimulation(battle, 33);

        battle.BluePlayer.BeginBlock(0, 1, automatic: false);
        director.AfterSimulation(battle);
        battle.BluePlayer.SetPosition(
            battle.Tutorial!.TargetX,
            battle.Tutorial.TargetZ);
        director.AfterSimulation(battle);

        battle.BluePlayer.BeginRoll(
            1,
            0,
            new RollProfileSettings
            {
                Distance = 3,
                DurationSeconds = .5f,
                RecoverySeconds = .25f
            });
        director.AfterSimulation(battle);

        Assert.Equal("rollInProgress", battle.Tutorial.StepId);
        Assert.False(battle.Tutorial.BarrierActive);
        Assert.False(battle.Tutorial.LessonCompleted);

        battle.BluePlayer.BeginRollRecovery();
        director.AfterSimulation(battle);
        Assert.False(battle.Tutorial.LessonCompleted);

        battle.BluePlayer.CompleteRollRecovery();
        director.AfterSimulation(battle);
        Assert.True(battle.Tutorial.LessonCompleted);
    }

    [Fact]
    public void InputPolicyRemovesForbiddenActionsButPreservesAim()
    {
        BattleInput source = new(
            1, 1, 1, 0.25f, 0.75f,
            attackHeld: true,
            castHeld: true,
            rollPressed: true,
            spaceHeld: true,
            receivedAtMs: 123);

        BattleInput filtered = TutorialInputPolicy.Filter(
            source,
            TutorialAllowedActions.Cast);

        Assert.Equal(0, filtered.MoveX);
        Assert.Equal(0, filtered.MoveZ);
        Assert.Equal(0.25f, filtered.AimX);
        Assert.Equal(0.75f, filtered.AimZ);
        Assert.False(filtered.AttackHeld);
        Assert.True(filtered.CastHeld);
        Assert.False(filtered.RollPressed);
        Assert.False(filtered.SpaceHeld);
    }

    [Fact]
    public void MovementInputPolicyAcceptsOnlyTheRequestedAxisAndSign()
    {
        BattleInput source = new(
            1, 1, 1, 0.25f, 0.75f,
            attackHeld: true,
            castHeld: true,
            rollPressed: true,
            spaceHeld: true,
            receivedAtMs: 123);

        BattleInput right = TutorialInputPolicy.Filter(
            source,
            TutorialAllowedActions.Move,
            "promptRight");
        Assert.Equal(1, right.MoveX);
        Assert.Equal(0, right.MoveZ);

        BattleInput rejectedLeft = TutorialInputPolicy.Filter(
            source,
            TutorialAllowedActions.Move,
            "moveLeft");
        Assert.Equal(0, rejectedLeft.MoveX);
        Assert.Equal(0, rejectedLeft.MoveZ);

        BattleInput backwardSource = new(
            2, -1, -1, 0.25f, 0.75f,
            attackHeld: true,
            castHeld: true,
            rollPressed: true,
            spaceHeld: true,
            receivedAtMs: 123);
        BattleInput backward = TutorialInputPolicy.Filter(
            backwardSource,
            TutorialAllowedActions.Move,
            "promptBackward");
        Assert.Equal(0, backward.MoveX);
        Assert.Equal(-1, backward.MoveZ);
        Assert.False(backward.AttackHeld);
        Assert.False(backward.CastHeld);
        Assert.False(backward.RollPressed);
        Assert.False(backward.SpaceHeld);
    }

    private static void CompleteMovementStage(
        BattleMatch battle,
        TutorialDirectorSimulation director,
        float moveX,
        float moveZ,
        string movingStep,
        string nextStep)
    {
        AcceptMovement(battle.BluePlayer, moveX, moveZ);
        director.AfterSimulation(battle);
        Assert.Equal(movingStep, battle.Tutorial!.StepId);
        Assert.False(battle.Tutorial.BarrierActive);
        battle.BluePlayer.SetPosition(
            battle.Tutorial.TargetX,
            battle.Tutorial.TargetZ);
        director.AfterSimulation(battle);
        Assert.Equal(nextStep, battle.Tutorial.StepId);
        if (nextStep != "complete")
            Assert.Null(battle.BluePlayer.CurrentInput);
    }

    private static void AcceptMovement(
        BattlePlayerState player,
        float moveX,
        float moveZ)
    {
        player.TryAcceptInput(new BattleInput(
            (uint)(player.AcknowledgedInputSequence + 1),
            moveX,
            moveZ,
            0,
            1,
            attackHeld: false,
            castHeld: false,
            rollPressed: false,
            spaceHeld: false,
            receivedAtMs: 123));
    }

    private static BattleMatch CreateBattle(string lessonId)
    {
        BattlePlayerState blue = new(
            "blue-connection", "blue-player", "Blue",
            TeamNames.Blue, "blue1", 0, -20, 0, 1);
        BattlePlayerState red = new(
            "teacher-connection", "teacher-player", "Coach",
            TeamNames.Red, "red1", 0, 20, 0, -1);
        blue.InitializeEnergy(100);
        red.InitializeEnergy(100);
        BattleMatch battle = new(
            "tutorial",
            blue,
            red,
            BattleModes.GuidedTutorial,
            lessonId);
        battle.MarkReady(blue.ConnectionId);
        battle.MarkReady(red.ConnectionId);
        Assert.True(battle.TryBeginStartNotification());
        battle.CompleteStartNotification();
        return battle;
    }

    private static TutorialDirectorSimulation CreateDirector() =>
        new(
            Options.Create(new TutorialSettings
            {
                ResumeDelayMs = 400,
                MovementStepDistance = 6,
                MovementDirectionThreshold = .25f,
                MovementExitFadeMs = 1000,
                AttackTargets =
                [
                    new TutorialPointSettings { X = 0, Z = 10 }
                ]
            }),
            Options.Create(new ProjectileSettings
            {
                InitialSpeed = 20,
                Radius = 0.5f,
                RopeSpeedMultiplier = 0.9f
            }));
}
