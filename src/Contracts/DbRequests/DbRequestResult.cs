namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.QueryResult;

public class DbRequestResult
{
    public List<IAssetAdministrationShell> AssetAdministrationShells { get; set; }

    public List<ISubmodel> Submodels { get; set; }

    public List<ISubmodelElement> SubmodelElements { get; set; }

    public List<IReference> References { get; set; }

    public DbFileRequestResult FileRequestResult { get; set; }

    public DbRequestPackageEnvResult PackageEnv { get; set; }

    public IAssetInformation AssetInformation { get; set; }

    public Events.EventPayload EventPayload { get; set; }

    public QResult QueryResult { get; set; }
}
