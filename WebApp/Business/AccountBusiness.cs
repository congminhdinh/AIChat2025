using Infrastructure;
using Infrastructure.Logging;
using Infrastructure.Web;
using Microsoft.Extensions.Options;

namespace WebApp.Business
{
    public class AccountBusiness: BaseHttpClient
    {
        private readonly AppSettings _appSettings;
        public AccountBusiness(HttpClient httpClient, IAppLogger<BaseHttpClient> appLogger, IOptionsMonitor<AppSettings> optionsMonitor) : base(httpClient, appLogger)
        {
            _appSettings = optionsMonitor.CurrentValue;
        }
    }
}
