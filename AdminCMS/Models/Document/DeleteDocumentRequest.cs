using Infrastructure;

namespace AdminCMS.Models.Document
{
    public class DeleteDocumentRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
