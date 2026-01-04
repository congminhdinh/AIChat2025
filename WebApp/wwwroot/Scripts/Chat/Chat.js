(function () {
    'use strict';

    let currentConversationId = null;
    let hubConnection = null;
    let signalRRetryCount = 0;
    const MAX_SIGNALR_RETRIES = 10;

    document.addEventListener('DOMContentLoaded', function () {
        initializeChat();
    });

    async function initializeChat() {
        await loadConversations();
        setupEventListeners();
        await setupSignalR();
    }

    async function setupSignalR() {
        // Check if signalR is loaded
        if (typeof signalR === 'undefined') {
            signalRRetryCount++;

            if (signalRRetryCount > MAX_SIGNALR_RETRIES) {
                console.error('SignalR library failed to load after multiple retries. Please refresh the page.');
                showError('Không thể tải thư viện SignalR. Vui lòng refresh trang.');
                return;
            }

            console.warn(`SignalR library not loaded yet. Retry ${signalRRetryCount}/${MAX_SIGNALR_RETRIES}...`);
            setTimeout(() => setupSignalR(), 500);
            return;
        }

        console.log('SignalR library loaded successfully. Initializing connection...');

        const apiGatewayUrl = window.location.origin.replace(/:\d+/, ':7235');

        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiGatewayUrl}/hubs/chat`, {
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        hubConnection.on('BotResponse', function (messageDto) {
            console.log('Received BotResponse:', messageDto);

            if (messageDto.conversationId !== currentConversationId) {
                console.warn(`Received message for conversation ${messageDto.conversationId}, but current is ${currentConversationId}. Ignoring.`);
                return;
            }

            removeLoadingIndicator();
            appendMessage(messageDto.content, messageDto.type, true, messageDto.id);
        });

        hubConnection.onreconnecting(function (error) {
            console.warn('SignalR reconnecting...', error);
            showError('Đang kết nối lại...');
        });

        hubConnection.onreconnected(function (connectionId) {
            console.log('SignalR reconnected:', connectionId);
            if (currentConversationId) {
                hubConnection.invoke('JoinConversation', currentConversationId)
                    .catch(err => console.error('Error rejoining conversation:', err));
            }
        });

        hubConnection.onclose(function (error) {
            console.error('SignalR connection closed:', error);
            showError('Mất kết nối. Đang thử kết nối lại...');
            setTimeout(() => startSignalRConnection(), 5000);
        });

        await startSignalRConnection();
    }

    async function startSignalRConnection() {
        try {
            await hubConnection.start();
            console.log('SignalR connected successfully');

            if (currentConversationId) {
                await hubConnection.invoke('JoinConversation', currentConversationId);
                console.log(`Joined conversation ${currentConversationId}`);
            }
        } catch (error) {
            console.error('Error starting SignalR:', error);
            showError('Không thể kết nối đến server. Thử lại sau 5 giây...');
            setTimeout(() => startSignalRConnection(), 5000);
        }
    }

    function setupEventListeners() {
        const sendButton = document.querySelector('.btn-send');
        if (sendButton) {
            sendButton.addEventListener('click', async function (e) {
                e.preventDefault();
                await handleSendMessage();
            });
        }

        const messageInput = document.querySelector('.chat-input');
        if (messageInput) {
            messageInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendMessage();
                }
            });
        }

        const newChatButton = document.querySelector('#btn-new-chat');
        if (newChatButton) {
            newChatButton.addEventListener('click', async function (e) {
                e.preventDefault();
                await openCreateModal();
            });
        }
    }

    async function loadConversations() {
        try {
            const response = await fetch('/Chat/GetConversations', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success && result.data) {
                renderConversations(result.data);
            } else {
                console.error('Failed to load conversations:', result.message);
            }
        } catch (error) {
            console.error('Error loading conversations:', error);
        }
    }

    function renderConversations(conversations) {
        const historyList = document.querySelector('.history-list');
        if (!historyList) return;

        historyList.innerHTML = '';

        conversations.forEach(function (conversation) {
            const item = document.createElement('div');
            item.className = 'history-item';
            item.dataset.conversationId = conversation.id;
            item.textContent = conversation.title || 'New Conversation';

            item.addEventListener('click', function () {
                handleConversationClick(conversation.id);
            });

            historyList.appendChild(item);
        });

        if (conversations.length > 0 && !currentConversationId) {
            handleConversationClick(conversations[0].id);
        }
    }

    async function handleConversationClick(conversationId) {
        document.querySelectorAll('.history-item').forEach(function (item) {
            item.classList.remove('active');
        });

        const clickedItem = document.querySelector(`[data-conversation-id="${conversationId}"]`);
        if (clickedItem) {
            clickedItem.classList.add('active');
        }

        if (currentConversationId && hubConnection && hubConnection.state === signalR.HubConnectionState.Connected) {
            await hubConnection.invoke('LeaveConversation', currentConversationId);
        }

        currentConversationId = conversationId;

        if (hubConnection && hubConnection.state === signalR.HubConnectionState.Connected) {
            await hubConnection.invoke('JoinConversation', conversationId);
        }

        await loadMessageHistory(conversationId);
    }

    async function loadMessageHistory(conversationId) {
        try {
            const response = await fetch(`/Chat/GetConversation?id=${conversationId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success && result.data && result.data.messages) {
                renderMessages(result.data.messages);
            } else {
                console.error('Failed to load message history:', result.message);
            }
        } catch (error) {
            console.error('Error loading message history:', error);
        }
    }

    function renderMessages(messages) {
        const chatContent = document.querySelector('.chat-content');
        if (!chatContent) return;

        chatContent.innerHTML = '';

        messages.forEach(function (message) {
            appendMessage(message.content, message.type, false, message.id);
        });

        scrollToBottom();
    }

    function appendMessage(content, type, animate = true, messageId = null) {
        const chatContent = document.querySelector('.chat-content');
        if (!chatContent) return;

        const messageDiv = document.createElement('div');
        messageDiv.className = type === 0 ? 'message user' : 'message bot';

        // Store messageId in bot messages for feedback
        if (type !== 0 && messageId) {
            messageDiv.dataset.messageId = messageId;
        }

        const avatarDiv = document.createElement('div');
        avatarDiv.className = type === 0 ? 'avatar user' : 'avatar bot';
        avatarDiv.textContent = type === 0 ? '👤' : '✨';

        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'msg-bubble';
        bubbleDiv.textContent = content;

        // Add action buttons for bot messages
        if (type !== 0) {
            const footerDiv = document.createElement('div');
            footerDiv.className = 'msg-footer';
            footerDiv.innerHTML = `
                <button class="action-btn btn-like" title="Like" onclick="handleLike(this)">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"></path>
                    </svg>
                </button>
                <button class="action-btn btn-dislike" title="Dislike" onclick="handleDislike(this)">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M10 15v4a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3zm7-13h2.67A2.31 2.31 0 0 1 22 4v7a2.31 2.31 0 0 1-2.33 2H17"></path>
                    </svg>
                </button>
                <button class="action-btn btn-comment" title="Comment" onclick="handleComment(this)">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
                    </svg>
                </button>
            `;
            bubbleDiv.appendChild(footerDiv);
        }

        messageDiv.appendChild(avatarDiv);
        messageDiv.appendChild(bubbleDiv);

        if (animate) {
            messageDiv.style.opacity = '0';
            messageDiv.style.transform = 'translateY(10px)';
        }

        chatContent.appendChild(messageDiv);

        if (animate) {
            setTimeout(function () {
                messageDiv.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                messageDiv.style.opacity = '1';
                messageDiv.style.transform = 'translateY(0)';
            }, 10);
        }

        scrollToBottom();
    }

    async function handleSendMessage() {
        const messageInput = document.querySelector('.chat-input');
        if (!messageInput) return;

        const content = messageInput.value.trim();

        if (!content) {
            return;
        }

        if (!currentConversationId) {
            console.error('No conversation selected');
            return;
        }

        messageInput.value = '';

        appendMessage(content, 0, true);

        showLoadingIndicator();

        try {
            const response = await fetch('/Chat/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    conversationId: currentConversationId,
                    message: content
                })
            });

            const result = await response.json();

            if (!result.success) {
                console.error('Failed to send message:', result.message);
                removeLoadingIndicator();
                showError(result.message || 'Không thể gửi tin nhắn. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error sending message:', error);
            removeLoadingIndicator();
            showError('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    function showLoadingIndicator() {
        const chatContent = document.querySelector('.chat-content');
        if (!chatContent) return;

        const loadingDiv = document.createElement('div');
        loadingDiv.className = 'message bot loading-message';
        loadingDiv.innerHTML = `
            <div class="avatar bot">✨</div>
            <div class="msg-bubble">
                <span class="loading-dots">
                    <span>.</span><span>.</span><span>.</span>
                </span>
            </div>
        `;

        chatContent.appendChild(loadingDiv);
        scrollToBottom();
    }

    function removeLoadingIndicator() {
        const loadingMessage = document.querySelector('.loading-message');
        if (loadingMessage) {
            loadingMessage.remove();
        }
    }

    function scrollToBottom() {
        const chatContent = document.querySelector('.chat-content');
        if (chatContent) {
            chatContent.scrollTop = chatContent.scrollHeight;
        }
    }

    function showError(message) {
        console.error(message);

        const chatContent = document.querySelector('.chat-content');
        if (chatContent) {
            const errorDiv = document.createElement('div');
            errorDiv.className = 'message system-error';
            errorDiv.style.cssText = 'text-align: center; color: #c33; padding: 10px; font-size: 0.9em;';
            errorDiv.textContent = message;
            chatContent.appendChild(errorDiv);

            setTimeout(function () {
                errorDiv.remove();
            }, 5000);
        }
    }

    async function openCreateModal() {
        try {
            const response = await fetch('/Chat/CreateConversationPartial', {
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

                const inputField = document.querySelector('#newConversationName');
                if (inputField) {
                    setTimeout(function () {
                        inputField.focus();
                    }, 100);
                }
            }
        } catch (error) {
            console.error('Error opening create modal:', error);
            showError('Không thể mở form tạo cuộc trò chuyện.');
        }
    }

    function closeCreateModal() {
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

    async function submitCreateConversation() {
        const inputField = document.querySelector('#newConversationName');
        if (!inputField) return;

        const title = inputField.value.trim();

        if (!title) {
            alert('Vui lòng nhập tên cuộc trò chuyện');
            inputField.focus();
            return;
        }

        try {
            const response = await fetch('/Chat/CreateConversation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    title: title
                })
            });

            const result = await response.json();

            if (result.success) {
                closeCreateModal();

                await loadConversations();

                if (result.data && result.data.id) {
                    currentConversationId = result.data.id;
                    await handleConversationClick(result.data.id);
                }
            } else {
                alert(result.message || 'Không thể tạo cuộc trò chuyện. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error creating conversation:', error);
            alert('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    // Feedback action handlers
    async function handleLike(button) {
        const messageDiv = button.closest('.message.bot');
        if (!messageDiv) return;

        const messageId = messageDiv.dataset.messageId;
        if (!messageId) {
            console.error('Message ID not found');
            return;
        }

        try {
            const response = await fetch('/Chat/RateChatFeedback', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    MessageId: parseInt(messageId),
                    Ratings: 1
                })
            });

            const result = await response.json();

            if (result.success) {
                // Toggle active state
                const likeBtn = messageDiv.querySelector('.btn-like');
                const dislikeBtn = messageDiv.querySelector('.btn-dislike');

                if (likeBtn.classList.contains('active')) {
                    likeBtn.classList.remove('active');
                } else {
                    likeBtn.classList.add('active');
                    dislikeBtn.classList.remove('active');
                }
            } else {
                console.error('Failed to rate:', result.message);
            }
        } catch (error) {
            console.error('Error rating message:', error);
        }
    }

    async function handleDislike(button) {
        const messageDiv = button.closest('.message.bot');
        if (!messageDiv) return;

        const messageId = messageDiv.dataset.messageId;
        if (!messageId) {
            console.error('Message ID not found');
            return;
        }

        try {
            const response = await fetch('/Chat/RateChatFeedback', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    MessageId: parseInt(messageId),
                    Ratings: 2
                })
            });

            const result = await response.json();

            if (result.success) {
                // Toggle active state
                const likeBtn = messageDiv.querySelector('.btn-like');
                const dislikeBtn = messageDiv.querySelector('.btn-dislike');

                if (dislikeBtn.classList.contains('active')) {
                    dislikeBtn.classList.remove('active');
                } else {
                    dislikeBtn.classList.add('active');
                    likeBtn.classList.remove('active');
                }
            } else {
                console.error('Failed to rate:', result.message);
            }
        } catch (error) {
            console.error('Error rating message:', error);
        }
    }

    async function handleComment(button) {
        const messageDiv = button.closest('.message.bot');
        if (!messageDiv) return;

        const messageId = messageDiv.dataset.messageId;
        if (!messageId) {
            console.error('Message ID not found');
            return;
        }

        try {
            // Load modal partial
            const modalResponse = await fetch('/Chat/ChatFeedbackPartial', {
                method: 'GET',
                headers: {
                    'Content-Type': 'text/html'
                }
            });

            if (!modalResponse.ok) {
                throw new Error('Failed to load modal content');
            }

            const html = await modalResponse.text();

            const modalContainer = document.querySelector('#modal-content-container');
            const modalOverlay = document.querySelector('#modal-overlay');

            if (modalContainer && modalOverlay) {
                modalContainer.innerHTML = html;
                modalOverlay.classList.add('active');

                // Set messageId
                document.querySelector('#feedbackMessageId').value = messageId;

                // Fetch existing feedback data
                const feedbackResponse = await fetch(`/Chat/GetChatFeedback?messageId=${messageId}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                const feedbackResult = await feedbackResponse.json();

                if (feedbackResult.success && feedbackResult.data) {
                    const feedback = feedbackResult.data;
                    document.querySelector('#feedbackId').value = feedback.id || '';
                    document.querySelector('#feedbackCategory').value = feedback.category || '1';
                    document.querySelector('#feedbackContent').value = feedback.content || '';
                }

                // Auto-focus textarea
                setTimeout(function () {
                    const textarea = document.querySelector('#feedbackContent');
                    if (textarea) {
                        textarea.focus();
                    }
                }, 100);
            }
        } catch (error) {
            console.error('Error opening feedback modal:', error);
            showError('Không thể mở form phản hồi.');
        }
    }

    function closeFeedbackModal() {
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

    async function submitFeedback() {
        const messageId = document.querySelector('#feedbackMessageId').value;
        const feedbackId = document.querySelector('#feedbackId').value;
        const category = parseInt(document.querySelector('#feedbackCategory').value);
        const content = document.querySelector('#feedbackContent').value.trim();

        if (!content) {
            alert('Vui lòng nhập nội dung phản hồi');
            return;
        }

        try {
            let response;

            // If feedbackId exists and category is not Initialized, update. Otherwise create.
            if (feedbackId && category !== 0) {
                response = await fetch('/Chat/UpdateChatFeedback', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        Id: parseInt(feedbackId),
                        Content: content,
                        Category: category
                    })
                });
            } else {
                response = await fetch('/Chat/CreateChatFeedback', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        MessageId: parseInt(messageId),
                        Content: content,
                        Category: category
                    })
                });
            }

            const result = await response.json();

            if (result.success) {
                closeFeedbackModal();
                // Show success message (you can use toastr if available)
                console.log('Feedback submitted successfully');
            } else {
                alert(result.message || 'Không thể gửi phản hồi. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error submitting feedback:', error);
            alert('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    window.closeCreateModal = closeCreateModal;
    window.submitCreateConversation = submitCreateConversation;
    window.handleLike = handleLike;
    window.handleDislike = handleDislike;
    window.handleComment = handleComment;
    window.closeFeedbackModal = closeFeedbackModal;
    window.submitFeedback = submitFeedback;
})();
