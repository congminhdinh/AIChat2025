// Chat.js - Handle chat interface logic
(function () {
    'use strict';

    let currentConversationId = null;

    // Wait for DOM to be ready
    document.addEventListener('DOMContentLoaded', function () {
        // Initialize chat interface
        initializeChat();
    });

    async function initializeChat() {
        // Load conversation list on page load
        await loadConversations();

        // Setup event listeners
        setupEventListeners();
    }

    function setupEventListeners() {
        // Send message on button click
        const sendButton = document.querySelector('.btn-send');
        if (sendButton) {
            sendButton.addEventListener('click', async function (e) {
                e.preventDefault();
                await handleSendMessage();
            });
        }

        // Send message on Enter key (without Shift)
        const messageInput = document.querySelector('.chat-input');
        if (messageInput) {
            messageInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendMessage();
                }
            });
        }
    }

    /**
     * Load and render conversation list
     */
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

    /**
     * Render conversations in the sidebar
     */
    function renderConversations(conversations) {
        const historyList = document.querySelector('.history-list');
        if (!historyList) return;

        // Clear existing items
        historyList.innerHTML = '';

        // Render each conversation
        conversations.forEach(function (conversation) {
            const item = document.createElement('div');
            item.className = 'history-item';
            item.dataset.conversationId = conversation.id;
            item.textContent = conversation.title || 'New Conversation';

            // Add click handler
            item.addEventListener('click', function () {
                handleConversationClick(conversation.id);
            });

            historyList.appendChild(item);
        });

        // Auto-select first conversation if available
        if (conversations.length > 0 && !currentConversationId) {
            handleConversationClick(conversations[0].id);
        }
    }

    /**
     * Handle conversation item click
     */
    async function handleConversationClick(conversationId) {
        // Update active state
        document.querySelectorAll('.history-item').forEach(function (item) {
            item.classList.remove('active');
        });

        const clickedItem = document.querySelector(`[data-conversation-id="${conversationId}"]`);
        if (clickedItem) {
            clickedItem.classList.add('active');
        }

        // Set current conversation
        currentConversationId = conversationId;

        // Load message history
        await loadMessageHistory(conversationId);
    }

    /**
     * Load message history for a conversation
     */
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

    /**
     * Render messages in the chat content area
     */
    function renderMessages(messages) {
        const chatContent = document.querySelector('.chat-content');
        if (!chatContent) return;

        // Clear existing messages
        chatContent.innerHTML = '';

        // Render each message
        messages.forEach(function (message) {
            appendMessage(message.content, message.type, false);
        });

        // Scroll to bottom
        scrollToBottom();
    }

    /**
     * Append a message to the chat
     * @param {string} content - Message content
     * @param {number} type - 0 for User, 1 for Bot
     * @param {boolean} animate - Whether to animate the message appearance
     */
    function appendMessage(content, type, animate = true) {
        const chatContent = document.querySelector('.chat-content');
        if (!chatContent) return;

        const messageDiv = document.createElement('div');
        messageDiv.className = type === 0 ? 'message user' : 'message bot';

        const avatarDiv = document.createElement('div');
        avatarDiv.className = type === 0 ? 'avatar user' : 'avatar bot';
        avatarDiv.textContent = type === 0 ? '👤' : '✨';

        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'msg-bubble';
        bubbleDiv.textContent = content;

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

    /**
     * Handle sending a message
     */
    async function handleSendMessage() {
        const messageInput = document.querySelector('.chat-input');
        if (!messageInput) return;

        const content = messageInput.value.trim();

        // Validate input
        if (!content) {
            return;
        }

        if (!currentConversationId) {
            console.error('No conversation selected');
            return;
        }

        // Clear input immediately
        messageInput.value = '';

        // Append user message to chat (Type 0 = User)
        appendMessage(content, 0, true);

        // Show loading indicator (optional)
        showLoadingIndicator();

        try {
            // Send message to API
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

            // Remove loading indicator
            removeLoadingIndicator();

            if (result.success && result.data) {
                // Append bot response (Type 1 = Bot)
                appendMessage(result.data.content, 1, true);
            } else {
                console.error('Failed to send message:', result.message);
                showError(result.message || 'Không thể gửi tin nhắn. Vui lòng thử lại.');
            }
        } catch (error) {
            console.error('Error sending message:', error);
            removeLoadingIndicator();
            showError('Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        }
    }

    /**
     * Show loading indicator in chat
     */
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

    /**
     * Remove loading indicator
     */
    function removeLoadingIndicator() {
        const loadingMessage = document.querySelector('.loading-message');
        if (loadingMessage) {
            loadingMessage.remove();
        }
    }

    /**
     * Scroll chat to bottom
     */
    function scrollToBottom() {
        const chatContent = document.querySelector('.chat-content');
        if (chatContent) {
            chatContent.scrollTop = chatContent.scrollHeight;
        }
    }

    /**
     * Show error message
     */
    function showError(message) {
        // You can customize this to show errors in a toast or alert
        console.error(message);

        // Optional: Show error in chat as a system message
        const chatContent = document.querySelector('.chat-content');
        if (chatContent) {
            const errorDiv = document.createElement('div');
            errorDiv.className = 'message system-error';
            errorDiv.style.cssText = 'text-align: center; color: #c33; padding: 10px; font-size: 0.9em;';
            errorDiv.textContent = message;
            chatContent.appendChild(errorDiv);

            // Remove error after 5 seconds
            setTimeout(function () {
                errorDiv.remove();
            }, 5000);
        }
    }
})();
