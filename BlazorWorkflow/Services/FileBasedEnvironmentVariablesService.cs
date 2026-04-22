using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BlazorWorkflow.Services
{
    /// <summary>
    /// File-based implementation of IEnvironmentVariablesService.
    /// Stores environment variables as a JSON file.
    /// </summary>
    public class FileBasedEnvironmentVariablesService : IEnvironmentVariablesService
    {
        private readonly string _filePath;
        private readonly ILogger<FileBasedEnvironmentVariablesService>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();
        private Dictionary<string, Dictionary<string, string>> _environments;

        public FileBasedEnvironmentVariablesService(string filePath, ILogger<FileBasedEnvironmentVariablesService>? logger = null)
        {
            _filePath = filePath;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Load or initialize
            _environments = LoadFromFile();
        }

        public List<string> GetAllEnvironments()
        {
            lock (_lock)
            {
                return _environments.Keys.OrderBy(k => k).ToList();
            }
        }

        public void AddEnvironment(string environmentName)
        {
            lock (_lock)
            {
                if (!_environments.ContainsKey(environmentName))
                {
                    _environments[environmentName] = new Dictionary<string, string>();
                    SaveToFile();
                    _logger?.LogInformation("Added environment: {Environment}", environmentName);
                }
            }
        }

        public void RemoveEnvironment(string environmentName)
        {
            lock (_lock)
            {
                if (_environments.Remove(environmentName))
                {
                    SaveToFile();
                    _logger?.LogInformation("Removed environment: {Environment}", environmentName);
                }
            }
        }

        public void RenameEnvironment(string oldName, string newName)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(oldName, out var variables))
                {
                    _environments.Remove(oldName);
                    _environments[newName] = variables;
                    SaveToFile();
                    _logger?.LogInformation("Renamed environment: {OldName} -> {NewName}", oldName, newName);
                }
            }
        }

        public Dictionary<string, string> GetAllVariables(string environmentName)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(environmentName, out var variables))
                {
                    return new Dictionary<string, string>(variables);
                }
                return new Dictionary<string, string>();
            }
        }

        public string? GetVariable(string environmentName, string key)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(environmentName, out var variables))
                {
                    return variables.TryGetValue(key, out var value) ? value : null;
                }
                return null;
            }
        }

        public void SetVariable(string environmentName, string key, string value)
        {
            lock (_lock)
            {
                if (!_environments.ContainsKey(environmentName))
                {
                    _environments[environmentName] = new Dictionary<string, string>();
                }
                _environments[environmentName][key] = value;
                SaveToFile();
                _logger?.LogDebug("Set variable: {Environment}.{Key} = {Value}", environmentName, key, value);
            }
        }

        public void RemoveVariable(string environmentName, string key)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(environmentName, out var variables))
                {
                    if (variables.Remove(key))
                    {
                        SaveToFile();
                        _logger?.LogDebug("Removed variable: {Environment}.{Key}", environmentName, key);
                    }
                }
            }
        }

        public void RenameVariable(string environmentName, string oldKey, string newKey)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(environmentName, out var variables) && variables.ContainsKey(oldKey))
                {
                    // Rebuild dictionary preserving insertion order with new key in same position
                    var rebuilt = new Dictionary<string, string>();
                    foreach (var kvp in variables)
                    {
                        if (kvp.Key == oldKey)
                            rebuilt[newKey] = kvp.Value;
                        else
                            rebuilt[kvp.Key] = kvp.Value;
                    }
                    _environments[environmentName] = rebuilt;
                    SaveToFile();
                    _logger?.LogDebug("Renamed variable: {Environment}.{OldKey} -> {NewKey}", environmentName, oldKey, newKey);
                }
            }
        }

        public bool VariableExists(string environmentName, string key)
        {
            lock (_lock)
            {
                if (_environments.TryGetValue(environmentName, out var variables))
                {
                    return variables.ContainsKey(key);
                }
                return false;
            }
        }

        private Dictionary<string, Dictionary<string, string>> LoadFromFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var environments = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, _jsonOptions);
                    if (environments != null)
                    {
                        _logger?.LogInformation("Loaded environment variables from: {FilePath}", _filePath);
                        return environments;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load environment variables from file: {FilePath}", _filePath);
            }

            // Return default environments if file doesn't exist or failed to load
            _logger?.LogInformation("Initializing default environments");
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["Development"] = new Dictionary<string, string>
                {
                    ["API_URL"] = "https://dev-api.example.com",
                    ["TIMEOUT"] = "30",
                    ["MAX_RETRIES"] = "3"
                },
                ["Staging"] = new Dictionary<string, string>
                {
                    ["API_URL"] = "https://staging-api.example.com",
                    ["TIMEOUT"] = "60",
                    ["MAX_RETRIES"] = "5"
                },
                ["Production"] = new Dictionary<string, string>
                {
                    ["API_URL"] = "https://api.example.com",
                    ["TIMEOUT"] = "120",
                    ["MAX_RETRIES"] = "10"
                }
            };
        }

        private void SaveToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_environments, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save environment variables to file: {FilePath}", _filePath);
            }
        }
    }
}
