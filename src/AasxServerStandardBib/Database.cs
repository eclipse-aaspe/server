using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxServerStandardBib.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Collections;
using AasCore.Aas3_0;
using AdminShellNS;
using MongoDB.Driver.GridFS;
using static AasxServerStandardBib.TimeSeriesPlotting.PlotArguments;
using System.IO;


//Author: Jonas Graubner
//contact: jogithub@graubner-bayern.de
namespace AasxServerStandardBib
{
    public interface IDatabase
    {
        void Initialize(String connectionString);

        #region AssetAdministrationShell
        public void WriteDBAssetAdministrationShell(IAssetAdministrationShell shell);
        public IQueryable<AssetAdministrationShell> GetLINQAssetAdministrationShell();
        public void UpdateDBAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier);
        public bool DeleteDBAssetAdministrationShellById(IAssetAdministrationShell shell);
        #endregion

        #region Submodel
        public void WriteDBSubmodel(ISubmodel submodel);
        public IQueryable<Submodel> GetLINQSubmodel();
        public void UpdateDBSubmodelById(string submodelIdentifier, ISubmodel newSubmodel);
        public void DeleteDBSubmodelById(string submodelIdentifier);
        #endregion

        #region ConceptDescription
        public void WriteDBConceptDescription(IConceptDescription conceptDescription);
        public IQueryable<ConceptDescription> GetLINQConceptDescription();
        public void UpdateDBConceptDescriptionById(IConceptDescription newConceptDescription, string cdIdentifier);
        public void DeleteDBConceptDescriptionById(string conceptDescription);
        #endregion

        #region Filestream
        public void WriteFile(Stream stream, string filename);
        public Stream ReadFile(string filename);
        #endregion

        public void importAASCoreEnvironment(IEnvironment environment);
        public void importAdminShellPackageEnv(AdminShellPackageEnv adminShellPackageEnv);
    }


    public class MongoDatabase : IDatabase
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private GridFSBucket _bucket;

        public void Initialize(String connectionString)
        {
            //_client = new MongoClient("mongodb://AAS:SefuSWE63811!@192.168.0.22:27017/?authSource=AAS");
            _client = new MongoClient(connectionString);
            try
            {
                _client.StartSession();
            }
            catch (System.TimeoutException ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Environment.Exit(1);
            }

            _database = _client.GetDatabase("AAS");
            var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("AasCore") || type.FullName.StartsWith("MongoDB"));
            BsonSerializer.RegisterSerializer(objectSerializer);
            _bucket = new GridFSBucket(_database, new GridFSBucketOptions
            {
                BucketName = "aasxFiles",
            });
        }
        private IMongoCollection<AssetAdministrationShell> getAasCollection()
        {
            return _database.GetCollection<AssetAdministrationShell>("AssetAdministrationShells");
        }
        private IMongoCollection<Submodel> getSubmodelCollection()
        {
            return _database.GetCollection<Submodel>("Submodels");
        }
        private IMongoCollection<ConceptDescription> getConceptDescriptionCollection()
        {
            return _database.GetCollection<ConceptDescription>("ConceptDescriptions");
        }


        #region AssetAdministrationShell
        public void WriteDBAssetAdministrationShell(IAssetAdministrationShell shell)
        {
            try
            {
                getAasCollection().InsertOne((AssetAdministrationShell)shell);
            }
            catch (MongoWriteException)
            {
            }
        }
        public bool DeleteDBAssetAdministrationShellById(IAssetAdministrationShell shell)
        {
            throw new NotImplementedException();
        }
        public IQueryable<AssetAdministrationShell> GetLINQAssetAdministrationShell()
        {
            return getAasCollection().AsQueryable();
        }
        public async void UpdateDBAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier)
        {
            await getAasCollection().ReplaceOneAsync(r => r.Id.Equals(aasIdentifier), (AssetAdministrationShell)body);
        }
        #endregion

        #region Submodel
        public void WriteDBSubmodel(ISubmodel submodel)
        {
            try
            {
                getSubmodelCollection().InsertOne((Submodel)submodel);
            }
            catch (MongoWriteException)
            {
            }
        }
        public IQueryable<Submodel> GetLINQSubmodel()
        {
            return getSubmodelCollection().AsQueryable();
        }
        public async void UpdateDBSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            await getSubmodelCollection().ReplaceOneAsync(r => r.Id.Equals(submodelIdentifier), (Submodel)newSubmodel);
        }
        public async void DeleteDBSubmodelById(string submodelIdentifier)
        {
            await getSubmodelCollection().DeleteOneAsync(a => a.Id.Equals(submodelIdentifier));
        }
        #endregion

        #region ConceptDescription
        public void WriteDBConceptDescription(IConceptDescription conceptDescription)
        {
            try
            {
                getConceptDescriptionCollection().InsertOne((ConceptDescription)conceptDescription);
            }
            catch (MongoWriteException)
            {
            }
        }
        public IQueryable<ConceptDescription> GetLINQConceptDescription()
        {
            return getConceptDescriptionCollection().AsQueryable();
        }
        public async void UpdateDBConceptDescriptionById(IConceptDescription newConceptDescription, string cdIdentifier)
        {
            await getConceptDescriptionCollection().ReplaceOneAsync(r => r.Id.Equals(cdIdentifier), (ConceptDescription)newConceptDescription);
        }
        public async void DeleteDBConceptDescriptionById(string conceptDescription)
        {
            await getConceptDescriptionCollection().DeleteOneAsync(a => a.Id.Equals(conceptDescription));
        }
        #endregion

        #region Filestream
        public async void WriteFile(Stream stream, string filename)
        {
            if (stream != null)
            {
                Console.WriteLine("New File");
                var id = await _bucket.UploadFromStreamAsync(filename, stream);
                stream.Close();
            }else
            {
                //throw new ArgumentNullException(nameof(stream));
            }
        }
        public Stream ReadFile(string filename)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, filename);
            var sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime);
            var options = new GridFSFindOptions
            {
                Limit = 1,
                Sort = sort
            };

            using (var cursor = _bucket.Find(filter, options))
            {
                var fileInfo = cursor.ToList().FirstOrDefault();
                // fileInfo either has the matching file information or is null
                return _bucket.OpenDownloadStream(fileInfo.Id);
            }
        }


        #endregion


        public void importAASCoreEnvironment(IEnvironment environment)
        {
            environment.AssetAdministrationShells.ForEach(shell => {
                WriteDBAssetAdministrationShell(shell);
            });

            environment.Submodels.ForEach(submodel =>
            {
                WriteDBSubmodel(submodel);
            });

            environment.ConceptDescriptions.ForEach(conceptDescription =>
            {
                WriteDBConceptDescription(conceptDescription);
            });
        }

        public void importAdminShellPackageEnv(AdminShellPackageEnv adminShellPackageEnv)
        {
            importAASCoreEnvironment(adminShellPackageEnv.AasEnv);

            //now import Files
            var files = adminShellPackageEnv.GetListOfSupplementaryFiles();
            var assetid = adminShellPackageEnv.AasEnv.AssetAdministrationShells[0].AssetInformation.GlobalAssetId; //unique identifier
            foreach ( var file in files )
            {
                if (file.Location == AdminShellNS.AdminShellPackageSupplementaryFile.LocationType.InPackage)
                {                    
                    WriteFile(adminShellPackageEnv.GetLocalStreamFromPackage(file.Uri.ToString()), assetid+file.Uri.ToString());
                    //ReadFile(assetid + file.Uri.ToString());
                }
            }
            
        }
    }
}
