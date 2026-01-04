(function () {
    'use strict';

    let currentPage = 1;
    let currentRatings = 0; // 0 = initial (default), 1 = like, 2 = dislike, null = all
    const pageSize = 10;

    document.addEventListener('DOMContentLoaded', function () {
        initializeDashboard();
    });

    function initializeDashboard() {
        setupEventListeners();
        loadInitialData();
    }

    function setupEventListeners() {
        // Filter dropdown change
        const filterSelect = document.querySelector('#filter-rating');
        if (filterSelect) {
            filterSelect.addEventListener('change', function (e) {
                currentRatings = e.target.value === '' ? null : parseInt(e.target.value);
                currentPage = 1; // Reset to page 1 on filter change
                loadFeedbackList();
            });
        }

        // Event delegation for pagination
        document.addEventListener('click', function (e) {
            if (e.target.classList.contains('page-link') &&
                !e.target.parentElement.classList.contains('disabled')) {
                e.preventDefault();
                const page = parseInt(e.target.getAttribute('data-page'));
                if (page > 0) {
                    currentPage = page;
                    loadFeedbackList();
                }
            }
        });
    }

    // 3 PARALLEL AJAX CALLS ON LOAD
    async function loadInitialData() {
        try {
            const [messageCount, ratingCounts, _] = await Promise.all([
                fetchMessageCount(),
                fetchRatingCounts(),
                loadFeedbackList() // Default: ratings=0 (Initial), page 1
            ]);

            updateMessageCount(messageCount);
            updateRatingCounts(ratingCounts);
        } catch (error) {
            console.error('Error loading initial data:', error);
            showError('Đã xảy ra lỗi khi tải dữ liệu dashboard');
        }
    }

    async function fetchMessageCount() {
        const response = await fetch('/Stat/GetMessageCount');
        if (!response.ok) throw new Error('Failed to fetch message count');

        const result = await response.json();
        if (!result.success) throw new Error(result.message);

        return result.data;
    }

    function updateMessageCount(count) {
        const element = document.querySelector('#total-messages-value');
        if (element) {
            element.textContent = count.toLocaleString('vi-VN');
        }
    }

    async function fetchRatingCounts() {
        const response = await fetch('/Stat/GetRatingCounts');
        if (!response.ok) throw new Error('Failed to fetch rating counts');

        const result = await response.json();
        if (!result.success) throw new Error(result.message);

        return result.data; // { Likes, Dislikes }
    }

    function updateRatingCounts(counts) {
        const total = counts.Likes + counts.Dislikes;
        const percentage = total > 0 ? Math.round((counts.Likes / total) * 100) : 50;

        // Update total ratings stat
        const totalElement = document.querySelector('#total-ratings-value');
        if (totalElement) {
            totalElement.textContent = total.toLocaleString('vi-VN');
        }

        // Update pie chart
        updatePieChart(percentage);
    }

    function updatePieChart(percentage) {
        const chartElement = document.querySelector('#pie-chart');
        if (chartElement) {
            // Update CSS conic-gradient
            const successColor = '#198754';
            const dangerColor = '#dc3545';
            chartElement.style.background =
                `conic-gradient(${successColor} 0% ${percentage}%, ${dangerColor} ${percentage}% 100%)`;

            // Update data-percentage attribute for ::after content
            chartElement.setAttribute('data-percentage', percentage + '%');
        }
    }

    async function loadFeedbackList() {
        try {
            showLoadingState();

            const params = new URLSearchParams({
                pageIndex: currentPage,
                pageSize: pageSize
            });

            if (currentRatings !== null) {
                params.append('ratings', currentRatings);
            }

            const response = await fetch(`/Stat/GetFeedbackList?${params.toString()}`);
            if (!response.ok) throw new Error('Failed to load feedback list');

            const html = await response.text();

            // Inject HTML
            const container = document.querySelector('#feedback-table-container');
            if (container) {
                container.innerHTML = html;

                // Smooth scroll to top of table
                container.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }

        } catch (error) {
            console.error('Error loading feedback list:', error);
            showError('Đã xảy ra lỗi khi tải danh sách phản hồi');
        }
    }

    function showLoadingState() {
        const container = document.querySelector('#feedback-table-container');
        if (!container) return;

        container.innerHTML = `
            <div class="loading-spinner">
                <div style="font-size: 3rem; color: #0d6efd;">⏳</div>
                <p style="margin-top: 15px;">Đang tải dữ liệu...</p>
            </div>
        `;
    }

    function showError(message) {
        if (typeof toastr !== 'undefined') {
            toastr.error(message);
        } else {
            alert(message);
        }
    }

})();
