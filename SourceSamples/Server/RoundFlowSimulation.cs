using MageDuel.Server.Battles.Domain;
using MageDuel.Server.Configuration;
using Microsoft.Extensions.Options;

namespace MageDuel.Server.Battles.Simulation;

public sealed class RoundFlowSimulation
{
    private readonly BattleSettings _battleSettings;
    private readonly ArenaSettings _arenaSettings;
    private readonly EnergySettings _energySettings;

    public RoundFlowSimulation(
        IOptions<BattleSettings> battleSettings,
        IOptions<ArenaSettings> arenaSettings,
        IOptions<EnergySettings> energySettings)
    {
        _battleSettings = battleSettings.Value;
        _arenaSettings = arenaSettings.Value;
        _energySettings = energySettings.Value;
    }

    public void Simulate(BattleMatch battle, int elapsedMs)
    {
        if (!battle.HasStarted)
        {
            return;
        }

        if (battle.HasBattleResult &&
            battle.State is not BattlePhase.MatchReaction and
                not BattlePhase.Finished)
        {
            BeginMatchReaction(battle);
            return;
        }

        if (battle.IsReconnectBreak)
        {
            return;
        }

        switch (battle.State)
        {
            case BattlePhase.WaitingBeforeBattle:
                SimulateWaitingBeforeBattle(battle, elapsedMs);
                break;
            case BattlePhase.MovingToRoundStart:
                SimulateMovingToRoundStart(battle, elapsedMs);
                break;
            case BattlePhase.Ready:
                SimulateReady(battle, elapsedMs);
                break;
            case BattlePhase.RoundActive:
                SimulateRoundActive(battle, elapsedMs);
                break;
            case BattlePhase.RoundEnding:
                SimulateRoundEnding(battle, elapsedMs);
                break;
            case BattlePhase.RoundReaction:
                SimulateRoundReaction(battle, elapsedMs);
                break;
            case BattlePhase.MatchReaction:
                SimulateMatchReaction(battle, elapsedMs);
                break;
        }
    }

    private void SimulateWaitingBeforeBattle(BattleMatch battle, int elapsedMs)
    {
        if (battle.PhaseTimeRemainingMs < 0)
        {
            PrepareRound(battle);
            battle.SetPhase(
                BattlePhase.WaitingBeforeBattle,
                SecondsToMilliseconds(_battleSettings.IntermissionSeconds));
            return;
        }

        if (battle.AdvancePhaseTimer(elapsedMs))
        {
            battle.SetPhase(BattlePhase.MovingToRoundStart, -1);
        }
    }

    private void PrepareRound(BattleMatch battle)
    {
        battle.ResetRegularRoundTerritory();
        battle.ResetRoundLossState();
        RestorePlayerResources(battle.BluePlayer);
        RestorePlayerResources(battle.RedPlayer);
        battle.BluePlayer.ClearInput();
        battle.RedPlayer.ClearInput();
        battle.ClearProjectiles();
    }

    private void SimulateMovingToRoundStart(BattleMatch battle, int elapsedMs)
    {
        float deltaTime = Math.Max(0, elapsedMs) / 1000.0f;
        bool blueArrived = MovePlayerToRoundStart(battle.BluePlayer, deltaTime);
        bool redArrived = MovePlayerToRoundStart(battle.RedPlayer, deltaTime);

        if (!blueArrived || !redArrived)
        {
            return;
        }

        battle.SetPhase(
            BattlePhase.Ready,
            SecondsToMilliseconds(_battleSettings.CountdownSeconds));
    }

    private bool MovePlayerToRoundStart(BattlePlayerState player, float deltaTime)
    {
        ZoneSettings zone = GetZone(player.CurrentZone);
        float targetX = (_arenaSettings.MinX + _arenaSettings.MaxX) * 0.5f;
        float targetZ = (zone.MinZ + zone.MaxZ) * 0.5f;
        float offsetX = targetX - player.X;
        float offsetZ = targetZ - player.Z;
        float distanceSquared = offsetX * offsetX + offsetZ * offsetZ;
        float tolerance = _battleSettings.RoundStartArrivalTolerance;

        if (distanceSquared <= tolerance * tolerance)
        {
            player.SetPosition(targetX, targetZ);
            player.SetFacing(0.0f, player.Team == TeamNames.Blue ? 1.0f : -1.0f);
            return true;
        }

        float distance = MathF.Sqrt(distanceSquared);
        float step = Math.Min(distance, _battleSettings.TransitionSpeed * deltaTime);
        if (step > 0.0f)
        {
            float directionX = offsetX / distance;
            float directionZ = offsetZ / distance;
            player.SetFacing(directionX, directionZ);
            player.SetPosition(
                Math.Clamp(player.X + directionX * step, _arenaSettings.MinX, _arenaSettings.MaxX),
                ClampToArenaZ(player.Z + directionZ * step));
        }

        return distance - step <= tolerance;
    }

    private float ClampToArenaZ(float z)
    {
        float minZ = _arenaSettings.Zones.Min(zone => zone.MinZ);
        float maxZ = _arenaSettings.Zones.Max(zone => zone.MaxZ);
        return Math.Clamp(z, minZ, maxZ);
    }

    private void SimulateReady(BattleMatch battle, int elapsedMs)
    {
        if (!battle.AdvancePhaseTimer(elapsedMs))
        {
            return;
        }

        int durationMs = battle.DuelScheduled && _battleSettings.FinalDuelTimeLimitSeconds <= 0
            ? -1
            : SecondsToMilliseconds(
                battle.DuelScheduled
                    ? _battleSettings.FinalDuelTimeLimitSeconds
                    : _battleSettings.RegularRoundDurationSeconds);
        battle.SetPhase(BattlePhase.RoundActive, durationMs);
    }

