(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentKeyword = '';
    let currentIsActive = -1;
    let searchTimeout = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeSystemPrompt();
    });

    function initializeSystemPrompt() {
        setupEventListeners();
        loadSystemPromptList();
    }

    function setupEventListeners() {
        // Create button - using event delegation for consistency
        Utils.on('#btnCreateSystemPrompt', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => openCreateModal());
        });

        // Search input with debounce
        const searchInput = document.querySelector('#txtSearchSystemPrompt');
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentKeyword = e.target.value.trim();
                    loadSystemPromptList(currentKeyword, currentIsActive, 1);
                }, 500); // 500ms debounce
            });
        }

        // Status filter
        const statusSelect = document.querySelector('#selectIsActive');
        if (statusSelect) {
            statusSelect.addEventListener('change', function (e) {
                currentIsActive = parseInt(e.target.value);
                loadSystemPromptList(currentKeyword, currentIsActive, 1);
            });
        }

        // Event delegation for dynamically loaded buttons
        document.addEventListener('click', function (e) {
            // Edit button
            if (e.target.closest('.btn-edit-system-prompt')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-edit-system-prompt');
                const id = btn.getAttribute('data-id');
                openEditModal(parseInt(id));
            }

            // Delete button
            if (e.target.closest('.btn-delete-system-prompt')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-delete-system-prompt');
                const id = btn.getAttribute('data-id');
                deleteSystemPrompt(parseInt(id));
            }

            // Pagination links - using closest() for consistency
            const pageLink = e.target.closest('.page-link');
            if (pageLink && !pageLink.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(pageLink.getAttribute('data-page'));
                if (page > 0) {
                    loadSystemPromptList(currentKeyword, currentIsActive, page);
                }
            }
        });
    }

    // ========== LOAD SYSTEM PROMPT LIST (PARTIAL VIEW) ==========
    async function loadSystemPromptList(keyword = '', isActive = -1, pageIndex = 1) {
        try {
            showLoadingState();
            currentPage = pageIndex;
            currentKeyword = keyword;
            currentIsActive = isActive;

            const params = new URLSearchParams({
                keyword: keyword,
                isActive: isActive,
                pageIndex: pageIndex,
                pageSize: pageSize
            });

            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/GetSystemPrompts?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load system prompts');
            }

            const html = await response.text();

            // Inject HTML into container
            const container = document.querySelector('#data-list-container');
            if (container) {
                container.innerHTML = html;
            }

        } catch (error) {
            console.error('Error loading system prompts:', error);
            showToast('error', 'Đã xảy ra lỗi khi tải danh sách system prompt');
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
            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/GetCreateSystemPromptModal`, {
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
                const submitBtn = document.querySelector('#btnSubmitCreateSystemPrompt');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitCreateSystemPrompt);
                    });
                }

                // Auto-focus on first input
                const nameInput = document.querySelector('#createSystemPromptName');
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

    async function submitCreateSystemPrompt() {
        const form = document.querySelector('#createSystemPromptForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        // Get form data
        const name = document.querySelector('#createSystemPromptName').value.trim();
        const content = document.querySelector('#createSystemPromptContent').value.trim();
        const description = document.querySelector('#createSystemPromptDescription').value.trim();
        const isActive = document.querySelector('#createIsActive').value === 'true';

        // Validate required fields
        if (!name) {
            showToast('warning', 'Vui lòng nhập tên');
            document.querySelector('#createSystemPromptName').focus();
            return;
        }

        if (!content) {
            showToast('warning', 'Vui lòng nhập nội dung');
            document.querySelector('#createSystemPromptContent').focus();
            return;
        }

        try {
            const requestData = {
                Name: name,
                Content: content,
                Description: description || null,
                IsActive: isActive
            };

            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/Create`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                closeCreateModal();
                showToast('success', result.message || 'Tạo system prompt thành công');
                await loadSystemPromptList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', result.message || 'Không thể tạo system prompt. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error creating system prompt:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== EDIT MODAL ==========
    async function openEditModal(id) {
        try {
            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/GetSystemPromptById?id=${id}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load system prompt details');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler with button protection
                const submitBtn = document.querySelector('#btnSubmitEditSystemPrompt');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitEditSystemPrompt);
                    });
                }

                // Auto-focus on first input
                const nameInput = document.querySelector('#editSystemPromptName');
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

    async function submitEditSystemPrompt() {
        const form = document.querySelector('#editSystemPromptForm');
        if (!form) {
            showToast('error', 'Không tìm thấy form');
            return;
        }

        // Get form data
        const id = parseInt(document.querySelector('#editSystemPromptId').value);
        const name = document.querySelector('#editSystemPromptName').value.trim();
        const content = document.querySelector('#editSystemPromptContent').value.trim();
        const description = document.querySelector('#editSystemPromptDescription').value.trim();
        const isActive = document.querySelector('#editIsActive').value === 'true';

        // Validate required fields
        if (!name) {
            showToast('warning', 'Vui lòng nhập tên');
            document.querySelector('#editSystemPromptName').focus();
            return;
        }

        if (!content) {
            showToast('warning', 'Vui lòng nhập nội dung');
            document.querySelector('#editSystemPromptContent').focus();
            return;
        }

        try {
            const requestData = {
                Id: id,
                Name: name,
                Content: content,
                Description: description || null,
                IsActive: isActive
            };

            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/Update`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                closeEditModal();
                showToast('success', result.message || 'Cập nhật system prompt thành công');
                await loadSystemPromptList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', result.message || 'Không thể cập nhật system prompt. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error updating system prompt:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== DELETE ==========
    async function deleteSystemPrompt(id) {
        const result = await Swal.fire({
            title: 'Xác nhận xóa',
            text: 'Bạn có chắc chắn muốn xóa system prompt này? Hành động này không thể hoàn tác.',
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
            const response = await fetch(`${WEB_APP_URL}/SystemPrompt/Delete?id=${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Xóa system prompt thành công');
                await loadSystemPromptList(currentKeyword, currentIsActive, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể xóa system prompt. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error deleting system prompt:', error);
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
