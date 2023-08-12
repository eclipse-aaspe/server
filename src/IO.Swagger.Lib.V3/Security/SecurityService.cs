using AasxServer;
using IO.Swagger.Lib.V3.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.Web;

namespace IO.Swagger.Lib.V3.Security
{
    public class SecurityService : ISecurityService
    {
        private AasSecurityContext _securityContext;


        //TODO:jtikekar Need to refactor, remove dependency from AasxHttpContextHelper
        public void SecurityCheckInit(HttpContext _context, string _route, string _httpOperation)
        {
            if (!AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification)
                return;

            int index = -1;
            NameValueCollection query = HttpUtility.ParseQueryString(_context.Request.QueryString.ToString());
            NameValueCollection headers = new NameValueCollection();
            foreach (var kvp in _context.Request.Headers)
            {
                headers.Add(kvp.Key.ToString(), kvp.Value.ToString());
            }
            string accessRights = AasxRestServerLibrary.AasxHttpContextHelper.SecurityCheck(query, headers, ref index);

            _securityContext = new AasSecurityContext(accessRights, _route, _httpOperation);
        }

        public void SecurityCheck(string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null)
        {
            if (!AasxRestServerLibrary.AasxHttpContextHelper.withAuthentification)
                return;

            CheckAccessRights(_securityContext.AccessRights, _securityContext.Route, _securityContext.NeededRights,
                objPath, aasOrSubmodel, objectAasOrSubmodel);
        }

        public static bool CheckAccessRights(string currentRole, string operation, string neededRights,
            string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null, bool testOnly = false)
        {
            bool withAllow = false;
            string getPolicy = null;
            return CheckAccessRightsWithAllow(currentRole, operation, neededRights, out withAllow, out  getPolicy,
                objPath, aasOrSubmodel, objectAasOrSubmodel, testOnly);
        }

        public static bool CheckAccessRightsWithAllow(string currentRole, string operation, string neededRights, out bool withAllow, out string getPolicy,
           string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null, bool testOnly = false)
        {
            string error = "Access not allowed";
            getPolicy = null;
            withAllow = false;

            if (Program.secretStringAPI != null)
            {
                if (neededRights == "READ")
                    return true;
                if ((neededRights == "UPDATE" || neededRights == "DELETE") && currentRole == "UPDATE")
                    return true;
            }
            else
            {
                if (AasxRestServerLibrary.AasxHttpContextHelper.checkAccessLevelWithError(
                    out error, currentRole, operation, neededRights, out withAllow, out getPolicy,
                    objPath, aasOrSubmodel, objectAasOrSubmodel))
                    return true;

                if (currentRole == null)
                {
                    /*
                    if (AasxServer.Program.redirectServer != "")
                    {
                        System.Collections.Specialized.NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                        string originalRequest = context.Request.Url.ToString();
                        queryString.Add("OriginalRequest", originalRequest);
                        Console.WriteLine("\nRedirect OriginalRequset: " + originalRequest);
                        string response = AasxServer.Program.redirectServer + "?" + "authType=" + AasxServer.Program.authType + "&" + queryString;
                        Console.WriteLine("Redirect Response: " + response + "\n");
                        SendRedirectResponse(context, response);
                        return false;
                    }
                    */
                }
            }

            /*
            dynamic res = new ExpandoObject();
            res.error = "You are not authorized for this operation!";
            context.Response.StatusCode = HttpStatusCode.Unauthorized;
            SendJsonResponse(context, res);
            */

            // Exception
            if (!testOnly)
            {
                throw new NotAllowed(error);
            }

            return false;
        }

    }
}
