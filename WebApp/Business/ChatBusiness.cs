using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Models.Chat;

namespace WebApp.Business
{
    public class ChatBusiness : BaseHttpClient
    {
        private readonly ICurrentUserProvider _currentUserProvider;

        public ChatBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, ICurrentUserProvider currentUserProvider)
            : base(httpClient, appLogger)
        {
            _currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        public async Task<BaseResponse<List<ConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = _currentUserProvider.Token;
                var response = await GetWithTokenAsync<BaseResponse<List<ConversationDto>>>(
                    "/web-api/chat/conversations/list",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<List<ConversationDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get conversations: {ex.Message}");
                return new BaseResponse<List<ConversationDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get conversations: {ex.Message}");
                return new BaseResponse<List<ConversationDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách hội thoại"
                };
            }
        }

        /// <summary>
        /// Get message history for a specific conversation
        /// </summary>
        public async Task<BaseResponse<List<MessageDto>>> GetMessageHistoryAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = _currentUserProvider.Token;
                var response = await GetWithTokenAsync<BaseResponse<List<MessageDto>>>(
                    $"/web-api/chat/conversations/{conversationId}/messages",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<List<MessageDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get message history: {ex.Message}");
                return new BaseResponse<List<MessageDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get message history: {ex.Message}");
                return new BaseResponse<List<MessageDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải lịch sử tin nhắn"
                };
            }
        }

        /// <summary>
        /// Send a new message and get bot response
        /// </summary>
        public async Task<BaseResponse<MessageDto>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = _currentUserProvider.Token;
                var response = await PostWithTokenAsync<SendMessageRequest, BaseResponse<MessageDto>>(
                    "/web-api/chat/messages",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<MessageDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during send message: {ex.Message}");
                return new BaseResponse<MessageDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during send message: {ex.Message}");
                return new BaseResponse<MessageDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi gửi tin nhắn"
                };
            }
        }
    }
}
