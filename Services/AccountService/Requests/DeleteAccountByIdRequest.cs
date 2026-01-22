using Infrastructure;

namespace AccountService.Requests
{
    public class DeleteAccountByIdRequest: BaseRequest
    {
        public int AccountId { get; set; }
    }
}