    private void SimulateRoundActive(BattleMatch battle, int elapsedMs)
    {
        if (battle.PhaseTimeRemainingMs >= 0 && battle.AdvancePhaseTimer(elapsedMs))
        {
            if (!battle.IsFinalDuel)
            {
                battle.ResolveRegularRoundByTerritory(
                    BattleEndReasons.Timeout);
            }
            BeginRoundReaction(battle);
        }
    }

    private void SimulateRoundEnding(BattleMatch battle, int elapsedMs)
    {
        if (!battle.AdvancePhaseTimer(elapsedMs))
        {
            return;
        }

        if (battle.IsRoundGetUpStage)
        {
            BeginRoundReaction(battle);
            return;
        }

        if (battle.HasSimultaneousRoundLoss)
        {
            if (battle.IsFinalDuel)
            {
                battle.RescheduleFinalTiebreakAfterSimultaneousLoss();
            }
            else
            {
                battle.ResolveRegularRoundByTerritory(
                    BattleEndReasons.SimultaneousLoss);
            }
        }
        else
        {
            battle.TryResolveSingleRoundLoss();
        }

        if (_battleSettings.RoundGetUpSeconds > 0 &&
            (battle.BlueRoundLost || battle.RedRoundLost))
        {
            battle.BeginRoundGetUp(
                SecondsToMilliseconds(_battleSettings.RoundGetUpSeconds));
            return;
        }

        BeginRoundReaction(battle);
    }

    private void BeginRoundReaction(BattleMatch battle)
    {
        battle.BluePlayer.ClearInput();
        battle.RedPlayer.ClearInput();
        if (battle.LastRoundWinnerTeam == TeamNames.Blue)
        {
            battle.BluePlayer.SetReactionState(PlayerActionStates.RoundWinner);
            battle.RedPlayer.SetReactionState(PlayerActionStates.RoundLoser);
        }
        else if (battle.LastRoundWinnerTeam == TeamNames.Red)
        {
            battle.BluePlayer.SetReactionState(PlayerActionStates.RoundLoser);
            battle.RedPlayer.SetReactionState(PlayerActionStates.RoundWinner);
        }
        else
        {
            battle.BluePlayer.SetReactionState(PlayerActionStates.RoundDraw);
            battle.RedPlayer.SetReactionState(PlayerActionStates.RoundDraw);
        }
        battle.SetPhase(
            BattlePhase.RoundReaction,
            SecondsToMilliseconds(_battleSettings.RoundReactionSeconds));
    }

    private void SimulateRoundReaction(BattleMatch battle, int elapsedMs)
    {
        if (!battle.AdvancePhaseTimer(elapsedMs))
        {
            return;
        }

        if (battle.HasBattleResult)
        {
            BeginMatchReaction(battle);
            return;
        }

        if (battle.DuelScheduled)
        {
            battle.PrepareScheduledDuel();
            battle.SetPhase(BattlePhase.WaitingBeforeBattle, -1);
            return;
        }

        if (battle.RoundNumber < _battleSettings.RegularRoundCount)
        {
            battle.MoveToNextRound();
            battle.SetPhase(BattlePhase.WaitingBeforeBattle, -1);
            return;
        }

        if (battle.TryCompleteBattleByScore())
        {
            BeginMatchReaction(battle);
            return;
        }

        battle.ScheduleFinalTiebreakDuel(
            _battleSettings.RegularRoundCount + 1);
        battle.PrepareScheduledDuel();
        battle.SetPhase(BattlePhase.WaitingBeforeBattle, -1);
    }

    private void BeginMatchReaction(BattleMatch battle)
    {
        if (battle.BattleEndReason ==
            BattleEndReasons.BothDisqualified)
        {
            battle.BluePlayer.SetReactionState(
                PlayerActionStates.MatchLoser);
            battle.RedPlayer.SetReactionState(
                PlayerActionStates.MatchLoser);
        }
        else if (battle.BattleWinnerPlayerId == null)
        {
            battle.BluePlayer.SetReactionState(PlayerActionStates.MatchDraw);
            battle.RedPlayer.SetReactionState(PlayerActionStates.MatchDraw);
        }
        else
        {
            battle.BluePlayer.SetReactionState(
                battle.BattleWinnerPlayerId == battle.BluePlayerId
                    ? PlayerActionStates.MatchWinner
                    : PlayerActionStates.MatchLoser);
            battle.RedPlayer.SetReactionState(
                battle.BattleWinnerPlayerId == battle.RedPlayerId
                    ? PlayerActionStates.MatchWinner
                    : PlayerActionStates.MatchLoser);
        }
        battle.SetPhase(
            BattlePhase.MatchReaction,
            SecondsToMilliseconds(_battleSettings.MatchReactionSeconds));
    }

    private static void SimulateMatchReaction(BattleMatch battle, int elapsedMs)
    {
        if (battle.AdvancePhaseTimer(elapsedMs))
        {
            battle.SetPhase(BattlePhase.Finished, -1);
        }
    }

    private ZoneSettings GetZone(string zoneName) =>
        _arenaSettings.Zones.FirstOrDefault(zone => zone.Name == zoneName)
        ?? throw new InvalidOperationException($"Zone '{zoneName}' was not found.");

    private void RestorePlayerResources(BattlePlayerState player)
    {
        player.RecoverEnergyPenalty(_energySettings.PenaltyRecoveryPercent);
        player.RestoreEnergyToCurrentMaximum();
        player.ResetRoundActions();
    }

    private static int SecondsToMilliseconds(float seconds) =>
        (int)MathF.Round(seconds * 1000.0f);
}
