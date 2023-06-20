using DataTransferObjects;
using DataTransferObjects.MetadataDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers
{
    public class MappingService : IMappingService
    {
        public IDTO Map(IClass source, string mappingResolverKey)
        {
            if (mappingResolverKey == null)
            {
                throw new Exception($"Could not resolve serializer modifier mapper.");
            }

            //TODO:jtikekar Refactor
            if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                return ResponseMetadataMapper.Map(source);
            }
            else
            {
                throw new Exception($"Invalid modifier mapping resolved key");
            }
        }

        public List<IDTO> Map(List<IClass> sourceList, string mappingResolverKey)
        {
            if (mappingResolverKey == null)
            {
                throw new Exception($"Could not resolve serializer modifier mapper.");
            }

            //TODO:jtikekar Refactor
            if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                var output = new List<IDTO>();

                foreach (var source in sourceList)
                {
                    var dto = ResponseMetadataMapper.Map(source);
                    output.Add(dto);
                }

                return output;
            }
            else
            {
                throw new Exception($"Invalid modifier mapping resolved key");
            }
        }

        public IClass Map(IMetadataDTO metadataDTO, string mappingResolverKey)
        {
            if (mappingResolverKey == null)
            {
                throw new Exception($"Could not resolve serializer modifier mapper.");
            }

            //TODO:jtikekar Refactor
            if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                return RequestMetadataMapper.Map(metadataDTO);
            }
            else
            {
                throw new Exception($"Invalid modifier mapping resolved key");
            }
        }
    }
}
