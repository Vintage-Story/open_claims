using System.IO;
using System.Text.Json;
using Vintagestory.API.Server;

namespace OpenClaims;

public static class Configuration
{
    public static float HoursPerExtraArea  = 5f;
    public static int MaxExtraAreas        = 10;
    public static int SurfaceBlocksPerHour = 400;
    public static int MaxExtraSurface      = 20_000;
    public static bool ClaimExpirationEnabled = false;
    public static int  ClaimExpirationDays    = 30;

    private const string ConfigDir  = "ModConfig/OpenClaims";
    private const string ConfigFile = "base.json";

    internal static void Load(ICoreServerAPI api)
    {
        string dirPath  = Path.Combine(api.DataBasePath, ConfigDir);
        string filePath = Path.Combine(dirPath, ConfigFile);

        ConfigData cfg;
        try
        {
            cfg = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(filePath)) ?? new ConfigData();
        }
        catch (DirectoryNotFoundException)
        {
            Directory.CreateDirectory(dirPath);
            cfg = new ConfigData();
            File.WriteAllText(filePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (FileNotFoundException)
        {
            cfg = new ConfigData();
            File.WriteAllText(filePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        }

        HoursPerExtraArea       = cfg.HoursPerExtraArea;
        MaxExtraAreas           = cfg.MaxExtraAreas;
        SurfaceBlocksPerHour    = cfg.SurfaceBlocksPerHour;
        MaxExtraSurface         = cfg.MaxExtraSurface;
        ClaimExpirationEnabled  = cfg.ClaimExpirationEnabled;
        ClaimExpirationDays     = cfg.ClaimExpirationDays;
    }

    private class ConfigData
    {
        public float HoursPerExtraArea      { get; set; } = 5f;
        public int   MaxExtraAreas          { get; set; } = 10;
        public int   SurfaceBlocksPerHour   { get; set; } = 400;
        public int   MaxExtraSurface        { get; set; } = 20_000;
        public bool  ClaimExpirationEnabled { get; set; } = false;
        public int   ClaimExpirationDays    { get; set; } = 30;
    }
}
