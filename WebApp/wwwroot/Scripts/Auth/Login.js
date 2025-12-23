// Login.js - Handle client-side login logic
(function () {
    'use strict';

    // Wait for DOM to be ready
    document.addEventListener('DOMContentLoaded', function () {
        const loginForm = document.querySelector('form');
        const usernameInput = document.getElementById('username');
        const passwordInput = document.getElementById('password');
        const submitButton = document.querySelector('.btn-submit');

        // Handle form submission
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            // Get form values
            const username = usernameInput.value.trim();
            const password = passwordInput.value.trim();

            // Basic validation
            if (!username || !password) {
                showError('Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu');
                return;
            }

            // Show loading state
            setLoadingState(true);

            try {
                // Call the backend API
                const response = await fetch('/Auth/ExecuteLogin', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        email: username,
                        password: password
                    })
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    // Login successful - redirect to home or dashboard
                    showSuccess('Đăng nhập thành công! Đang chuyển hướng...');
                    setTimeout(function () {
                        window.location.href = result.redirectUrl || '/';
                    }, 1000);
                } else {
                    // Login failed - show error message
                    showError(result.message || 'Đăng nhập thất bại. Vui lòng thử lại.');
                }
            } catch (error) {
                console.error('Login error:', error);
                showError('Đã xảy ra lỗi kết nối. Vui lòng thử lại sau.');
            } finally {
                setLoadingState(false);
            }
        });

        // Set loading state on submit button
        function setLoadingState(isLoading) {
            if (isLoading) {
                submitButton.disabled = true;
                submitButton.textContent = 'Đang đăng nhập...';
                submitButton.style.opacity = '0.7';
            } else {
                submitButton.disabled = false;
                submitButton.textContent = 'Đăng nhập';
                submitButton.style.opacity = '1';
            }
        }

        // Show error message
        function showError(message) {
            // Remove any existing alerts
            removeAlerts();

            // Create error alert
            const alert = document.createElement('div');
            alert.className = 'alert alert-error';
            alert.style.cssText = 'padding: 12px 16px; margin-bottom: 20px; background-color: #fee; border: 1px solid #fcc; border-radius: 8px; color: #c33;';
            alert.textContent = message;

            // Insert at the top of the login card
            const loginCard = document.querySelector('.login-card');
            loginCard.insertBefore(alert, loginCard.firstChild);
        }

        // Show success message
        function showSuccess(message) {
            // Remove any existing alerts
            removeAlerts();

            // Create success alert
            const alert = document.createElement('div');
            alert.className = 'alert alert-success';
            alert.style.cssText = 'padding: 12px 16px; margin-bottom: 20px; background-color: #efe; border: 1px solid #cfc; border-radius: 8px; color: #3c3;';
            alert.textContent = message;

            // Insert at the top of the login card
            const loginCard = document.querySelector('.login-card');
            loginCard.insertBefore(alert, loginCard.firstChild);
        }

        // Remove all alerts
        function removeAlerts() {
            const alerts = document.querySelectorAll('.alert');
            alerts.forEach(function (alert) {
                alert.remove();
            });
        }
    });
})();
