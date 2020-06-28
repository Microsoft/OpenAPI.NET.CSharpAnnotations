using System;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Used by <see cref="SchemaReferenceRegistry"/> to resolve schema id name for a given <see cref="Type"/>.
    /// </summary>
    [Serializable]
    public class DefaultSchemaIdResolver : ISchemaIdResolver
    {
        /// <inheritdoc />
        public string ResolveSchemaId(Type type)
        {
            // Type.ToString() returns full name for non-generic types and
            // returns a full name without unnecessary assembly information for generic types.
            var typeName = type.ToString();

            return typeName.SanitizeClassName();
        }
    }
}
