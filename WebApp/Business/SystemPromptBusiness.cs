using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.SystemPrompt;

namespace WebApp.Business
{
    public class SystemPromptBusiness : BaseHttpClient
    {
        private readonly IdentityHelper _identityHelper;

        public SystemPromptBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<PaginatedListDto<SystemPromptDto>>> GetListAsync(GetListSystemPromptRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<PaginatedListDto<SystemPromptDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetObjectQueryWithTokenAsync<BaseResponse<PaginatedListDto<SystemPromptDto>>>(
                    "/web-api/chat/system-prompt/",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<PaginatedListDto<SystemPromptDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get system prompt list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<SystemPromptDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ system prompt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get system prompt list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<SystemPromptDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách system prompt"
                };
            }
        }

        public async Task<BaseResponse<SystemPromptDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<SystemPromptDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<SystemPromptDto>>(
                    $"/web-api/chat/system-prompt/{id}",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<SystemPromptDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get system prompt: {ex.Message}");
                return new BaseResponse<SystemPromptDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ system prompt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get system prompt: {ex.Message}");
                return new BaseResponse<SystemPromptDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải system prompt"
                };
            }
        }

        public async Task<BaseResponse<int>> CreateAsync(CreateSystemPromptRequest request, CancellationToken cancellationToken = default)
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

                var response = await PostWithTokenAsync<CreateSystemPromptRequest, BaseResponse<int>>(
                    "/web-api/chat/system-prompt/",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during create system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ system prompt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during create system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tạo system prompt"
                };
            }
        }

        public async Task<BaseResponse<int>> UpdateAsync(UpdateSystemPromptRequest request, CancellationToken cancellationToken = default)
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

                var response = await PutWithTokenAsync<UpdateSystemPromptRequest, BaseResponse<int>>(
                    $"/web-api/chat/system-prompt/{request.Id}",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during update system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ system prompt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi cập nhật system prompt"
                };
            }
        }

        public async Task<BaseResponse<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
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

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Bearer", token);

                var response = await DeleteWithHeadersAsync<BaseResponse<int>>(
                    $"/web-api/chat/system-prompt/{id}",
                    headers,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during delete system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ system prompt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during delete system prompt: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi xóa system prompt"
                };
            }
        }
    }
}
