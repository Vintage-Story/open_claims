using OpenConfiguration;
using Vintagestory.API.Common;

namespace OpenClaims;

#pragma warning disable CA2211
public static partial class Configuration
{
    private static ModLogger Logger(ICoreAPI api) => new(api.Logger, "OpenClaims");

    internal static void Load(ICoreAPI api)
    {
        LoadProgression(api);
        LoadExpiration(api);
        LoadClaim(api);
    }
}
