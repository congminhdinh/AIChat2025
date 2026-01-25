using AdminCMS.Helpers;
using AdminCMS.Models;
using AdminCMS.Models.Tenant;
using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.Extensions.Options;

namespace AdminCMS.Business
{
    public class TenantBusiness : BaseHttpClient
    {
        private readonly AppSettings _appSettings;
        private readonly IdentityHelper _identityHelper;

        public TenantBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IOptionsMonitor<AppSettings> optionsMonitor, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _appSettings = optionsMonitor.CurrentValue;
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<PaginatedListDto<TenantDto>>> GetListAsync(GetTenantListRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<PaginatedListDto<TenantDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetObjectQueryWithTokenAsync<BaseResponse<PaginatedListDto<TenantDto>>>(
                    "/web-api/tenant/list",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<PaginatedListDto<TenantDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get tenant list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<TenantDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get tenant list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<TenantDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách tenant"
                };
            }
        }

        public async Task<BaseResponse<TenantDto>> GetByIdAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<TenantDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<TenantDto>>(
                    $"/web-api/tenant/{tenantId}",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<TenantDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get tenant: {ex.Message}");
                return new BaseResponse<TenantDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get tenant: {ex.Message}");
                return new BaseResponse<TenantDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải thông tin tenant"
                };
            }
        }

        public async Task<BaseResponse<int>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
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

                // Prepare create request for TenantService
                var createRequest = new
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    Email = request.Email,
                    Password = request.Password,
                    AccountName = request.AccountName,
                    PermissionsList = request.PermissionsList
                };

                var response = await PostWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/tenant/create",
                    createRequest,
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
                _logger.LogError($"HTTP error during create tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during create tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tạo tenant"
                };
            }
        }

        public async Task<BaseResponse<int>> UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken = default)
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

                // Prepare update request for TenantService
                var updateRequest = new
                {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description
                };

                var response = await PostWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/tenant/update",
                    updateRequest,
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
                _logger.LogError($"HTTP error during update tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi cập nhật tenant"
                };
            }
        }

        public async Task<BaseResponse<int>> DeactivateAsync(int tenantId, CancellationToken cancellationToken = default)
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

                // Prepare deactivate request
                var deactivateRequest = new
                {
                    Id = tenantId
                };

                var response = await PostWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/tenant/deactivate",
                    deactivateRequest,
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
                _logger.LogError($"HTTP error during deactivate tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during deactivate tenant: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi vô hiệu hóa tenant"
                };
            }
        }

        public async Task<BaseResponse<TenantKeyDto>> GetTenantKeyAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<TenantKeyDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<TenantKeyDto>>(
                    $"/web-api/tenant/tenant-key/{tenantId}",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<TenantKeyDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get tenant key: {ex.Message}");
                return new BaseResponse<TenantKeyDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get tenant key: {ex.Message}");
                return new BaseResponse<TenantKeyDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải tenant key"
                };
            }
        }

        public async Task<BaseResponse<int>> RefreshTenantKeyAsync(int tenantId, CancellationToken cancellationToken = default)
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

                var refreshRequest = new
                {
                    Id = tenantId
                };

                var response = await PostWithTokenAsync<object, BaseResponse<int>>(
                    "/web-api/tenant/tenant-key",
                    refreshRequest,
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
                _logger.LogError($"HTTP error during refresh tenant key: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ tenant"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during refresh tenant key: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi làm mới tenant key"
                };
            }
        }
    }
}
