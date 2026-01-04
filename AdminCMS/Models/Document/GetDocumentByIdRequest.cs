using Infrastructure;

namespace AdminCMS.Models.Document
{
    public class GetDocumentByIdRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
