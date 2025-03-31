namespace AasRegistryDiscovery.WebApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasRegistryDiscovery.WebApi.Models;

public interface IPersistenceService
{
    AssetAdministrationShellDescriptor CreateAssetAdminitrationShellDescriptor(AssetAdministrationShellDescriptor aasDescriptor);
    SubmodelDescriptor CreateSubmodelDescriptorWithinAasDescriptor(string? aasIdentifier, SubmodelDescriptor smDescriptor);
    AssetAdministrationShellDescriptor GetAssetAdministrationShellDescriptorById(string aasIdentifier);
    SubmodelDescriptor GetSubmodelDescriptorWithinAasDescriptor(string? aasIdentifier, string? smIdentifier);
}
