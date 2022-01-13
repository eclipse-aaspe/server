
using AasxServerBlazor.Interfaces;
using System;

namespace AasxServerBlazor
{
    public class UserService : IUserService
    {
        /// <summary>
        /// Validates user credentials based on username and password
        /// </summary>
        public bool ValidateCredentials(string username, string password)
        {
#if DEBUG
            return true;
#else
            string passwordFromEnvironment = Environment.GetEnvironmentVariable("ServicePassword");
            if (string.IsNullOrEmpty(passwordFromEnvironment))
            {
                return false;
            }
            else
            {
                return username.Equals("admin") && password.Equals(passwordFromEnvironment);
            }
#endif
        }
    }
}
