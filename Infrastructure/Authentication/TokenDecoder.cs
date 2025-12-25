using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Infrastructure.Authentication
{
    public static class TokenDecoder
    {
        public static Dictionary<string, object> DecodeJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = AuthorizationConstants.JWT_SECRET_KEY;
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, 
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;
                var result = new Dictionary<string, object>();
                foreach (var group in jwtToken.Claims.GroupBy(c => c.Type))
                {
                    if (group.Count() > 1)
                    {
                        result[group.Key] = group.Select(x => x.Value).ToList();
                    }
                    else
                    {
                        result[group.Key] = group.First().Value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation failed: {ex.Message}");
                throw;
            }
        }


        public static int GetTenantId(Dictionary<string, object> claimsDictionary)
        {
            return GetValueAsInt(claimsDictionary, AuthorizationConstants.TOKEN_CLAIMS_TENANT);
        }

        public static int GetUserId(Dictionary<string, object> claimsDictionary)
        {
            int userId = GetValueAsInt(claimsDictionary, AuthorizationConstants.TOKEN_CLAIMS_USER);
            if (userId == 0)
            {
                userId = GetValueAsInt(claimsDictionary, "nameid");
            }

            return userId;
        }
        private static int GetValueAsInt(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] != null)
            {
                object value = data[key];

                try
                {
                    if (value is System.Collections.IEnumerable list && !(value is string))
                    {
                        var enumerator = list.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            value = enumerator.Current;
                        }
                    }
                    return Convert.ToInt32(value);
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }
    }
}
