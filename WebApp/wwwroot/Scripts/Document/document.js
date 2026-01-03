(function () {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;

    document.addEventListener('DOMContentLoaded', function () {
        initializeDocument();
    });

    function initializeDocument() {
        setupEventListeners();
        loadDocuments();
    }

    function setupEventListeners() {
        const uploadButton = document.querySelector('#btn-upload-document');
        if (uploadButton) {
            uploadButton.addEventListener('click', async function (e) {
                e.preventDefault();
                await openUploadModal();
            });
        }

        const refreshButton = document.querySelector('#btn-refresh-documents');
        if (refreshButton) {
            refreshButton.addEventListener('click', function (e) {
                e.preventDefault();
                loadDocuments();
            });
        }
    }

    async function loadDocuments(pageIndex = 1) {
        try {
            currentPage = pageIndex;

            const params = new URLSearchParams({
                pageIndex: currentPage,
                pageSize: pageSize
            });

            const response = await fetch(`/Document/GetDocuments?${params.toString()}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success && result.data) {
                renderDocuments(result.data);
            } else {
                console.error('Failed to load documents:', result.message);
                showError(result.message || 'Không thể tải danh sách tài liệu');
            }
        } catch (error) {
            console.error('Error loading documents:', error);
            showError('Đã xảy ra lỗi khi tải danh sách tài liệu');
        }
    }

    function renderDocuments(paginatedData) {
        const tableBody = document.querySelector('#documents-table-body');
        if (!tableBody) return;

        tableBody.innerHTML = '';

        if (!paginatedData.items || paginatedData.items.length === 0) {
            const row = document.createElement('tr');
            row.innerHTML = '<td colspan="7" class="text-center">Không có tài liệu nào</td>';
            tableBody.appendChild(row);
            return;
        }

        paginatedData.items.forEach(function (doc) {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${doc.id}</td>
                <td>${doc.fileName}</td>
                <td>${getActionText(doc.action)}</td>
                <td>${doc.isApproved ? '<span class="badge bg-success">Đã duyệt</span>' : '<span class="badge bg-warning">Chưa duyệt</span>'}</td>
                <td>${doc.uploadedBy || 'N/A'}</td>
                <td>${formatDate(doc.createdAt)}</td>
                <td>
                    <button class="btn btn-sm btn-primary" onclick="window.vectorizeDocument(${doc.id})">
                        Vectorize
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="window.deleteDocument(${doc.id})">
                        Xóa
                    </button>
                </td>
            `;
            tableBody.appendChild(row);
        });

        renderPagination(paginatedData);
    }

    function renderPagination(paginatedData) {
        const paginationContainer = document.querySelector('#pagination-container');
        if (!paginationContainer) return;

        paginationContainer.innerHTML = '';

        const totalPages = paginatedData.totalPages;
        const currentPageIndex = paginatedData.pageIndex;

        if (totalPages <= 1) return;

        const ul = document.createElement('ul');
        ul.className = 'pagination';

        // Previous button
        const prevLi = document.createElement('li');
        prevLi.className = `page-item ${currentPageIndex === 1 ? 'disabled' : ''}`;
        prevLi.innerHTML = `<a class="page-link" href="#">Trước</a>`;
        if (currentPageIndex > 1) {
            prevLi.addEventListener('click', function (e) {
                e.preventDefault();
                loadDocuments(currentPageIndex - 1);
            });
        }
        ul.appendChild(prevLi);

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            const li = document.createElement('li');
            li.className = `page-item ${i === currentPageIndex ? 'active' : ''}`;
            li.innerHTML = `<a class="page-link" href="#">${i}</a>`;
            li.addEventListener('click', function (e) {
                e.preventDefault();
                loadDocuments(i);
            });
            ul.appendChild(li);
        }

        // Next button
        const nextLi = document.createElement('li');
        nextLi.className = `page-item ${currentPageIndex === totalPages ? 'disabled' : ''}`;
        nextLi.innerHTML = `<a class="page-link" href="#">Sau</a>`;
        if (currentPageIndex < totalPages) {
            nextLi.addEventListener('click', function (e) {
                e.preventDefault();
                loadDocuments(currentPageIndex + 1);
            });
        }
        ul.appendChild(nextLi);

        paginationContainer.appendChild(ul);
    }

    function getActionText(action) {
        const actions = {
            0: 'Upload',
            1: 'Standardization',
            2: 'Vectorize Start',
            3: 'Vectorize Success',
            4: 'Vectorize Failed',
            5: 'Update Metadata'
        };
        return actions[action] || 'Unknown';
    }

    function formatDate(dateString) {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleString('vi-VN');
    }

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
                    setTimeout(function () {
                        inputField.focus();
                    }, 100);
                }
            }
        } catch (error) {
            console.error('Error opening upload modal:', error);
            showError('Không thể mở form tải lên tài liệu.');
        }
    }

    function closeUploadModal() {
        const modalOverlay = document.querySelector('#modal-overlay');
        if (modalOverlay) {
            modalOverlay.classList.remove('active');

            setTimeout(function () {
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

        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            alert('Vui lòng chọn tệp để tải lên');
            return;
        }

        const file = fileInput.files[0];
        const documentName = nameInput ? nameInput.value.trim() : '';

        const formData = new FormData();
        formData.append('file', file);
        if (documentName) {
            formData.append('documentName', documentName);
        }

        try {
            const response = await fetch('/Document/Upload', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                closeUploadModal();
                showSuccess(result.message || 'Tải lên tài liệu thành công');
                await loadDocuments(currentPage);
            } else {
                alert(result.message || 'Không thể tải lên tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error uploading document:', error);
            alert('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    async function vectorizeDocument(documentId) {
        if (!confirm('Bạn có chắc chắn muốn vectorize tài liệu này?')) {
            return;
        }

        try {
            const response = await fetch(`/Document/Vectorize?id=${documentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success) {
                showSuccess(result.message || 'Đã gửi yêu cầu vectorize tài liệu');
                await loadDocuments(currentPage);
            } else {
                alert(result.message || 'Không thể vectorize tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error vectorizing document:', error);
            alert('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    async function deleteDocument(documentId) {
        if (!confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) {
            return;
        }

        try {
            const response = await fetch(`/Document/Delete?id=${documentId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success) {
                showSuccess(result.message || 'Xóa tài liệu thành công');
                await loadDocuments(currentPage);
            } else {
                alert(result.message || 'Không thể xóa tài liệu. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error deleting document:', error);
            alert('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    function showError(message) {
        console.error(message);
        alert(message);
    }

    function showSuccess(message) {
        console.log(message);
        alert(message);
    }

    window.closeUploadModal = closeUploadModal;
    window.submitUpload = submitUpload;
    window.vectorizeDocument = vectorizeDocument;
    window.deleteDocument = deleteDocument;
})();
