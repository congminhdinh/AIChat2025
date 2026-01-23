using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Models;
using WebApp.Requests;

namespace WebApp.Business
{
    public class AuthBusiness: BaseHttpClient
    {
        public AuthBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger) : base(httpClient, appLogger)
        {
        }

        public async Task<BaseResponse<TokenDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "X-Tenant-Key", request.TenantKey }
                };
                var response = await PostWithHeadersAsync<LoginRequest, BaseResponse<TokenDto>>(
                    "/web-api/account/auth/login",
                    request,
                    headers,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<TokenDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during login: {ex.Message}");
                return new BaseResponse<TokenDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ xác thực"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                return new BaseResponse<TokenDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi trong quá trình đăng nhập"
                };
            }
        }
    }
}
