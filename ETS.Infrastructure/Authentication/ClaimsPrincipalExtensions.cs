using System.Security.Claims;

namespace ETS.Infrastructure.Authentication
{
    internal static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal? principal)
        {
            var userId = principal?.FindFirstValue("user_id");
            return userId;
        }

        public static string? GetUserEmail(this ClaimsPrincipal? principal)
        {
            var userEmail = principal?.FindFirstValue(ClaimTypes.Email);
            return userEmail;
        }

        public static string? GetUserFullName(this ClaimsPrincipal? principal)
        {
            var fullName = principal?.FindFirstValue("fullname");
            return fullName;
        }

        public static string? GetUserPhone(this ClaimsPrincipal? principal)
        {
            var userPhone = principal?.FindFirstValue("phoneNumber");
            return userPhone;
        }
                

    }
}
