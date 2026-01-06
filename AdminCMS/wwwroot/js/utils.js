/**
 * Global Utilities for AdminCMS
 * This file contains shared utility functions and global variables
 */

// Global UrlRoot variable - populated from appsettings.json via _Layout.cshtml
var UrlRoot = UrlRoot || '/';

/**
 * Utility functions
 */
const Utils = {
    /**
     * Build URL with UrlRoot prefix
     * @param {string} path - The path to append to UrlRoot
     * @returns {string} Full URL
     */
    buildUrl: function(path) {
        // Remove leading slash from path if UrlRoot already ends with slash
        if (UrlRoot.endsWith('/') && path.startsWith('/')) {
            path = path.substring(1);
        }
        // Add trailing slash to UrlRoot if it doesn't have one and path doesn't start with one
        if (!UrlRoot.endsWith('/') && !path.startsWith('/')) {
            return UrlRoot + '/' + path;
        }
        return UrlRoot + path;
    },

    /**
     * Show loading indicator
     */
    showLoading: function() {
        // Implementation will be added later
    },

    /**
     * Hide loading indicator
     */
    hideLoading: function() {
        // Implementation will be added later
    }
};
