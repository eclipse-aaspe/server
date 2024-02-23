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


//Author: Jonas Graubner
//contact: jogithub@graubner-bayern.de
namespace AasxServerStandardBib
{
    public interface IDatabase
    {
        void Initialize(String connectionString);

        #region AssetAdministrationShell
        public void WriteDBAssetAdministrationShell(IAssetAdministrationShell shell);
        public IQueryable<AssetAdministrationShell> getLINQAssetAdministrationShell();
        public void UpdateDBAssetAdministrationShellById(IAssetAdministrationShell body, string aasIdentifier);
        public bool DeleteDBAssetAdministrationShell(IAssetAdministrationShell shell);
        #endregion

        #region Submodel
        public void WriteDBSubmodel(ISubmodel submodel);
        public IQueryable<Submodel> getLINQSubmodel();
        public void UpdateDBSubmodelById(string submodelIdentifier, ISubmodel newSubmodel);
        public void DeleteDBSubmodelById(string submodelIdentifier);
        #endregion

        public void importAASCoreEnvironment(IEnvironment environment);
    }


    public class MongoDatabase : IDatabase
    {
        private MongoClient _client;
        private IMongoDatabase _database;

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
        }
        private IMongoCollection<AssetAdministrationShell> getAasCollection()
        {
            return _database.GetCollection<AssetAdministrationShell>("AssetAdministrationShells");
        }
        private IMongoCollection<Submodel> getSubmodelCollection()
        {
            return _database.GetCollection<Submodel>("Submodels");
        }
        private IMongoCollection<IConceptDescription> getConceptDescriptionCollection()
        {
            return _database.GetCollection<IConceptDescription>("ConceptDescriptions");
        }

        
        public void WriteDBAssetAdministrationShell(IAssetAdministrationShell shell)
        {
            try
            {
                getAasCollection().InsertOne((AssetAdministrationShell)shell);
            } catch (MongoWriteException)
            {
            }
        }
        public void writeDBConceptDescription(IConceptDescription conceptDescription)
        {
            try
            {
                getConceptDescriptionCollection().InsertOne(conceptDescription);
            }
            catch (MongoWriteException)
            {
            }
        }

        #region AssetAdministrationShell
        public bool DeleteDBAssetAdministrationShell(IAssetAdministrationShell shell)
        {
            throw new NotImplementedException();
        }
        public IQueryable<AssetAdministrationShell> getLINQAssetAdministrationShell()
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
        public async void DeleteDBSubmodelById(string submodelIdentifier)
        {
            await getSubmodelCollection().DeleteOneAsync(a => a.Id == submodelIdentifier);
        }
        public IQueryable<Submodel> getLINQSubmodel()
        {
            return getSubmodelCollection().AsQueryable();
        }
        public async void UpdateDBSubmodelById(string submodelIdentifier, ISubmodel newSubmodel)
        {
            await getSubmodelCollection().ReplaceOneAsync(r => r.Id.Equals(submodelIdentifier), (Submodel)newSubmodel);
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
                writeDBConceptDescription(conceptDescription);
            });
        } 
    }
}
