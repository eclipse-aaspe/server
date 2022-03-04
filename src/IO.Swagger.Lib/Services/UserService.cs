using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Swagger.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        bool Authenticate(string username, string password);
    }

    /// <summary>
    /// 
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Authenticate(string username, string password)
        {
            return (!AasxServer.Program.noSecurity) || (username.Equals("admin") && password.Equals("admin"));
        }
    }
}
