using Infrastructure;
using Infrastructure.Utils;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
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
        // local file system
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

        public Stream? DownloadFile(string relativeDirectory)
        {
            try
            {
                var baseDir = _appSettings.DocumentFilePath;
                var fullPath = Path.Combine(baseDir, relativeDirectory);

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning($"File not found: {fullPath}");
                    return null;
                }
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {relativeDirectory}");
                return null;
            }
        }

        // Minio
        public async Task<BaseResponse<StringValueDto>> UploadObject(UploadMinioRequest file)
        {
            
            try
            {

                var minioClient = NewMinIOClient();
                var fileName = string.IsNullOrWhiteSpace(file.FileName) ? file.File.FileName : file.FileName;
                var newFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}{Path.GetExtension(fileName)}";
                var filePath = !string.IsNullOrEmpty(file.Directory) ? Path.Combine(file.Directory, newFileName) : newFileName;


                using (var stream = file.File.OpenReadStream())
                {
                    PutObjectArgs putObjectArgs = new PutObjectArgs()
                                                  .WithBucket(_appSettings.MinioBucket)
                                                  .WithObject(filePath)
                                                  .WithStreamData(stream)
                                                  .WithObjectSize(stream.Length)
                                                  .WithContentType(fileName.GetMimeType());
                    var result = await minioClient.PutObjectAsync(putObjectArgs);
                }
                ;

                _logger.LogInformation($"{filePath} is uploaded successfully");
                return new BaseResponse<StringValueDto>(new StringValueDto { Value = filePath }, file.CorrelationId());

            }
            catch (MinioException e)
            {
                _logger.LogWarning("Error occurred: " + e);
                return new BaseResponse<StringValueDto>("Upload file failed", BaseResponseStatus.Error, file.CorrelationId());
            }
        }

        public async Task<Stream?> DownloadMinioFile(string objectName)
        {
            try
            {
                var minioClient = NewMinIOClient();

                // Check if object exists
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_appSettings.MinioBucket)
                    .WithObject(objectName);

                await minioClient.StatObjectAsync(statObjectArgs);

                // Get object stream
                var memoryStream = new MemoryStream();
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_appSettings.MinioBucket)
                    .WithObject(objectName)
                    .WithCallbackStream((stream) =>
                    {
                        stream.CopyTo(memoryStream);
                    });

                await minioClient.GetObjectAsync(getObjectArgs);
                memoryStream.Position = 0;

                _logger.LogInformation($"File {objectName} downloaded successfully from MinIO");
                return memoryStream;
            }
            catch (MinioException e)
            {
                _logger.LogError(e, $"Error downloading file from MinIO: {objectName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {objectName}");
                return null;
            }
        }

        public async Task<bool> SetPolicy(string bucketname)
        {
            string policyJson = $@"{{""Version"":""2012-10-17"",""Statement"":[{{""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Action"":[""s3:ListBucket"",""s3:ListBucketMultipartUploads"",""s3:GetBucketLocation""],""Resource"":[""arn:aws:s3:::{bucketname}""]}},{{""Effect"":""Allow"",""Principal"":{{""AWS"":[""*""]}},""Action"":[""s3:ListMultipartUploadParts"",""s3:PutObject"",""s3:AbortMultipartUpload"",""s3:DeleteObject"",""s3:GetObject""],""Resource"":[""arn:aws:s3:::{bucketname}/*""]}}]}}";

            try
            {
                var minioClient = NewMinIOClient();

                SetPolicyArgs args = new SetPolicyArgs()
                                         .WithBucket(bucketname)
                                         .WithPolicy(policyJson);
                await minioClient.SetPolicyAsync(args);

                return true;
            }

            catch (MinioException e)
            {
                _logger.LogWarning("Error occurred: " + e);
                return false;
            }
        }

        private IMinioClient NewMinIOClient()
        {
            var minioClient = new MinioClient()
                    .WithEndpoint(_appSettings.MinioEndpoint)
                    .WithCredentials(_appSettings.MinioAccessKey, _appSettings.MinioSecretKey)
                    .WithSSL(false)
                    .Build();
            return minioClient;
        }

    }
}
