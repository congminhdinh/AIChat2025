(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentKeyword = '';
    let searchTimeout = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeAccount();
    });

    function initializeAccount() {
        setupEventListeners();
        loadAccountList();
    }

    function setupEventListeners() {
        // Create button
        const createButton = document.querySelector('#btn-create-account');
        if (createButton) {
            createButton.addEventListener('click', function (e) {
                e.preventDefault();
                openCreateModal();
            });
        }

        // Refresh button
        const refreshButton = document.querySelector('#btn-refresh-accounts');
        if (refreshButton) {
            refreshButton.addEventListener('click', function (e) {
                e.preventDefault();
                loadAccountList(currentKeyword, 1);
            });
        }

        // Search input with debounce
        const searchInput = document.querySelector('#txtSearchAccount');
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentKeyword = e.target.value.trim();
                    loadAccountList(currentKeyword, 1);
                }, 500); // 500ms debounce
            });
        }

        // Event delegation for dynamically loaded buttons
        document.addEventListener('click', function (e) {
            // Edit button
            if (e.target.closest('.btn-edit-account')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-edit-account');
                const accountId = btn.getAttribute('data-account-id');
                openEditModal(accountId);
            }

            // Change Password button
            if (e.target.closest('.btn-change-password-account')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-change-password-account');
                const accountId = btn.getAttribute('data-account-id');
                openChangePasswordModalForAccount(accountId);
            }

            // Delete button
            if (e.target.closest('.btn-delete-account')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-delete-account');
                const accountId = btn.getAttribute('data-account-id');
                deleteAccount(accountId);
            }

            // Pagination links
            if (e.target.classList.contains('page-link') && !e.target.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(e.target.getAttribute('data-page'));
                if (page > 0) {
                    loadAccountList(currentKeyword, page);
                }
            }
        });
    }

    // ========== LOAD ACCOUNT LIST (PARTIAL VIEW) ==========
    async function loadAccountList(keyword = '', pageIndex = 1) {
        try {
            showLoadingState();
            currentPage = pageIndex;
            currentKeyword = keyword;

            const params = new URLSearchParams({
                keyword: keyword,
                pageIndex: pageIndex,
                pageSize: pageSize
            });

            const response = await fetch(`${WEB_APP_URL}/Account/GetAccounts?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load accounts');
            }

            const html = await response.text();

            // Inject HTML into container
            const container = document.querySelector('#data-list-container');
            if (container) {
                container.innerHTML = html;
            }

        } catch (error) {
            console.error('Error loading accounts:', error);
            showToast('error', 'Đã xảy ra lỗi khi tải danh sách tài khoản');
        }
    }

    function showLoadingState() {
        const container = document.querySelector('#data-list-container');
        if (!container) return;

        const loadingHtml = `
            <div class="loading-spinner">
                <i class="bi bi-hourglass-split"></i>
                <p class="mt-3">Đang tải dữ liệu...</p>
            </div>
        `;

        container.innerHTML = loadingHtml;
    }

    // ========== EDIT MODAL ==========
    async function openEditModal(accountId) {
        try {
            const response = await fetch(`${WEB_APP_URL}/Account/GetAccountById?id=${accountId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load account details');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler
                const submitBtn = document.querySelector('#btnSubmitEditAccount');
                if (submitBtn) {
                    submitBtn.addEventListener('click', submitEditAccount);
                }

                const nameInput = document.querySelector('#editAccountName');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening edit modal:', error);
            showToast('error', 'Không thể mở form chỉnh sửa tài khoản.');
        }
    }

    function closeEditModal() {
        const modalOverlay = document.querySelector('#modal-overlay');
        if (modalOverlay) {
            modalOverlay.classList.remove('active');
            setTimeout(() => {
                const modalContainer = document.querySelector('#modal-content-container');
                if (modalContainer) {
                    modalContainer.innerHTML = '';
                }
            }, 200);
        }
    }

    async function submitEditAccount() {
        const form = document.querySelector('#editAccountForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        const formData = new FormData(form);

        // Validate required fields
        const accountName = formData.get('Name');
        if (!accountName || accountName.trim() === '') {
            showToast('warning', 'Vui lòng nhập tên tài khoản');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Account/Update`, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeEditModal();
                showToast('success', result.message || 'Cập nhật tài khoản thành công');
                await loadAccountList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể cập nhật tài khoản. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error updating account:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== DELETE ==========
    async function deleteAccount(accountId) {
        const result = await Swal.fire({
            title: 'Xác nhận xóa',
            text: 'Bạn có chắc chắn muốn xóa tài khoản này? Hành động này không thể hoàn tác.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy bỏ'
        });

        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Account/Delete?id=${accountId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Xóa tài khoản thành công');
                await loadAccountList(currentKeyword, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể xóa tài khoản. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error deleting account:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== CREATE MODAL ==========
    async function openCreateModal() {
        try {
            const response = await fetch(`${WEB_APP_URL}/Account/GetCreateAccountModal`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load create account modal');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler
                const submitBtn = document.querySelector('#btnSubmitCreateAccount');
                if (submitBtn) {
                    submitBtn.addEventListener('click', submitCreateAccount);
                }

                const nameInput = document.querySelector('#createAccountName');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening create modal:', error);
            showToast('error', 'Không thể mở form tạo tài khoản.');
        }
    }

    function closeCreateModal() {
        const modalOverlay = document.querySelector('#modal-overlay');
        if (modalOverlay) {
            modalOverlay.classList.remove('active');
            setTimeout(() => {
                const modalContainer = document.querySelector('#modal-content-container');
                if (modalContainer) {
                    modalContainer.innerHTML = '';
                }
            }, 200);
        }
    }

    async function submitCreateAccount() {
        const form = document.querySelector('#createAccountForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        const formData = new FormData(form);

        // Validate required fields
        const accountName = formData.get('Name');
        if (!accountName || accountName.trim() === '') {
            showToast('warning', 'Vui lòng nhập tên tài khoản');
            return;
        }

        const email = formData.get('Email');
        if (!email || email.trim() === '') {
            showToast('warning', 'Vui lòng nhập email');
            return;
        }

        const password = formData.get('Password');
        if (!password || password.trim() === '') {
            showToast('warning', 'Vui lòng nhập mật khẩu');
            return;
        }

        if (password.length < 6) {
            showToast('warning', 'Mật khẩu phải có ít nhất 6 ký tự');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Account/Create`, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeCreateModal();
                showToast('success', result.message || 'Tạo tài khoản thành công');
                await loadAccountList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể tạo tài khoản. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error creating account:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== NOTIFICATION HELPERS ==========
    function showToast(type, message) {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });

        Toast.fire({
            icon: type, // 'success', 'error', 'warning', 'info'
            title: message
        });
    }

    // ========== CHANGE PASSWORD MODAL ==========
    async function openChangePasswordModal() {
        try {
            const response = await fetch(`${WEB_APP_URL}/Account/GetChangePasswordModal`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load change password modal');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler
                const submitBtn = document.querySelector('#btnSubmitChangePassword');
                if (submitBtn) {
                    submitBtn.addEventListener('click', submitChangePassword);
                }

                const oldPasswordInput = document.querySelector('#oldPassword');
                if (oldPasswordInput) {
                    setTimeout(() => oldPasswordInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening change password modal:', error);
            showToast('error', 'Không thể mở form đổi mật khẩu.');
        }
    }

    async function openChangePasswordModalForAccount(accountId) {
        try {
            const response = await fetch(`${WEB_APP_URL}/Account/GetAdminChangePasswordModal?accountId=${accountId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load admin change password modal');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler
                const submitBtn = document.querySelector('#btnSubmitAdminChangePassword');
                if (submitBtn) {
                    submitBtn.addEventListener('click', submitAdminChangePassword);
                }

                const newPasswordInput = document.querySelector('#adminNewPassword');
                if (newPasswordInput) {
                    setTimeout(() => newPasswordInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening admin change password modal:', error);
            showToast('error', 'Không thể mở form đổi mật khẩu.');
        }
    }

    function closeAdminChangePasswordModal() {
        const modalOverlay = document.querySelector('#modal-overlay');
        if (modalOverlay) {
            modalOverlay.classList.remove('active');
            setTimeout(() => {
                const modalContainer = document.querySelector('#modal-content-container');
                if (modalContainer) {
                    modalContainer.innerHTML = '';
                }
            }, 200);
        }
    }

    async function submitAdminChangePassword() {
        const accountId = document.querySelector('#adminPasswordAccountId')?.value;
        const newPassword = document.querySelector('#adminNewPassword')?.value;
        const confirmPassword = document.querySelector('#adminConfirmPassword')?.value;

        // Validate required fields
        if (!newPassword || newPassword.trim() === '') {
            showToast('warning', 'Vui lòng nhập mật khẩu mới');
            return;
        }

        if (!confirmPassword || confirmPassword.trim() === '') {
            showToast('warning', 'Vui lòng xác nhận mật khẩu mới');
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast('warning', 'Mật khẩu xác nhận không khớp');
            return;
        }

        if (newPassword.length < 6) {
            showToast('warning', 'Mật khẩu phải có ít nhất 6 ký tự');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Account/AdminChangePassword`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    AccountId: parseInt(accountId),
                    NewPassword: newPassword,
                    ConfirmPassword: confirmPassword
                })
            });

            const result = await response.json();

            if (result.success) {
                closeAdminChangePasswordModal();
                showToast('success', result.message || 'Đặt lại mật khẩu thành công');
            } else {
                showToast('error', result.message || 'Không thể đặt lại mật khẩu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error changing password:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    function closeChangePasswordModal() {
        const modalOverlay = document.querySelector('#modal-overlay');
        if (modalOverlay) {
            modalOverlay.classList.remove('active');
            setTimeout(() => {
                const modalContainer = document.querySelector('#modal-content-container');
                if (modalContainer) {
                    modalContainer.innerHTML = '';
                }
            }, 200);
        }
    }

    async function submitChangePassword() {
        const oldPassword = document.querySelector('#oldPassword')?.value;
        const newPassword = document.querySelector('#newPassword')?.value;
        const confirmPassword = document.querySelector('#confirmPassword')?.value;

        // Validate required fields
        if (!oldPassword || oldPassword.trim() === '') {
            showToast('warning', 'Vui lòng nhập mật khẩu hiện tại');
            return;
        }

        if (!newPassword || newPassword.trim() === '') {
            showToast('warning', 'Vui lòng nhập mật khẩu mới');
            return;
        }

        if (!confirmPassword || confirmPassword.trim() === '') {
            showToast('warning', 'Vui lòng xác nhận mật khẩu mới');
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast('warning', 'Mật khẩu xác nhận không khớp');
            return;
        }

        if (newPassword.length < 6) {
            showToast('warning', 'Mật khẩu phải có ít nhất 6 ký tự');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Account/ChangePassword`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    OldPassword: oldPassword,
                    NewPassword: newPassword,
                    ConfirmPassword: confirmPassword
                })
            });

            const result = await response.json();

            if (result.success) {
                closeChangePasswordModal();
                showToast('success', result.message || 'Đổi mật khẩu thành công');
            } else {
                showToast('error', result.message || 'Không thể đổi mật khẩu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error changing password:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // Expose functions to global scope for inline onclick handlers
    window.closeEditModal = closeEditModal;
    window.closeCreateModal = closeCreateModal;
    window.closeAdminChangePasswordModal = closeAdminChangePasswordModal;
    window.closeChangePasswordModal = closeChangePasswordModal;
    window.openChangePasswordModal = openChangePasswordModal;
})();
