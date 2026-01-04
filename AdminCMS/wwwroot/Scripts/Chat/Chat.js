(function () {
    'use strict';

    let currentConversationId = null;
    let hubConnection = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeChat();
    });

    async function initializeChat() {
        await loadConversations();
        setupEventListeners();
        await setupSignalR();
    }

    async function setupSignalR() {
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
            appendMessage(messageDto.content, messageDto.type, true);
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
            appendMessage(message.content, message.type, false);
        });

        scrollToBottom();
    }

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

    window.closeCreateModal = closeCreateModal;
    window.submitCreateConversation = submitCreateConversation;
})();
