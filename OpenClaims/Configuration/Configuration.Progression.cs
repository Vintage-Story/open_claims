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

public static partial class Configuration
{
    public static ProgressionConfiguration Progression = new();

    private static void LoadProgression(ICoreAPI api)
        => Progression = ConfigManager.Load<ProgressionConfiguration>(api, "ModConfig/OpenClaims", "progression", Logger(api));
}
