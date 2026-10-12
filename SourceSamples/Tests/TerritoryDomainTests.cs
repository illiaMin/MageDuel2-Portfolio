using MageDuel.Server.Battles.Domain;
using Xunit;

namespace MageDuel2Server.Tests.Battles;

public sealed class TerritoryDomainTests
{
    [Fact]
    public void TerritoryOrderMatchesArenaProgression()
    {
        Assert.Equal(0, TerritoryZones.IndexOf(TerritoryZones.Blue3));
        Assert.Equal(3, TerritoryZones.IndexOf(TerritoryZones.Neutral));
        Assert.Equal(6, TerritoryZones.IndexOf(TerritoryZones.Red3));
        Assert.Equal(0, TerritoryZones.DistanceFromCenter(TerritoryZones.Neutral));
        Assert.Equal(3, TerritoryZones.DistanceFromCenter(TerritoryZones.Red3));
    }

    [Fact]
    public void PositioningPreservesRoundTimerAndRestartsAtFirstInstruction()
    {
        BattleMatch battle = CreateBattle();
        battle.SetPhase(BattlePhase.RoundActive, 42_000);

        battle.RestartFirstPositioningWindow(5_000);

        Assert.Equal(BattlePhase.Positioning, battle.State);
        Assert.Equal(PositioningStage.FirstInstruction, battle.PositioningStage);
        Assert.Equal(42_000, battle.RoundTimeRemainingMs);
        Assert.Equal(5_000, battle.PositioningTimeRemainingMs);

        battle.ResumeActiveRound();

        Assert.Equal(BattlePhase.RoundActive, battle.State);
        Assert.Equal(42_000, battle.PhaseTimeRemainingMs);
        Assert.Equal(PositioningStage.None, battle.PositioningStage);
    }

    [Fact]
    public void SimultaneousSecondYellowProducesTwoDefeatsWithoutWinner()
    {
        BattleMatch battle = CreateBattle();
        battle.ApplyYellowCards("first", "first", 2);

        battle.ApplyYellowCards("second", "second", 2);

        Assert.True(battle.HasBattleResult);
        Assert.Null(battle.BattleWinnerTeam);
        Assert.Null(battle.BattleWinnerPlayerId);
        Assert.Equal(
            BattleEndReasons.BothDisqualified,
            battle.BattleEndReason);
        Assert.Equal(2, battle.BluePlayer.YellowCardCount);
        Assert.Equal(2, battle.RedPlayer.YellowCardCount);
    }

    [Fact]
    public void HitKnockbackMovesPlayerAndPreservesMatchCounters()
    {
        BattlePlayerState player = CreatePlayer(
            "blue",
            TeamNames.Blue,
            TerritoryZones.Blue1,
            -20.0f);
        player.BeginCast();
        player.CompleteCast(3);
        player.RegisterHit();
        player.ClearHeldProjectiles();

        player.BeginHitKnockback(0.0f, -40.0f, 1.0f);

        Assert.False(player.AdvanceHitKnockback(0.5f));
        Assert.InRange(player.Z, -40.0f, -20.0f);
        Assert.True(player.AdvanceHitKnockback(0.5f));
        Assert.Equal(-40.0f, player.Z, 3);
        Assert.Equal(1, player.HitCount);
        Assert.Equal(0, player.ProjectileCount);
        Assert.Equal(PlayerActionStates.Free, player.ActionState);
    }

    [Fact]
    public void TerminalMatchResultClearsFlyingAndHeldProjectiles()
    {
        BattleMatch battle = CreateBattle();
        battle.BluePlayer.BeginCast();
        battle.BluePlayer.CompleteCast(2);
        battle.RedPlayer.BeginCast();
        battle.RedPlayer.CompleteCast(1);
        battle.AddProjectile(new BattleProjectileState(
            "still-flying",
            battle.BluePlayerId,
            0.0f,
            0.0f,
            0.0f,
            70.0f,
            70.0f));

        battle.CompleteByFullRingOut(battle.RedPlayerId);

        Assert.Empty(battle.Projectiles);
        Assert.Equal(0, battle.BluePlayer.ProjectileCount);
        Assert.Equal(0, battle.RedPlayer.ProjectileCount);
    }

    private static BattleMatch CreateBattle() => new(
        "battle",
        CreatePlayer(
            "blue",
            TeamNames.Blue,
            TerritoryZones.Blue1,
            -20.0f),
        CreatePlayer(
            "red",
            TeamNames.Red,
            TerritoryZones.Red1,
            20.0f));

    private static BattlePlayerState CreatePlayer(
        string id,
        string team,
        string zone,
        float z)
    {
        BattlePlayerState player = new(
            id + "-connection",
            id,
            id,
            team,
            zone,
            0.0f,
            z,
            0.0f,
            team == TeamNames.Blue ? 1.0f : -1.0f);
        player.InitializeEnergy(100.0f);
        return player;
    }
}
