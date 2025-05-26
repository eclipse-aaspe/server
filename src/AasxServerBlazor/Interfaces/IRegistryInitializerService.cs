namespace AasxServerBlazor.Interfaces;

using AasRegistryDiscovery.WebApi.Models;
using AasxServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRegistryInitializerService
{
    void CreateAssetAdministrationShellDescriptor(AssetAdministrationShellDescriptor newAasDesc, DateTime timestamp, bool initial = false);
    void CreateMultipleAssetAdministrationShellDescriptor(List<AssetAdministrationShellDescriptor> body, DateTime timestamp);
    ISubmodel? GetAasRegistry();

    List<AssetAdministrationShellDescriptor> GetAasDescriptorsForSubmodelView();
    List<string> GetRegistryList();
    Task InitRegistry(List<AasxCredentialsEntry> cList, DateTime timestamp, bool initAgain = false);
}
