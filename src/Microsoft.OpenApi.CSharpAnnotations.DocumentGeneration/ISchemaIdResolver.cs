using System;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Used by <see cref="SchemaReferenceRegistry"/> to resolve schema id name for a given <see cref="Type"/>.
    /// </summary>
    public interface ISchemaIdResolver
    {
        /// <summary>
        /// Resolves the schema identifier.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        string ResolveSchemaId(Type type);
    }
}
