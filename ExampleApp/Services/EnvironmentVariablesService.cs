namespace ExampleApp.Services;

public class EnvironmentVariablesService
{
    private readonly Dictionary<string, Dictionary<string, string>> _environments = new();

    public EnvironmentVariablesService()
    {
        // Initialize with default environments
        _environments["Development"] = new Dictionary<string, string>
        {
            ["API_URL"] = "https://dev-api.example.com",
            ["TIMEOUT"] = "30",
            ["MAX_RETRIES"] = "3"
        };

        _environments["Staging"] = new Dictionary<string, string>
        {
            ["API_URL"] = "https://staging-api.example.com",
            ["TIMEOUT"] = "60",
            ["MAX_RETRIES"] = "5"
        };

        _environments["Production"] = new Dictionary<string, string>
        {
            ["API_URL"] = "https://api.example.com",
            ["TIMEOUT"] = "120",
            ["MAX_RETRIES"] = "10"
        };
    }

    public List<string> GetAllEnvironments()
    {
        return _environments.Keys.OrderBy(k => k).ToList();
    }

    public void AddEnvironment(string environmentName)
    {
        if (!_environments.ContainsKey(environmentName))
        {
            _environments[environmentName] = new Dictionary<string, string>();
        }
    }

    public void RemoveEnvironment(string environmentName)
    {
        _environments.Remove(environmentName);
    }

    public void RenameEnvironment(string oldName, string newName)
    {
        if (_environments.TryGetValue(oldName, out var variables))
        {
            _environments.Remove(oldName);
            _environments[newName] = variables;
        }
    }

    public Dictionary<string, string> GetAllVariables(string environmentName)
    {
        if (_environments.TryGetValue(environmentName, out var variables))
        {
            return new Dictionary<string, string>(variables);
        }
        return new Dictionary<string, string>();
    }

    public string? GetVariable(string environmentName, string key)
    {
        if (_environments.TryGetValue(environmentName, out var variables))
        {
            return variables.TryGetValue(key, out var value) ? value : null;
        }
        return null;
    }

    public void SetVariable(string environmentName, string key, string value)
    {
        if (!_environments.ContainsKey(environmentName))
        {
            _environments[environmentName] = new Dictionary<string, string>();
        }
        _environments[environmentName][key] = value;
    }

    public void RemoveVariable(string environmentName, string key)
    {
        if (_environments.TryGetValue(environmentName, out var variables))
        {
            variables.Remove(key);
        }
    }

    public bool VariableExists(string environmentName, string key)
    {
        if (_environments.TryGetValue(environmentName, out var variables))
        {
            return variables.ContainsKey(key);
        }
        return false;
    }
}
