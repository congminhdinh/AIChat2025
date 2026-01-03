using Infrastructure;
using Infrastructure.Enums;
using Infrastructure.Logging;
using Infrastructure.Web;
using WebApp.Models.Account;

namespace WebApp.Business
{
    public class PermissionBusiness: BaseHttpClient
    {
        private ICurrentUserProvider _currentUserProvider;
        public PermissionBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, ICurrentUserProvider currentUserProvider) : base(httpClient, appLogger)
        {
            _currentUserProvider = currentUserProvider;
        }

        public async Task<List<int>> GetPermissionsAsync()
        {
            var token = _currentUserProvider.Token;
            var userId = _currentUserProvider.UserId;
            var response = await GetWithTokenAsync<BaseResponse<AccountDto>>($"/web-api/account/{userId}", token);
            if(response == null || response.Data == null)
            {
               return new List<int>();
            }
            return response.Data.PermissionList;
        }
    }
}
