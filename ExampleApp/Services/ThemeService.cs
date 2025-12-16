using Microsoft.JSInterop;

namespace ExampleApp.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "dark";

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;

    public async Task InitializeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeManager.initialize");
        }
        catch
        {
            _currentTheme = "dark";
        }
    }

    public async Task<string> GetThemeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeManager.getTheme");
            return _currentTheme;
        }
        catch
        {
            return "dark";
        }
    }

    public async Task<string> SetThemeAsync(string theme)
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeManager.setTheme", theme);
            OnThemeChanged?.Invoke();
            return _currentTheme;
        }
        catch
        {
            return _currentTheme;
        }
    }

    public async Task<string> ToggleThemeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeManager.toggleTheme");
            OnThemeChanged?.Invoke();
            return _currentTheme;
        }
        catch
        {
            return _currentTheme;
        }
    }
}
