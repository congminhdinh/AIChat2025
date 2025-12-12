using Infrastructure;
using Microsoft.Extensions.Options;
using StorageService.Dtos;
using StorageService.Requests;

namespace StorageService.Features
{
    public class StorageBusiness
    {
        private readonly ILogger<StorageBusiness> _logger;
        private readonly AppSettings _appSettings;

        public StorageBusiness(ILogger<StorageBusiness> logger, IOptionsMonitor<AppSettings> optionsMonitor)
        {
            _logger = logger;
            _appSettings = optionsMonitor.CurrentValue;
        }

        public async Task<BaseResponse<StringValueDto>> UploadFileSystem(UploadFileSystemRequest input)
        {
            if (input.File == null)
            {
                return new BaseResponse<StringValueDto>("Input is null", BaseResponseStatus.Error, input.CorrelationId());
            }
            var uploadDir = _appSettings.DocumentFilePath;
            var fileName = string.IsNullOrEmpty(input.FileName) ? input.File.FileName : input.FileName;
            var fileDir = Path.Combine(uploadDir, input.Directory ?? string.Empty);
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            var fileNameWithPrefix = $"{Path.GetFileNameWithoutExtension(fileName)}_{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}{Path.GetExtension(fileName)}";
            var filePath = Path.Combine(fileDir, fileNameWithPrefix);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await input.File.CopyToAsync(stream);
            }
            var relativePath = Path.Combine(input.Directory ?? string.Empty, fileNameWithPrefix);
            return new BaseResponse<StringValueDto>(new StringValueDto { Value = relativePath }, input.CorrelationId());
        }
    }
}
