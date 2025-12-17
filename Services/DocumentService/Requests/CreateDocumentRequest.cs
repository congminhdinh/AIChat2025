using Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DocumentService.Requests
{
    public class CreateDocumentRequest : BaseRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
