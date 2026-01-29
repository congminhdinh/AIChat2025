using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Helpers;
using WebApp.Models.Chat;

namespace WebApp.Business
{
    public class ChatBusiness : BaseHttpClient
    {
        private readonly IdentityHelper _identityHelper;

        public ChatBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<ConversationDto>> CreateConversationAsync(CreateConversationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<ConversationDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PostWithTokenAsync<CreateConversationRequest, BaseResponse<ConversationDto>>(
                    "/web-api/chat/conversations",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<ConversationDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi tạo hội thoại"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during create conversation: {ex.Message}");
                return new BaseResponse<ConversationDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during create conversation: {ex.Message}");
                return new BaseResponse<ConversationDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tạo hội thoại"
                };
            }
        }

        public async Task<BaseResponse<List<ConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<List<ConversationDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<List<ConversationDto>>>(
                    "/web-api/chat/conversations/list",
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<List<ConversationDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi tải danh sách hội thoại"
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

        public async Task<BaseResponse<ConversationDto>> GetConversationByIdAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<ConversationDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<ConversationDto>>(
                    $"/web-api/chat/conversations/{conversationId}",
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<ConversationDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi tải hội thoại"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get conversation: {ex.Message}");
                return new BaseResponse<ConversationDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get conversation: {ex.Message}");
                return new BaseResponse<ConversationDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải hội thoại"
                };
            }
        }

        public async Task<BaseResponse<MessageDto>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<MessageDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PostWithTokenAsync<SendMessageRequest, MessageDto>(
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
                        Message = "Có lỗi khi gửi tin nhắn"
                    };
                }

                return new BaseResponse<MessageDto>
                {
                    Status = BaseResponseStatus.Success,
                    Data = response
                };
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

        public async Task<BaseResponse<int>> CountMessage()
        {
            var token = await _identityHelper.GetAccessTokenAsync();
            return await GetWithTokenAsync<BaseResponse<int>>("/web-api/chat/messages/count", token);
        }
    }
}
