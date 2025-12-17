using Infrastructure;

namespace DocumentService.Requests
{
    public class VectorizeDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
