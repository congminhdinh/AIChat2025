using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class PhoneNumberUtils
    {
        private static readonly Regex VietnamesePhoneRegex = new Regex(
            @"^(\+84|84|0)(3[2-9]|5[689]|7[06-9]|8[1-689]|9[0-46-9])[0-9]{7}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validates Vietnamese phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove spaces and dashes
            var cleanedNumber = phoneNumber.Replace(" ", "").Replace("-", "");

            // Check Vietnamese mobile number format
            return VietnamesePhoneRegex.IsMatch(cleanedNumber);
        }

        /// <summary>
        /// Normalizes phone number to standard format (with +84 prefix)
        /// </summary>
        /// <param name="phoneNumber">Phone number to normalize</param>
        /// <returns>Normalized phone number or null if invalid</returns>
        public static string? NormalizePhoneNumber(string phoneNumber)
        {
            if (!IsValidPhoneNumber(phoneNumber))
                return null;

            var cleanedNumber = phoneNumber.Replace(" ", "").Replace("-", "");

            if (cleanedNumber.StartsWith("+84"))
                return cleanedNumber;

            if (cleanedNumber.StartsWith("84"))
                return "+" + cleanedNumber;


            //format = 0


            if (cleanedNumber.StartsWith("0"))
                return "+84" + cleanedNumber.Substring(1);

            return "+84" + cleanedNumber;
        }
    }
}
