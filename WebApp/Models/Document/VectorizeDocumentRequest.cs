using Infrastructure;

namespace WebApp.Models.Document
{
    public class VectorizeDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
