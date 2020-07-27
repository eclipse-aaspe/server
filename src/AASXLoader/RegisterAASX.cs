using BaSyx.Models.Connectivity;
using BaSyx.Models.Core.AssetAdministrationShell.Generics;
using BaSyx.Models.Core.AssetAdministrationShell.Implementations;
using BaSyx.Models.Export;
using BaSyx.Registry.Client.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;

namespace AASXLoader
{
    public class Registry
    {
        static void Main()
        {

        }
        public static void RegisterAASX(string registry, string server, string aasxFolder)
        {
            RegistryClientSettings settings = new RegistryClientSettings();
            settings.RegistryConfig.RegistryUrl = "http://" + registry;

            RegistryClient registryClient = new RegistryClient(settings);

            string[] aasxFiles = Directory.GetFiles(aasxFolder, "*.aasx");
            foreach (var aasxFile in aasxFiles)
            {
                Package package = Package.Open(aasxFile, FileMode.Open, FileAccess.Read);
                using (AASX aasx = new AASX(package))
                {
                    AssetAdministrationShellEnvironment environment = aasx.GetEnvironment();
                    IAssetAdministrationShell aas = environment?.AssetAdministrationShells[0];

                    if (aas != null)
                    {
                        AssetAdministrationShellDescriptor shellDescriptor = new AssetAdministrationShellDescriptor(aas,
                            new List<HttpEndpoint>()
                            {
                                //Hier die entsprechende IP-Adresse der Verwaltungsschale eintragen, die du bereits gestartet hast
                                new HttpEndpoint("http://" + server + "/aas/" + aas.IdShort + "/complete")
                            });

                        foreach (var submodel in aas.Submodels)
                        {
                            if (submodel != null)
                            {
                                SubmodelDescriptor submodelDescriptor = new SubmodelDescriptor(submodel,
                                     new List<HttpEndpoint>()
                                     {
                                         //Hier die entsprechende IP-Adresse der Verwaltungsschale oder des sich selbst-hostenden Teilmodells eintragen
                                         //In diesem Beispiel hostet die VWS das Teilmodell
                                         new HttpEndpoint("http://" + server + "/aas/" + aas.IdShort + "/submodels/" + submodel.IdShort + "/table")
                                     });

                                shellDescriptor.SubmodelDescriptors.Create(submodelDescriptor);
                            }
                        }
                        Console.WriteLine("Register AAS: {0} {1}", aas.IdShort, aas.Identification.Id);
                        registryClient.CreateAssetAdministrationShell(shellDescriptor);
                    }
                    else
                    {
                        Console.WriteLine("Can not register AASX: {0}", aasxFile);
                    }
                }
            }
            Console.WriteLine("View registry on http://{0}/ui", registry);
            Console.WriteLine();
        }
    }
}
