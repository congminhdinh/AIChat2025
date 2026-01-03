using Infrastructure;

namespace WebApp.Models.Document
{
    public class GetDocumentByIdRequest : BaseRequest
    {
        public int DocumentId { get; set; }
    }
}
