using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.AspNetCore.WebUtilities;
using System.Buffers.Text;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.Models.Document;

namespace WebApp.Business
{
    public class DocumentBusiness : BaseHttpClient
    {
        private readonly IdentityHelper _identityHelper;

        public DocumentBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IdentityHelper identityHelper)
            : base(httpClient, appLogger)
        {
            _identityHelper = identityHelper;
        }

        public async Task<BaseResponse<PaginatedListDto<DocumentDto>>> GetListAsync(GetDocumentListRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<PaginatedListDto<DocumentDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetObjectQueryWithTokenAsync<BaseResponse<PaginatedListDto<DocumentDto>>>(
                    "/web-api/document/list",
                    request,
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<PaginatedListDto<DocumentDto>>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get document list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<DocumentDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get document list: {ex.Message}");
                return new BaseResponse<PaginatedListDto<DocumentDto>>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải danh sách tài liệu"
                };
            }
        }

        public async Task<BaseResponse<DocumentDto>> GetByIdAsync(int documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<DocumentDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await GetWithTokenAsync<BaseResponse<DocumentDto>>(
                    $"/web-api/document/{documentId}",
                    token,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<DocumentDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during get document: {ex.Message}");
                return new BaseResponse<DocumentDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during get document: {ex.Message}");
                return new BaseResponse<DocumentDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải tài liệu"
                };
            }
        }

        public async Task<BaseResponse<int>> UploadAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
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

                using var form = new MultipartFormDataContent();

                if (request.File != null)
                {
                    var fileContent = new StreamContent(request.File.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.File.ContentType);
                    form.Add(fileContent, "file", request.File.FileName);
                }
                var queryParams = new Dictionary<string, string?>
                {
                    { "doctype", request.DocumentType.ToString() },
                    { "fatherDocumentId", request.FatherDocumentId.ToString() }
                };

                if (!string.IsNullOrEmpty(request.DocumentName))
                {
                    queryParams.Add("documentName", request.DocumentName);
                }
                //form.Add(new StringContent(request.DocumentType.ToString()), "Doctype");
                //form.Add(new StringContent(request.FatherDocumentId.ToString()), "FatherDocumentId");

                //if (!string.IsNullOrEmpty(request.DocumentName))
                //{
                //    form.Add(new StringContent(request.DocumentName), "DocumentName");
                //}
                var url = QueryHelpers.AddQueryString("/web-api/document/", queryParams);
                var response = await PostFormDataWithTokenAsync<BaseResponse<int>>(
                    url,
                    form,
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
                _logger.LogError($"HTTP error during upload document: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during upload document: {ex.Message}");
                return new BaseResponse<int>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi tải lên tài liệu"
                };
            }
        }

        public async Task<BaseResponse<DocumentDto>> UpdateAsync(UpdateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _identityHelper.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<DocumentDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không tìm thấy token xác thực"
                    };
                }

                var response = await PutAsync<UpdateDocumentRequest, BaseResponse<DocumentDto>>(
                    "/web-api/document/",
                    request,
                    cancellationToken
                );

                if (response == null)
                {
                    return new BaseResponse<DocumentDto>
                    {
                        Status = BaseResponseStatus.Error,
                        Message = "Không nhận được phản hồi từ server"
                    };
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP error during update document: {ex.Message}");
                return new BaseResponse<DocumentDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during update document: {ex.Message}");
                return new BaseResponse<DocumentDto>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi cập nhật tài liệu"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAsync(int documentId, CancellationToken cancellationToken = default)
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
                    $"/web-api/document/{documentId}",
                    headers,
                    cancellationToken
                );

                if (response == null)
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
                _logger.LogError($"HTTP error during delete document: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during delete document: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi xóa tài liệu"
                };
            }
        }

        public async Task<BaseResponse<bool>> VectorizeAsync(int documentId, CancellationToken cancellationToken = default)
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

                var response = await PostStringWithHeadersAsync<BaseResponse<bool>>(
                    $"/web-api/document/vectorize/{documentId}",
                    string.Empty,
                    headers,
                    cancellationToken
                );

                if (response == null)
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
                _logger.LogError($"HTTP error during vectorize document: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Lỗi kết nối đến dịch vụ quản lý tài liệu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during vectorize document: {ex.Message}");
                return new BaseResponse<bool>
                {
                    Status = BaseResponseStatus.Error,
                    Message = "Đã xảy ra lỗi khi nạp dữ liệu cho tài liệu"
                };
            }
        }
    }
}
