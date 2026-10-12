namespace MageDuel.Server.Battles.Domain;

public enum PositioningStage : byte
{
    None = 0,
    FirstInstruction = 1,
    SecondInstruction = 2,
    FinalInstruction = 3,
    AwaitingHitRecovery = 4
}

public static class TerritoryZones
{
    public const string Blue3 = "blue3";
    public const string Blue2 = "blue2";
    public const string Blue1 = "blue1";
    public const string Neutral = "neutral";
    public const string Red1 = "red1";
    public const string Red2 = "red2";
    public const string Red3 = "red3";

    private static readonly string[] OrderedValues =
    [
        Blue3,
        Blue2,
        Blue1,
        Neutral,
        Red1,
        Red2,
        Red3
    ];

    public static IReadOnlyList<string> Ordered => OrderedValues;

    public static int IndexOf(string zone)
    {
        int index = Array.IndexOf(OrderedValues, zone);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Unknown territory zone '{zone}'.");
        }

        return index;
    }

    public static string At(int index)
    {
        if (index < 0 || index >= OrderedValues.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return OrderedValues[index];
    }

    public static int DistanceFromCenter(string zone) =>
        Math.Abs(IndexOf(zone) - IndexOf(Neutral));
}

public static class TerritorialEventKinds
{
    public const string Hit = "hit";
    public const string ZoneExit = "zoneExit";
}

public static class YellowCardReasons
{
    public const string ProjectileReleasedDuringPositioning =
        "projectileReleasedDuringPositioning";

    public const string CastWithoutTakingPosition =
        "castWithoutTakingPosition";

    public const string FailedSecondPositioningWindow =
        "failedSecondPositioningWindow";

    public const string FailedFinalPositioningWindow =
        "failedFinalPositioningWindow";
}
