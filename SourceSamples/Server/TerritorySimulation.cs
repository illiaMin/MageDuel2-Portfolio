using MageDuel.Server.Battles.Domain;
using MageDuel.Server.Configuration;
using Microsoft.Extensions.Options;

namespace MageDuel.Server.Battles.Simulation;

public sealed class TerritorySimulation
{
    private readonly BattleSettings _settings;

    public TerritorySimulation(IOptions<BattleSettings> settings)
    {
        _settings = settings.Value;
    }

    public void AdvanceHitKnockbacks(
        BattleMatch battle,
        float deltaTime)
    {
        bool blueFinished =
            battle.BluePlayer.AdvanceHitKnockback(deltaTime);
        bool redFinished =
            battle.RedPlayer.AdvanceHitKnockback(deltaTime);

        if (blueFinished && redFinished &&
            battle.State == BattlePhase.Positioning &&
            battle.PositioningStage ==
            PositioningStage.AwaitingHitRecovery)
        {
            battle.ResumePositioningAfterHitRecovery();
        }
    }

    public void ResolvePendingLosses(BattleMatch battle)
    {
        if (!battle.HasPendingTerritorialLoss ||
            battle.HasBattleResult)
        {
            return;
        }

        bool simultaneous =
            battle.HasSimultaneousTerritorialLoss;
        string? loserPlayerId = simultaneous
            ? null
            : battle.BlueTerritorialLossPending
                ? battle.BluePlayerId
                : battle.RedPlayerId;
        string roundEndReason = simultaneous
            ? BattleEndReasons.SimultaneousLoss
            : battle.BlueTerritorialLossPending
                ? battle.BlueTerritorialLossReason ??
                  BattleEndReasons.ZoneExit
                : battle.RedTerritorialLossReason ??
                  BattleEndReasons.ZoneExit;

        if (!simultaneous && loserPlayerId != null)
        {
            int direction = loserPlayerId == battle.BluePlayerId
                ? -1
                : 1;
            int loserIndex = TerritoryZones.IndexOf(
                loserPlayerId == battle.BluePlayerId
                    ? battle.BluePlayer.CurrentZone
                    : battle.RedPlayer.CurrentZone);

            int nextLoserIndex = loserIndex + direction;
            if (nextLoserIndex < 0 ||
                nextLoserIndex >= TerritoryZones.Ordered.Count)
            {
                BattlePlayerState loser =
                    loserPlayerId == battle.BluePlayerId
                        ? battle.BluePlayer
                        : battle.RedPlayer;

                // A hit starts its knockback before territory resolution.
                // Keep the match alive until the victim has physically
                // crossed the arena boundary so the final flight is visible
                // in authoritative snapshots.
                if (loser.IsInHitKnockback)
                {
                    return;
                }

                battle.ClearPendingTerritorialLosses();
                battle.CompleteByFullRingOut(loserPlayerId);
                return;
            }

            ShiftAssignedZones(battle, direction);
        }

        battle.ClearPendingTerritorialLosses();

        if (ShouldFinishRoundWithoutPositioning(battle))
        {
            battle.ResolveRegularRoundByTerritory(
                roundEndReason);
            battle.BeginRoundEnding(
                SecondsToMilliseconds(
                    _settings.TerritoryKnockbackSeconds));
            return;
        }

        int positioningWindowMs =
            SecondsToMilliseconds(
                _settings.PositioningWindowSeconds);

        bool isAnyHitRecoveryActive =
            battle.BluePlayer.IsInHitKnockback ||
            battle.RedPlayer.IsInHitKnockback;

        if (isAnyHitRecoveryActive)
        {
            battle.BeginPositioningAfterHitRecovery(
                positioningWindowMs);
        }
        else
        {
            battle.RestartFirstPositioningWindow(
                positioningWindowMs);
        }
    }

    private bool ShouldFinishRoundWithoutPositioning(
        BattleMatch battle)
    {
        int thresholdMs = SecondsToMilliseconds(
            _settings.PositioningSkipThresholdSeconds);

        return battle.RoundTimeRemainingMs >= 0 &&
               battle.RoundTimeRemainingMs < thresholdMs;
    }

    private static void ShiftAssignedZones(
        BattleMatch battle,
        int direction)
    {
        int blueIndex =
            TerritoryZones.IndexOf(
                battle.BluePlayer.CurrentZone);
        int redIndex =
            TerritoryZones.IndexOf(
                battle.RedPlayer.CurrentZone);

        battle.BluePlayer.SetZone(
            TerritoryZones.At(blueIndex + direction));
        battle.RedPlayer.SetZone(
            TerritoryZones.At(redIndex + direction));
    }

    private static int SecondsToMilliseconds(float seconds) =>
        (int)MathF.Round(seconds * 1000.0f);
}
