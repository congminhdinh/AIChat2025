(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentKeyword = '';
    let searchTimeout = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializePromptConfig();
    });

    function initializePromptConfig() {
        setupEventListeners();
        loadPromptConfigList();
    }

    function setupEventListeners() {
        // Create button - using event delegation for consistency
        Utils.on('#btnCreatePromptConfig', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => openCreateModal());
        });

        // Search input with debounce
        const searchInput = document.querySelector('#txtSearchPromptConfig');
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentKeyword = e.target.value.trim();
                    loadPromptConfigList(currentKeyword, 1);
                }, 500); // 500ms debounce
            });
        }

        // Event delegation for dynamically loaded buttons
        document.addEventListener('click', function (e) {
            // Edit button
            if (e.target.closest('.btn-edit-prompt-config')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-edit-prompt-config');
                const id = btn.getAttribute('data-id');
                openEditModal(parseInt(id));
            }

            // Delete button
            if (e.target.closest('.btn-delete-prompt-config')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-delete-prompt-config');
                const id = btn.getAttribute('data-id');
                deletePromptConfig(parseInt(id));
            }

            // Pagination links - using closest() for consistency
            const pageLink = e.target.closest('.page-link');
            if (pageLink && !pageLink.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(pageLink.getAttribute('data-page'));
                if (page > 0) {
                    loadPromptConfigList(currentKeyword, page);
                }
            }
        });
    }

    // ========== LOAD SYSTEM PROMPT LIST (PARTIAL VIEW) ==========
    async function loadPromptConfigList(keyword = '', pageIndex = 1) {
        try {
            showLoadingState();
            currentPage = pageIndex;
            currentKeyword = keyword;

            const params = new URLSearchParams({
                keyword: keyword,
                pageIndex: pageIndex,
                pageSize: pageSize
            });

            const response = await fetch(`${WEB_APP_URL}/PromptConfig/GetPromptConfigs?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load prompt configs');
            }

            const html = await response.text();

            // Inject HTML into container
            const container = document.querySelector('#data-list-container');
            if (container) {
                container.innerHTML = html;
            }

        } catch (error) {
            console.error('Error loading prompt configs:', error);
            showToast('error', 'Đã xảy ra lỗi khi tải danh sách prompt config');
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
            const response = await fetch(`${WEB_APP_URL}/PromptConfig/GetCreatePromptConfigModal`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load create modal');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler with button protection
                const submitBtn = document.querySelector('#btnSubmitCreatePromptConfig');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitCreatePromptConfig);
                    });
                }

                // Auto-focus on first input
                const nameInput = document.querySelector('#createPromptConfigKey');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening create modal:', error);
            showToast('error', 'Không thể mở form tạo mới.');
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

    async function submitCreatePromptConfig() {
        const form = document.querySelector('#createPromptConfigForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        // Get form data
        const name = document.querySelector('#createPromptConfigKey').value.trim();
        const content = document.querySelector('#createPromptConfigValue').value.trim();

        // Validate required fields
        if (!name) {
            showToast('warning', 'Vui lòng nhập tên');
            document.querySelector('#createPromptConfigKey').focus();
            return;
        }

        if (!content) {
            showToast('warning', 'Vui lòng nhập nội dung');
            document.querySelector('#createPromptConfigValue').focus();
            return;
        }

        try {
            const requestData = {
                Key: name,
                Value: content
            };

            const response = await fetch(`${WEB_APP_URL}/PromptConfig/Create`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                closeCreateModal();
                showToast('success', result.message || 'Tạo prompt config thành công');
                await loadPromptConfigList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', result.message || 'Không thể tạo prompt config. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error creating prompt config:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== EDIT MODAL ==========
    async function openEditModal(id) {
        try {
            const response = await fetch(`${WEB_APP_URL}/PromptConfig/GetPromptConfigById?id=${id}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load prompt config details');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler with button protection
                const submitBtn = document.querySelector('#btnSubmitEditPromptConfig');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitEditPromptConfig);
                    });
                }

                // Auto-focus on first input
                const nameInput = document.querySelector('#editPromptConfigKey');
                if (nameInput) {
                    setTimeout(() => nameInput.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening edit modal:', error);
            showToast('error', 'Không thể mở form chỉnh sửa.');
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

    async function submitEditPromptConfig() {
        const form = document.querySelector('#editPromptConfigForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        // Get form data
        const id = parseInt(document.querySelector('#editPromptConfigId').value);
        const name = document.querySelector('#editPromptConfigKey').value.trim();
        const content = document.querySelector('#editPromptConfigValue').value.trim();

        // Validate required fields
        if (!name) {
            showToast('warning', 'Vui lòng nhập tên');
            document.querySelector('#editPromptConfigKey').focus();
            return;
        }

        if (!content) {
            showToast('warning', 'Vui lòng nhập nội dung');
            document.querySelector('#editPromptConfigValue').focus();
            return;
        }

        try {
            const requestData = {
                Id: id,
                Key: name,
                Value: content
            };

            const response = await fetch(`${WEB_APP_URL}/PromptConfig/Update`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                closeEditModal();
                showToast('success', result.message || 'Cập nhật prompt config thành công');
                await loadPromptConfigList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', result.message || 'Không thể cập nhật prompt config. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error updating prompt config:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== DELETE ==========
    async function deletePromptConfig(id) {
        const result = await Swal.fire({
            title: 'Xác nhận xóa',
            text: 'Bạn có chắc chắn muốn xóa prompt config này? Hành động này không thể hoàn tác.',
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
            const response = await fetch(`${WEB_APP_URL}/PromptConfig/Delete?id=${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Xóa prompt config thành công');
                await loadPromptConfigList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể xóa prompt config. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error deleting prompt config:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== TOAST NOTIFICATIONS ==========
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

    // ========== EXPOSE FUNCTIONS TO GLOBAL SCOPE (for onclick handlers) ==========
    window.closeCreateModal = closeCreateModal;
    window.closeEditModal = closeEditModal;

})();
