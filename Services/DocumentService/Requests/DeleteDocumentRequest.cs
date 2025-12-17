using Infrastructure;

namespace DocumentService.Requests
{
    public class DeleteDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
