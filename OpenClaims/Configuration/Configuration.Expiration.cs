using OpenConfiguration;
using Vintagestory.API.Common;

namespace OpenClaims;

public class ExpirationConfiguration
{
    public bool ClaimExpirationEnabled = false;
    public int ClaimExpirationDays = 60;
}

public static partial class Configuration
{
    public static ExpirationConfiguration Expiration = new();

    private static void LoadExpiration(ICoreAPI api)
        => Expiration = ConfigManager.Load<ExpirationConfiguration>(api, "ModConfig/OpenClaims", "expiration", Logger(api));
}
