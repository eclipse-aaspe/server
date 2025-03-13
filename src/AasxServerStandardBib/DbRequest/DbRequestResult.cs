namespace AasxServerStandardBib.DbRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;

public class DbRequestResult : IDbRequestResult
{
    public List<IAssetAdministrationShell> AssetAdministrationShells {  get; set; }

    public ISubmodel Submodel { get; set; }

    public Exception Exception { get; set; }

}
