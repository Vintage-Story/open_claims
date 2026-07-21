using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Server;

namespace OpenClaims;

#pragma warning disable CA2211
public static class Configuration
{
    private static Dictionary<string, object> LoadConfigurationByDirectoryAndName(ICoreServerAPI api, string directory, string name)
    {
        string directoryPath = Path.Combine(api.DataBasePath, directory);
        string configPath = Path.Combine(api.DataBasePath, directory, $"{name}.json");
        Dictionary<string, object> defaultConfig = BuildDefaultConfig();
        Dictionary<string, object> loadedConfig;
        try
        {
            // Load server configurations
            string jsonConfig = File.ReadAllText(configPath);
            loadedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonConfig) ?? defaultConfig;

            // Backfill keys missing from the user's file (e.g. added by a mod update) with their default value
            bool missingKeyAdded = false;
            foreach (var entry in defaultConfig)
            {
                if (loadedConfig.ContainsKey(entry.Key)) continue;

                Debug.Log($"WARNING: Configuration key '{entry.Key}' missing from {name}.json, adding it with its default value");
                loadedConfig[entry.Key] = entry.Value;
                missingKeyAdded = true;
            }

            if (missingKeyAdded)
            {
                try
                {
                    string mergedJson = JsonConvert.SerializeObject(loadedConfig, Formatting.Indented);
                    File.WriteAllText(configPath, mergedJson);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Cannot save updated configs to {configPath}, reason: {ex.Message}");
                }
            }
        }
        catch (DirectoryNotFoundException)
        {
            Debug.Log($"WARNING: Configuration directory does not exist, creating {name}.json and directory...");
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot create directory: {ex.Message}");
            }
            Debug.Log("Loading default configurations...");
            loadedConfig = defaultConfig;

            Debug.Log($"Configurations loaded, saving configs in: {configPath}");
            try
            {
                // Saving default configurations
                string defaultJson = JsonConvert.SerializeObject(loadedConfig, Formatting.Indented);
                File.WriteAllText(configPath, defaultJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot save default files to {configPath}, reason: {ex.Message}");
            }
        }
        catch (FileNotFoundException)
        {
            Debug.Log($"WARNING: Configuration {name}.json cannot be found, recreating file from default");
            Debug.Log("Loading default configurations...");
            loadedConfig = defaultConfig;

            Debug.Log($"Configurations loaded, saving configs in: {configPath}");
            try
            {
                // Saving default configurations
                string defaultJson = JsonConvert.SerializeObject(loadedConfig, Formatting.Indented);
                File.WriteAllText(configPath, defaultJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot save default files to {configPath}, reason: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Cannot read the configurations: {ex.Message}");
            Debug.Log("Loading default values...");
            loadedConfig = defaultConfig;
        }
        return loadedConfig;
    }

    /// <summary>
    /// Builds the default configuration dictionary from the field defaults below.
    /// Whole numbers are boxed as <see cref="long"/> to match what Newtonsoft.Json produces when
    /// deserializing a JSON file, since the values below are also used as a fallback config source.
    /// </summary>
    private static Dictionary<string, object> BuildDefaultConfig() => new()
    {
        ["HoursPerExtraArea"] = HoursPerExtraArea,
        ["MaxExtraAreas"] = (long)MaxExtraAreas,
        ["SurfaceBlocksPerHour"] = (long)SurfaceBlocksPerHour,
        ["MaxExtraSurface"] = (long)MaxExtraSurface,

        ["ClaimExpirationEnabled"] = ClaimExpirationEnabled,
        ["ClaimExpirationDays"] = (long)ClaimExpirationDays,
    };

    #region baseconfigs
    #region Extra Area Progression
    public static double HoursPerExtraArea = 5.0;
    public static int MaxExtraAreas = 10;
    public static int SurfaceBlocksPerHour = 400;
    public static int MaxExtraSurface = 20_000;
    #endregion
    #region Claim Expiration
    public static bool ClaimExpirationEnabled = false;
    public static int ClaimExpirationDays = 60;
    #endregion

    private const string ConfigDir = "ModConfig/OpenClaims";
    private const string ConfigFile = "base";

    internal static void Load(ICoreServerAPI api)
    {
        Dictionary<string, object> baseConfigs = LoadConfigurationByDirectoryAndName(
            api,
            ConfigDir,
            ConfigFile
        );
        { //HoursPerExtraArea
            if (baseConfigs.TryGetValue("HoursPerExtraArea", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: HoursPerExtraArea is null");
                else if (value is not double) Debug.LogError($"CONFIGURATION ERROR: HoursPerExtraArea is not double is {value.GetType()}");
                else HoursPerExtraArea = (double)value;
            else Debug.LogError("CONFIGURATION ERROR: HoursPerExtraArea not set");
        }
        { //MaxExtraAreas
            if (baseConfigs.TryGetValue("MaxExtraAreas", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: MaxExtraAreas is null");
                else if (value is not long) Debug.LogError($"CONFIGURATION ERROR: MaxExtraAreas is not int is {value.GetType()}");
                else MaxExtraAreas = (int)(long)value;
            else Debug.LogError("CONFIGURATION ERROR: MaxExtraAreas not set");
        }
        { //SurfaceBlocksPerHour
            if (baseConfigs.TryGetValue("SurfaceBlocksPerHour", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: SurfaceBlocksPerHour is null");
                else if (value is not long) Debug.LogError($"CONFIGURATION ERROR: SurfaceBlocksPerHour is not int is {value.GetType()}");
                else SurfaceBlocksPerHour = (int)(long)value;
            else Debug.LogError("CONFIGURATION ERROR: SurfaceBlocksPerHour not set");
        }
        { //MaxExtraSurface
            if (baseConfigs.TryGetValue("MaxExtraSurface", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: MaxExtraSurface is null");
                else if (value is not long) Debug.LogError($"CONFIGURATION ERROR: MaxExtraSurface is not int is {value.GetType()}");
                else MaxExtraSurface = (int)(long)value;
            else Debug.LogError("CONFIGURATION ERROR: MaxExtraSurface not set");
        }
        { //ClaimExpirationEnabled
            if (baseConfigs.TryGetValue("ClaimExpirationEnabled", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: ClaimExpirationEnabled is null");
                else if (value is not bool) Debug.LogError($"CONFIGURATION ERROR: ClaimExpirationEnabled is not boolean is {value.GetType()}");
                else ClaimExpirationEnabled = (bool)value;
            else Debug.LogError("CONFIGURATION ERROR: ClaimExpirationEnabled not set");
        }
        { //ClaimExpirationDays
            if (baseConfigs.TryGetValue("ClaimExpirationDays", out object? value))
                if (value is null) Debug.LogError("CONFIGURATION ERROR: ClaimExpirationDays is null");
                else if (value is not long) Debug.LogError($"CONFIGURATION ERROR: ClaimExpirationDays is not int is {value.GetType()}");
                else ClaimExpirationDays = (int)(long)value;
            else Debug.LogError("CONFIGURATION ERROR: ClaimExpirationDays not set");
        }
    }
    #endregion
}
