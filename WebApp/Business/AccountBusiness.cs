using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.Extensions.Options;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.Account;

namespace WebApp.Business
{
    public class AccountBusiness : BaseHttpClient
    {
        private readonly AppSettings _appSettings;
        private readonly IdentityHelper _identityHelper;

        public AccountBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IOptionsMonitor<AppSettings> optionsMonitor, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _appSettings = optionsMonitor.CurrentValue;
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<PaginatedListDto<AccountDto>>> GetListAsync(GetAccountListRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<PaginatedListDto<AccountDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetObjectQueryWithTokenAsync<BaseResponse<PaginatedListDto<AccountDto>>>(
                    "/web-api/account/list",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<PaginatedListDto<AccountDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get account list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<AccountDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get account list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<AccountDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách tài khoản"
                };
            }
        }

        public async Task<BaseResponse<AccountDto>> GetByIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<AccountDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<AccountDto>>(
                    $"/web-api/account/{accountId}",
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<AccountDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi lấy tài khoản"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get account: {ex.Message}");
                return new BaseResponse<AccountDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get account: {ex.Message}");
                return new BaseResponse<AccountDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải tài khoản"
                };
            }
        }

        public async Task<BaseResponse<int>> UpdateAsync(UpdateWebAppAccountRequest request, CancellationToken cancellationToken = default)
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

                string? avatarPath = request.Avatar;

                // Handle avatar upload if changed
                if (request.IsChanged && request.NewAvatar != null)
                {
                    var uploadResult = await UploadAvatarAsync(request.AccountId, request.NewAvatar, token, cancellationToken);
                    if (uploadResult.Status == BaseResponseStatus.Error)
                    {
                        return new BaseResponse<int>
                        {
                            Status = BaseResponseStatus.Error,
                            Message = uploadResult.Message
                        };
                    }
                    avatarPath = uploadResult.Data;
                }

                // Prepare update request for AccountService
                var updateRequest = new
                {
                    AccountId = request.AccountId,
                    Name = request.Name,
                    IsActive = request.IsActive,
                    Avatar = avatarPath
                };

                var response = await PutWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/account/",
                    updateRequest,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<int>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Có lỗi khi cập nhật tài khoản"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during update account: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update account: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi cập nhật tài khoản"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAsync(int accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Bearer", token);

                var response = await DeleteWithHeadersAsync<BaseResponse<bool>>(
                    $"/web-api/account/{accountId}",
                    headers,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during delete account: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during delete account: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi xóa tài khoản"
                };
            }
        }

        public async Task<BaseResponse<int>> CreateAsync(CreateWebAppAccountRequest request, CancellationToken cancellationToken = default)
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

                string? avatarPath = null;

                // Handle avatar upload if provided
                if (request.IsChanged && request.NewAvatar != null)
                {
                    // Create a temporary accountId (0) for the upload, will be updated after account creation
                    var uploadResult = await UploadAvatarAsync(0, request.NewAvatar, token, cancellationToken);
                    if (uploadResult.Status == BaseResponseStatus.Error)
                    {
                        return new BaseResponse<int>
                        {
                            Status = BaseResponseStatus.Error,
                            Message = uploadResult.Message
                        };
                    }
                    avatarPath = uploadResult.Data;
                }

                // Prepare create request for AccountService
                var createRequest = new
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                    IsActive = request.IsActive,
                    Avatar = avatarPath
                };

                var response = await PostWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/account/",
                    createRequest,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
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
                _logger.LogError($"HTTP error during create account: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during create account: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tạo tài khoản"
                };
            }
        }

        public async Task<BaseResponse<bool>> AdminChangePasswordAsync(int accountId, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                // Prepare request for admin password change
                var apiRequest = new
                {
                    AccountId = accountId,
                    NewPassword = newPassword
                };

                var response = await PostWithTokenAsync<object, BaseResponse<bool>>(
                    "/web-api/account/admin-change-password",
                    apiRequest,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during admin change password: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during admin change password: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi đổi mật khẩu"
                };
            }
        }

        public async Task<BaseResponse<bool>> ChangePasswordAsync(string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var accountId = _identityHelper.GetUserId();
                if (accountId <= 0)
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không xác định được tài khoản hiện tại"
                    };
                }

                // Prepare request matching AccountService contract
                var apiRequest = new
                {
                    AccountId = accountId,
                    NewPassword = newPassword
                };

                var response = await PostWithTokenAsync<object, BaseResponse<bool>>(
                    "/web-api/account/change-password",
                    apiRequest,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<bool>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during change password: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ account"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during change password: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi đổi mật khẩu"
                };
            }
        }

        private async Task<BaseResponse<string>> UploadAvatarAsync(int accountId, IFormFile file, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                if (file != null)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    form.Add(fileContent, "file", file.FileName);
                }

                var directoryPath = $"avatar/{accountId}";
                form.Add(new StringContent(directoryPath), "directoryPath");

                var response = await PostFormDataWithTokenAsync<BaseResponse<StringValueDto>>(
                    "/web-api/storage/upload-minio-file",
                    form,
                    token,
                    cancellationToken
                );

                if (response == null || response.Status == 0)
                {
                    return new BaseResponse<string>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ storage service"
                    };
                }

                if (response.Status == BaseResponseStatus.Error)
                {
                    return new BaseResponse<string>
                    {
                        Status = BaseResponseStatus.Error,
                        Data = response.Data?.Value
                    };
                }

                // Return the Value property which contains the file path
                return new BaseResponse<string>
                {
                    Status = BaseResponseStatus.Success,
                    Data = response.Data?.Value,
                    Message = "Upload avatar thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading avatar: {ex.Message}");
                return new BaseResponse<string>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi upload avatar"
                };
            }
        }
    }
}
