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
    },

    /**
     * Button Protection - Prevents double-click and shows loading state
     * Tracks processing buttons globally to prevent multiple simultaneous clicks
     */
    ButtonProtection: {
        processingButtons: new WeakSet(),

        /**
         * Protect a button from double-clicks by adding loading state and disabling it
         * @param {HTMLElement} button - The button element to protect
         * @param {Function} asyncFn - The async function to execute
         * @returns {Promise} The result of the async function
         */
        protect: function(button, asyncFn) {
            // If button is already processing, ignore this click
            if (this.processingButtons.has(button)) {
                console.warn('Button is already processing, ignoring click');
                return Promise.resolve();
            }

            // Mark button as processing
            this.processingButtons.add(button);
            button.disabled = true;
            button.classList.add('btn-loading');

            // Store original button content (in case we need to restore it)
            const originalHtml = button.innerHTML;

            // Execute the async function and handle cleanup
            return Promise.resolve(asyncFn())
                .finally(() => {
                    // Always clean up, even if the async function throws
                    this.processingButtons.delete(button);
                    button.disabled = false;
                    button.classList.remove('btn-loading');
                });
        },

        /**
         * Add loading state to a button without promise wrapping
         * @param {HTMLElement} button - The button element
         */
        startLoading: function(button) {
            if (!button) return;
            this.processingButtons.add(button);
            button.disabled = true;
            button.classList.add('btn-loading');
        },

        /**
         * Remove loading state from a button
         * @param {HTMLElement} button - The button element
         */
        stopLoading: function(button) {
            if (!button) return;
            this.processingButtons.delete(button);
            button.disabled = false;
            button.classList.remove('btn-loading');
        },

        /**
         * Check if a button is currently processing
         * @param {HTMLElement} button - The button element
         * @returns {boolean} True if button is processing
         */
        isProcessing: function(button) {
            return this.processingButtons.has(button);
        }
    },

    /**
     * AJAX Helper with automatic loading state management
     * @param {Object} options - jQuery AJAX options
     * @param {HTMLElement} button - Optional button to show loading state
     * @returns {Promise} AJAX promise
     */
    ajax: function(options, button = null) {
        if (button) {
            this.ButtonProtection.startLoading(button);
        }

        return $.ajax(options)
            .always(() => {
                if (button) {
                    this.ButtonProtection.stopLoading(button);
                }
            });
    },

    /**
     * Event Delegation Helper - Standardized event binding for dynamic content
     * @param {string} selector - CSS selector for the target element
     * @param {string} eventType - Event type (e.g., 'click', 'change')
     * @param {Function} handler - Event handler function
     * @param {HTMLElement} context - Context element (defaults to document)
     */
    on: function(selector, eventType, handler, context = document) {
        context.addEventListener(eventType, function(e) {
            const target = e.target.closest(selector);
            if (target) {
                handler.call(target, e);
            }
        });
    }
};
