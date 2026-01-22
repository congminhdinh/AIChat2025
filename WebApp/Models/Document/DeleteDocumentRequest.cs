using Infrastructure;

namespace WebApp.Models.Document
{
    public class DeleteDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
