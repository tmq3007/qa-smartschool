using QASmartTouch.Models.Camera;
using System.IO;
using System.Text.Json;

namespace QASmartTouch.Services.Camera;

/// <summary>
/// Service for loading and saving camera configuration
/// </summary>
public class CameraConfigService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;

    public CameraConfigService()
    {
        // Store config in AppData\Roaming\CameraConfigDemo
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CameraConfigDemo"
        );

        _configFilePath = Path.Combine(_configDirectory, "camera_config.json");
    }

    /// <summary>
    /// Loads the camera configuration from file
    /// </summary>
    public CameraProfile Load()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return CameraProfile.CreateDefault();
            }

            var json = File.ReadAllText(_configFilePath);
            var profile = JsonSerializer.Deserialize<CameraProfile>(json);

            return profile ?? CameraProfile.CreateDefault();
        }
        catch (Exception ex)
        {
            // Log error and return default
            Console.WriteLine($"Error loading config: {ex.Message}");
            return CameraProfile.CreateDefault();
        }
    }

    /// <summary>
    /// Asynchronously loads the camera configuration
    /// </summary>
    public async Task<CameraProfile> LoadAsync()
    {
        return await Task.Run(() => Load());
    }

    /// <summary>
    /// Saves the camera configuration to file
    /// </summary>
    public void Save(CameraProfile profile)
    {
        try
        {
            // Validate profile before saving
            if (!profile.IsValid())
            {
                throw new ArgumentException("Invalid camera profile");
            }

            // Ensure directory exists
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            // Serialize to JSON with formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously saves the camera configuration
    /// </summary>
    public async Task SaveAsync(CameraProfile profile)
    {
        await Task.Run(() => Save(profile));
    }

    /// <summary>
    /// Deletes the configuration file
    /// </summary>
    public void DeleteConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting config: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the path to the configuration file
    /// </summary>
    public string GetConfigPath() => _configFilePath;

    /// <summary>
    /// Checks if a configuration file exists
    /// </summary>
    public bool ConfigExists() => File.Exists(_configFilePath);
}
