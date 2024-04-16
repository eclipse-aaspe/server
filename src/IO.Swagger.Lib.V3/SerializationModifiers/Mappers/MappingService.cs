using DataTransferObjects;
using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers;

/// <inheritdoc />
public class MappingService : IMappingService
{
    private readonly IResponseMetadataMapper _responseMetadataMapper;
    private readonly IResponseValueMapper _responseValueMapper;
    private readonly IRequestMetadataMapper _requestMetadataMapper;
    private readonly IRequestValueMapper _requestValueMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingService"/> class.
    /// </summary>
    /// <param name="responseMetadataMapper">The mapper for response metadata DTOs.</param>
    /// <param name="responseValueMapper">The mapper for response value DTOs.</param>
    /// <param name="requestMetadataMapper">The mapper for request metadata DTOs.</param>
    /// <param name="requestValueMapper">The mapper for request value DTOs.</param>
    public MappingService(IResponseMetadataMapper responseMetadataMapper, IResponseValueMapper responseValueMapper, IRequestMetadataMapper requestMetadataMapper,
        IRequestValueMapper requestValueMapper)
    {
        _responseMetadataMapper = responseMetadataMapper;
        _responseValueMapper = responseValueMapper;
        _requestMetadataMapper = requestMetadataMapper;
        _requestValueMapper = requestValueMapper;
    }

    /// <inheritdoc />
    public IDTO Map(IClass source, string mappingResolverKey)
    {
        if (mappingResolverKey == null)
        {
            throw new Exception($"Could not resolve serializer modifier mapper.");
        }

        if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return _responseMetadataMapper.Map(source);
        }

        if (mappingResolverKey.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            //TODO: somehow it was never seen that this is an issue....
           //return _requestValueMapper.Map(source);
            return null;
        }

        throw new Exception("Invalid modifier mapping resolved key");
    }

    /// <inheritdoc />
    public List<IDTO> Map(IEnumerable<IClass> sourceList, string mappingResolverKey)
    {
        if (mappingResolverKey == null)
        {
            throw new Exception($"Could not resolve serializer modifier mapper.");
        }

        if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return sourceList.Select(_responseMetadataMapper.Map).ToList();
        }

        if (mappingResolverKey.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            return sourceList.Select(_responseValueMapper.Map).Cast<IDTO>().ToList();
        }

        throw new Exception($"Invalid modifier mapping resolved key");
    }

    /// <inheritdoc />
    public IClass Map(IDTO dto, string mappingResolverKey)
    {
        if (mappingResolverKey == null)
        {
            throw new Exception($"Could not resolve serializer modifier mapper.");
        }

        if (mappingResolverKey.Equals("metadata", StringComparison.OrdinalIgnoreCase) && dto is IMetadataDTO metadataDTO)
        {
            return _requestMetadataMapper.Map(metadataDTO);
        }

        if (mappingResolverKey.Equals("value", StringComparison.OrdinalIgnoreCase) && dto is IValueDTO valueDTO)
        {
            return _requestValueMapper.Map(valueDTO);
        }

        throw new Exception($"Invalid modifier mapping resolved key");
    }
}