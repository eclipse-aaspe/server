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


//Author: Jonas Graubner
//contact: jogithub@graubner-bayern.de
namespace AasxServerStandardBib
{
    public interface IDatabase
    {
        void Initialize(String connectionString);
        public void writeDB(String collectionName, object data, bool throwError = false);
        public void importAASCoreEnvironment(AasCore.Aas3_0.Environment environment);
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

        public void writeDB(String collectionName, object data, bool throwError = false)
        {
            var collection = _database.GetCollection<object>(collectionName);
            try
            {
                collection.InsertOne(data);
            }
            catch (MongoWriteException ex)
            {
                if (throwError)
                {
                    throw new DuplicateException($"{collectionName} with id {data} already exists.");
                }
            }
        }

        public void importAASCoreEnvironment(AasCore.Aas3_0.Environment environment)
        {
            environment.AssetAdministrationShells.ForEach(shell => {
                writeDB("AssetAdministrationShells", shell);
            });

            environment.Submodels.ForEach(submodel =>
            {
                writeDB("Submodels", submodel);
            });

            environment.ConceptDescriptions.ForEach(conceptDescription =>
            {
                writeDB("ConceptDescriptions", conceptDescription);
            });

        }
    }
}
