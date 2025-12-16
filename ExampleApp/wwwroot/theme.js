// Theme Management for Blazor Execution Flow
window.themeManager = {
    // Get the current theme from localStorage or system preference
    getTheme: function () {
        const stored = localStorage.getItem('theme');
        if (stored) {
            return stored;
        }
        // Default to system preference
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    // Set theme and persist to localStorage
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
        return theme;
    },

    // Toggle between light and dark
    toggleTheme: function () {
        const current = this.getTheme();
        const newTheme = current === 'dark' ? 'light' : 'dark';
        return this.setTheme(newTheme);
    },

    // Initialize theme on page load
    initialize: function () {
        const theme = this.getTheme();
        document.documentElement.setAttribute('data-theme', theme);
        return theme;
    }
};

// Initialize theme immediately to prevent flash
window.themeManager.initialize();
