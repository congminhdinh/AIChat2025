using Infrastructure;

namespace WebApp.Models.PromptConfig
{
    public class GetPromptConfigByIdRequest : BaseRequest
    {
        public int Id { get; set; }
    }
}
