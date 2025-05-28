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
    public DbRequestPackageEnvResult PackageEnv { get; set; }

    public List<IAssetAdministrationShell> AssetAdministrationShells { get; set; }
    public List<IReference> References { get; set; }
    public List<ISubmodel> Submodels { get; set; }
    public List<ISubmodelElement> SubmodelElements { get; set; }
    public List<IConceptDescription> ConceptDescriptions { get; set; }
    public AasCore.Aas3_0.Environment Environment { get; set; }

    public DbFileRequestResult FileRequestResult { get; set; }

    public IAssetInformation AssetInformation { get; set; }

    public Events.EventPayload EventPayload { get; set; }

    // Queries
    public QResult QueryResult { get; set; }
    public int Count { get; set; }
}
