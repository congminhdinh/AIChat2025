(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentKeyword = '';
    let searchTimeout = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeTenant();
    });

    function initializeTenant() {
        setupEventListeners();
        loadTenantList();
    }

    function setupEventListeners() {
        // Create button - using event delegation for consistency
        Utils.on('#btn-create-tenant', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => openCreateModal());
        });

        // Refresh button - using event delegation for consistency
        Utils.on('#btn-refresh-tenants', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => loadTenantList(currentKeyword, 1));
        });

        // Search input with debounce
        const searchInput = document.querySelector('#txtSearchTenant');
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentKeyword = e.target.value.trim();
                    loadTenantList(currentKeyword, 1);
                }, 500); // 500ms debounce
            });
        }

        // Event delegation for dynamically loaded buttons
        document.addEventListener('click', function (e) {
            // Edit button
            if (e.target.closest('.btn-edit-tenant')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-edit-tenant');
                const tenantId = btn.getAttribute('data-tenant-id');
                openEditModal(tenantId);
            }

            // Disable button
            if (e.target.closest('.btn-disable-tenant')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-disable-tenant');
                const tenantId = btn.getAttribute('data-tenant-id');
                disableTenant(tenantId);
            }

            // View Tenant Key button
            if (e.target.closest('.btn-view-key')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-view-key');
                const tenantId = btn.getAttribute('data-tenant-id');
                openTenantKeyModal(tenantId);
            }

            // Pagination links - using closest() for consistency
            const pageLink = e.target.closest('.page-link');
            if (pageLink && !pageLink.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(pageLink.getAttribute('data-page'));
                if (page > 0) {
                    loadTenantList(currentKeyword, page);
                }
            }
        });
    }

    // ========== LOAD TENANT LIST (PARTIAL VIEW) ==========
    async function loadTenantList(keyword = '', pageIndex = 1) {
        try {
            showLoadingState();
            currentPage = pageIndex;
            currentKeyword = keyword;

            const params = new URLSearchParams({
                keyword: keyword,
                pageIndex: pageIndex,
                pageSize: pageSize
            });

            const response = await fetch(`${WEB_APP_URL}/Tenant/GetTenants?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load tenants');
            }

            const html = await response.text();

            // Inject HTML into container
            const container = document.querySelector('#data-list-container');
            if (container) {
                container.innerHTML = html;
            }

        } catch (error) {
            console.error('Error loading tenants:', error);
            showToast('error', 'Đã xảy ra lỗi khi tải danh sách tenant');
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

    // ========== CREATE MODAL ==========
    async function openCreateModal() {
        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/GetCreateTenantModal`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load create tenant modal');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                const submitBtn = document.querySelector('#btnSubmitCreateTenant');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitCreateTenant);
                    });
                }

                const nameInput = document.querySelector('#createTenantName');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening create modal:', error);
            showToast('error', 'Không thể mở form tạo tenant.');
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

    async function submitCreateTenant() {
        const form = document.querySelector('#createTenantForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        const formData = new FormData(form);

        // Validate required fields
        const tenantName = formData.get('Name');
        if (!tenantName || tenantName.trim() === '') {
            showToast('warning', 'Vui lòng nhập tên tenant');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/Create`, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeCreateModal();
                showToast('success', result.message || 'Tạo tenant thành công');
                await loadTenantList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể tạo tenant. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error creating tenant:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== EDIT MODAL ==========
    async function openEditModal(tenantId) {
        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/GetTenantById?id=${tenantId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load tenant details');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler with button protection
                const submitBtn = document.querySelector('#btnSubmitEditTenant');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitEditTenant);
                    });
                }

                const nameInput = document.querySelector('#editTenantName');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening edit modal:', error);
            showToast('error', 'Không thể mở form chỉnh sửa tenant.');
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

    async function submitEditTenant() {
        const form = document.querySelector('#editTenantForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        const formData = new FormData(form);

        // Validate required fields
        const tenantName = formData.get('Name');
        if (!tenantName || tenantName.trim() === '') {
            showToast('warning', 'Vui lòng nhập tên tenant');
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/Update`, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeEditModal();
                showToast('success', result.message || 'Cập nhật tenant thành công');
                await loadTenantList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể cập nhật tenant. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error updating tenant:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== DISABLE TENANT (RIGOROUS CONFIRMATION) ==========
    async function disableTenant(tenantId) {
        const result = await Swal.fire({
            title: 'Xác nhận vô hiệu hóa Tenant',
            html: '<p>Bạn có chắc chắn muốn vô hiệu hóa tenant này?</p>' +
                  '<p class="text-danger"><strong>Tất cả tài khoản thuộc tenant sẽ bị khóa.</strong></p>' +
                  '<p class="text-muted small">Hành động này có thể được hoàn tác.</p>',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#ffc107',  // Warning yellow
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Vô hiệu hóa',
            cancelButtonText: 'Hủy bỏ',
            reverseButtons: true
        });

        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/Deactivate?id=${tenantId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Vô hiệu hóa tenant thành công');
                await loadTenantList(currentKeyword, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể vô hiệu hóa tenant. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error disabling tenant:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== TENANT KEY MODAL ==========
    async function openTenantKeyModal(tenantId) {
        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/GetTenantKey?id=${tenantId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load tenant key');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');
            }
        } catch (error) {
            console.error('Error opening tenant key modal:', error);
            showToast('error', 'Khong the tai thong tin tenant key.');
        }
    }

    function closeTenantKeyModal() {
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

    async function copyTenantKey() {
        const keyInput = document.querySelector('#txtTenantKey');
        if (!keyInput) return;

        try {
            await navigator.clipboard.writeText(keyInput.value);
            showToast('success', 'Da sao chep tenant key');
        } catch (error) {
            console.error('Error copying tenant key:', error);
            // Fallback for older browsers
            keyInput.select();
            document.execCommand('copy');
            showToast('success', 'Da sao chep tenant key');
        }
    }

    async function refreshTenantKey() {
        const tenantIdInput = document.querySelector('#tenantKeyTenantId');
        if (!tenantIdInput) {
            showToast('error', 'Khong tim thay thong tin tenant');
            return;
        }

        const tenantId = tenantIdInput.value;

        const result = await Swal.fire({
            title: 'Xac nhan lam moi Tenant Key',
            html: '<p>Ban co chac chan muon lam moi tenant key nay?</p>' +
                  '<p class="text-danger"><strong>Webapp dang su dung key cu se khong the ket noi duoc.</strong></p>',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#ffc107',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Lam moi',
            cancelButtonText: 'Huy bo',
            reverseButtons: true
        });

        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${WEB_APP_URL}/Tenant/RefreshTenantKey?id=${tenantId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Lam moi tenant key thanh cong');
                // Reload the modal to show new key
                await openTenantKeyModal(tenantId);
            } else {
                showToast('error', responseData.message || 'Khong the lam moi tenant key. Vui long thu lai.');
            }
        } catch (error) {
            console.error('Error refreshing tenant key:', error);
            showToast('error', 'Da xay ra loi ket noi. Vui long thu lai.');
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

    // ========== GLOBAL EXPOSURE ==========
    window.closeCreateModal = closeCreateModal;
    window.closeEditModal = closeEditModal;
    window.closeTenantKeyModal = closeTenantKeyModal;
    window.copyTenantKey = copyTenantKey;
    window.refreshTenantKey = refreshTenantKey;

})();
