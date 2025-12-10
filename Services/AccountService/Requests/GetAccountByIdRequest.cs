using Infrastructure;

namespace AccountService.Requests
{
    public class GetAccountByIdRequest: BaseRequest
    {
        public int AccountId { get; set; }
    }
}
