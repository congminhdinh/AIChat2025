using Infrastructure;
using Infrastructure.Enums;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
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

        //public IActionResult PermissionCheck()
        //{
        //    if (!_currentUserProvider.IsAdmin)
        //    {
        //        return new RedirectResult("Error");
        //    }
        //    return ;
        //}
    }
}
