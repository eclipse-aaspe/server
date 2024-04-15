using DataTransferObjects;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers
{
    /// <summary>
    /// Interface for mapping between domain classes and DTOs using different mapping strategies.
    /// </summary>
    public interface IMappingService
    {
        /// <summary>
        /// Maps a single source domain class to a DTO based on the specified mapping resolver key.
        /// </summary>
        /// <param name="source">The source domain class to be mapped.</param>
        /// <param name="mappingResolverKey">The key used to resolve the mapping strategy.</param>
        /// <returns>The mapped DTO.</returns>
        IDTO Map(IClass source, string mappingResolverKey);

        /// <summary>
        /// Maps a collection of source domain classes to DTOs based on the specified mapping resolver key.
        /// </summary>
        /// <param name="sourceList">The collection of source domain classes to be mapped.</param>
        /// <param name="mappingResolverKey">The key used to resolve the mapping strategy.</param>
        /// <returns>The list of mapped DTOs.</returns>
        List<IDTO> Map(IEnumerable<IClass> sourceList, string mappingResolverKey);

        /// <summary>
        /// Maps a DTO to a domain class based on the specified mapping resolver key.
        /// </summary>
        /// <param name="dto">The DTO to be mapped.</param>
        /// <param name="mappingResolverKey">The key used to resolve the mapping strategy.</param>
        /// <returns>The mapped domain class.</returns>
        IClass Map(IDTO dto, string mappingResolverKey);
    }
}