using Infrastructure;

namespace StorageService.Requests
{
    public class UploadFileSystemRequest: BaseRequest
    {
        public IFormFile File { get; set; }
        public string? FileName { get; set; }
        public string? Directory { get; set; }
    }

    public class UploadMinioRequest : BaseRequest
    {
        public IFormFile File { get; set; }
        public string? FileName { get; set; }
        public string? Directory { get; set; }
    }
}
