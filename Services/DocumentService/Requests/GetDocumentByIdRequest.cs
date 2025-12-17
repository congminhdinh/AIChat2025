using Infrastructure;

namespace DocumentService.Requests
{
    public class GetDocumentByIdRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
