(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentKeyword = '';
    let searchTimeout = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeDocument();
    });

    function initializeDocument() {
        setupEventListeners();
        loadDocumentList();
    }

    function setupEventListeners() {
        // Upload button - using event delegation for consistency
        Utils.on('#btn-upload-document', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => openUploadModal());
        });

        // Refresh button - using event delegation for consistency
        Utils.on('#btn-refresh-documents', 'click', function (e) {
            e.preventDefault();
            const btn = this;
            Utils.ButtonProtection.protect(btn, () => loadDocumentList(currentKeyword, 1));
        });

        // Search input with debounce
        const searchInput = document.querySelector('#txtSearchDocument');
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentKeyword = e.target.value.trim();
                    loadDocumentList(currentKeyword, 1);
                }, 500); // 500ms debounce
            });
        }

        // Event delegation for dynamically loaded buttons
        document.addEventListener('click', function (e) {
            // Edit button
            if (e.target.closest('.btn-edit-doc')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-edit-doc');
                const docId = btn.getAttribute('data-doc-id');
                openEditModal(docId);
            }

            // Vectorize button
            if (e.target.closest('.btn-vectorize-doc')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-vectorize-doc');
                const docId = btn.getAttribute('data-doc-id');
                vectorizeDocument(docId);
            }

            // Delete button
            if (e.target.closest('.btn-delete-doc')) {
                e.preventDefault();
                const btn = e.target.closest('.btn-delete-doc');
                const docId = btn.getAttribute('data-doc-id');
                deleteDocument(docId);
            }

            // Pagination links - using closest() for consistency
            const pageLink = e.target.closest('.page-link');
            if (pageLink && !pageLink.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(pageLink.getAttribute('data-page'));
                if (page > 0) {
                    loadDocumentList(currentKeyword, page);
                }
            }
        });
    }

    // ========== LOAD DOCUMENT LIST (PARTIAL VIEW) ==========
    async function loadDocumentList(keyword = '', pageIndex = 1) {
        try {
            showLoadingState();
            currentPage = pageIndex;
            currentKeyword = keyword;

            const params = new URLSearchParams({
                keyword: keyword,
                pageIndex: pageIndex,
                pageSize: pageSize
            });

            const response = await fetch(`/Document/GetDocuments?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load documents');
            }

            const html = await response.text();

            // Inject HTML into container
            const container = document.querySelector('.content-card');
            const pageHeader = container.querySelector('.page-header');

            // Remove old table and pagination
            const oldTable = container.querySelector('.table-container');
            const oldPagination = container.querySelector('#pagination-container');
            if (oldTable) oldTable.remove();
            if (oldPagination) oldPagination.remove();

            // Insert new content after page header
            pageHeader.insertAdjacentHTML('afterend', html);

        } catch (error) {
            console.error('Error loading documents:', error);
            showToast('error', 'Đã xảy ra lỗi khi tải danh sách tài liệu');
        }
    }

    function showLoadingState() {
        const container = document.querySelector('.content-card');
        const pageHeader = container.querySelector('.page-header');

        const oldTable = container.querySelector('.table-container');
        if (oldTable) oldTable.remove();

        const loadingHtml = `
            <div class="table-container">
                <table>
                    <thead>
                        <tr>
                            <th style="width: 50px;">ID</th>
                            <th>Tên tài liệu</th>
                            <th style="width: 150px;">Trạng thái</th>
                            <th style="width: 100px;">Phê duyệt</th>
                            <th>Người tải</th>
                            <th style="width: 180px;">Thời gian tạo</th>
                            <th style="width: 200px;">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody id="documents-table-body">
                        <tr>
                            <td colspan="7" class="text-center">Đang tải...</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        `;

        pageHeader.insertAdjacentHTML('afterend', loadingHtml);
    }

    // ========== UPLOAD MODAL ==========
    async function openUploadModal() {
        try {
            const response = await fetch('/Document/UploadDocumentPartial', {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load modal content');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                const inputField = document.querySelector('#documentFile');
                if (inputField) {
                    setTimeout(() => inputField.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening upload modal:', error);
            showToast('error', 'Không thể mở form tải lên tài liệu.');
        }
    }

    function closeUploadModal() {
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

    async function submitUpload() {
        const fileInput = document.querySelector('#documentFile');
        const nameInput = document.querySelector('#documentName');
        const docTypeSelect = document.querySelector('#documentType');
        const fatherDocIdInput = document.querySelector('#fatherDocumentId');

        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            showToast('warning', 'Vui lòng chọn tệp để tải lên');
            return;
        }

        const file = fileInput.files[0];
        const documentName = nameInput ? nameInput.value.trim() : '';
        const documentType = docTypeSelect ? parseInt(docTypeSelect.value) : 1;
        const fatherDocumentId = fatherDocIdInput && fatherDocIdInput.value ? parseInt(fatherDocIdInput.value) : -1;

        // Validate fatherDocumentId if document type is NghiDinh
        if (documentType === 2 && fatherDocumentId <= 0) {
            showToast('warning', 'Vui lòng nhập ID tài liệu Luật cho Nghị Định');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);
        if (documentName) {
            formData.append('documentName', documentName);
        }
        formData.append('documentType', documentType);
        formData.append('fatherDocumentId', fatherDocumentId);

        try {
            const response = await fetch('/Document/Upload', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeUploadModal();
                showToast('success', result.message || 'Tải lên tài liệu thành công');
                await loadDocumentList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể tải lên tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error uploading document:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== EDIT MODAL ==========
    async function openEditModal(documentId) {
        try {
            const response = await fetch(`/Document/GetDocumentById?id=${documentId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load document details');
            }

            const html = await response.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Attach submit handler with button protection
                const submitBtn = document.querySelector('#btnSubmitEditDocument');
                if (submitBtn) {
                    submitBtn.addEventListener('click', function(e) {
                        e.preventDefault();
                        Utils.ButtonProtection.protect(submitBtn, submitEditDocument);
                    });
                }

                const inputField = document.querySelector('#editDocumentName');
                if (inputField) {
                    setTimeout(() => inputField.focus(), 100);
                }
            }
        } catch (error) {
            console.error('Error opening edit modal:', error);
            showToast('error', 'Không thể mở form chỉnh sửa tài liệu.');
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

    async function submitEditDocument() {
        const documentIdInput = document.querySelector('#editDocumentId');
        const documentNameInput = document.querySelector('#editDocumentName');

        if (!documentIdInput || !documentNameInput) {
            showToast('error', 'Không tìm thấy thông tin tài liệu');
            return;
        }

        const documentId = parseInt(documentIdInput.value);
        const documentName = documentNameInput.value.trim();

        if (!documentName) {
            showToast('warning', 'Vui lòng nhập tên tài liệu');
            return;
        }

        try {
            const response = await fetch('/Document/Edit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    documentId: documentId,
                    documentName: documentName
                    // DocType and FatherDocumentId will be hardcoded server-side
                })
            });

            const result = await response.json();

            if (result.success) {
                closeEditModal();
                showToast('success', result.message || 'Cập nhật tài liệu thành công');
                await loadDocumentList(currentKeyword, currentPage);
            } else {
                showToast('error', result.message || 'Không thể cập nhật tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error updating document:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== VECTORIZE ==========
    async function vectorizeDocument(documentId) {
        const result = await Swal.fire({
            title: 'Xác nhận vectorize',
            text: 'Bạn có chắc chắn muốn nạp dữ liệu cho tài liệu này?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#0d6efd',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Xác nhận',
            cancelButtonText: 'Hủy bỏ'
        });

        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`/Document/Vectorize?id=${documentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Đã gửi yêu cầu nạp dữ liệu cho tài liệu');
                await loadDocumentList(currentKeyword, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể nạp dữ liệu cho tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error vectorizing document:', error);
            showToast('error', 'Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // ========== DELETE ==========
    async function deleteDocument(documentId) {
        const result = await Swal.fire({
            title: 'Xác nhận xóa',
            text: 'Bạn có chắc chắn muốn xóa tài liệu này? Hành động này không thể hoàn tác.',
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
            const response = await fetch(`/Document/Delete?id=${documentId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const responseData = await response.json();

            if (responseData.success) {
                showToast('success', responseData.message || 'Xóa tài liệu thành công');
                await loadDocumentList(currentKeyword, currentPage);
            } else {
                showToast('error', responseData.message || 'Không thể xóa tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error deleting document:', error);
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

    // Expose functions to global scope for inline onclick handlers with button protection
    window.closeUploadModal = closeUploadModal;
    window.submitUpload = function() {
        const submitBtn = document.querySelector('#btnSubmitUpload');
        if (submitBtn) {
            Utils.ButtonProtection.protect(submitBtn, submitUpload);
        } else {
            submitUpload();
        }
    };
    window.closeEditModal = closeEditModal;
})();
