using OpenConfiguration;
using Vintagestory.API.Common;

namespace OpenClaims;

public class ProgressionConfiguration
{
    public double HoursPerExtraArea = 5.0;
    public int MaxExtraAreas = 10;
    public int SurfaceBlocksPerHour = 400;
    public int MaxExtraSurface = 20_000;
}

public class ExpirationConfiguration
{
    public bool ClaimExpirationEnabled = false;
    public int ClaimExpirationDays = 60;
}

public class ClaimConfiguration
{
    public int MinDistanceBetweenPlayersClaims = 0;
}

#pragma warning disable CA2211
public static class Configuration
{
    public static ProgressionConfiguration Progression = new();
    public static ExpirationConfiguration Expiration = new();
    public static ClaimConfiguration Claim = new();

    internal static void Load(ICoreAPI api)
    {
        ModLogger logger = new(api.Logger, "OpenClaims");
        Progression = ConfigManager.Load<ProgressionConfiguration>(api, "ModConfig/OpenClaims", "progression", logger);
        Expiration = ConfigManager.Load<ExpirationConfiguration>(api, "ModConfig/OpenClaims", "expiration", logger);
        Claim = ConfigManager.Load<ClaimConfiguration>(api, "ModConfig/OpenClaims", "claim", logger);
    }
}
