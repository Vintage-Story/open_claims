using OpenConfiguration;
using Vintagestory.API.Common;

namespace OpenClaims;

public class ClaimConfiguration
{
    public int MinDistanceBetweenPlayersClaims = 0;
}

public static partial class Configuration
{
    public static ClaimConfiguration Claim = new();

    private static void LoadClaim(ICoreAPI api)
        => Claim = ConfigManager.Load<ClaimConfiguration>(api, "ModConfig/OpenClaims", "claim", Logger(api));
}
