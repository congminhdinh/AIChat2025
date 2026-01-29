using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.Chat;

namespace WebApp.Business
{
    public class ChatFeedbackBusiness : BaseHttpClient
    {
        private readonly IdentityHelper _identityHelper;

        public ChatFeedbackBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<ChatFeedbackDetailDto>> GetChatFeedbackDetailAsync(int messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<ChatFeedbackDetailDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<ChatFeedbackDetailDto>>(
                    $"/web-api/chat/chat-feedback/{messageId}",
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<ChatFeedbackDetailDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi lấy chi tiết phản hồi"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get chat feedback: {ex.Message}");
                return new BaseResponse<ChatFeedbackDetailDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get chat feedback: {ex.Message}");
                return new BaseResponse<ChatFeedbackDetailDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải phản hồi"
                };
            }
        }

        public async Task<BaseResponse<int>> RateChatFeedbackAsync(RateChatFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PostWithTokenAsync<RateChatFeedbackRequest, BaseResponse<int>>(
                    "/web-api/chat/chat-feedback/rate",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi đánh giá phản hồi"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during rate feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during rate feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi đánh giá"
                };
            }
        }

        public async Task<BaseResponse<int>> CreateChatFeedbackAsync(CreateChatFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PostWithTokenAsync<CreateChatFeedbackRequest, BaseResponse<int>>(
                    "/web-api/chat/chat-feedback",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi tạo phản hồi"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during create feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during create feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tạo phản hồi"
                };
            }
        }

        public async Task<BaseResponse<int>> UpdateChatFeedbackAsync(UpdateChatFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PutWithTokenAsync<UpdateChatFeedbackRequest, BaseResponse<int>>(
                    $"/web-api/chat/chat-feedback/{request.Id}",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi cập nhật phản hồi"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during update feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update feedback: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi cập nhật phản hồi"
                };
            }
        }

        public async Task<BaseResponse<PaginatedListDto<ChatFeedbackDto>>> GetChatFeedbackListAsync(
            short? ratings = null,
            int pageIndex = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<PaginatedListDto<ChatFeedbackDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                // Build query string
                var queryParams = $"?PageIndex={pageIndex}&PageSize={pageSize}";
                if (ratings.HasValue)
                {
                    queryParams += $"&Ratings={ratings.Value}";
                }

                var response = await GetWithTokenAsync<BaseResponse<PaginatedListDto<ChatFeedbackDto>>>(
                    $"/web-api/chat/chat-feedback/{queryParams}",
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<PaginatedListDto<ChatFeedbackDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi tải danh sách phản hồi"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get chat feedback list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<ChatFeedbackDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ chat"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get chat feedback list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<ChatFeedbackDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách phản hồi"
                };
            }
        }
    }
}
