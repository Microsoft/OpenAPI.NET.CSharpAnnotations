using System;
using System.Reflection;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Utilized by <see cref="SchemaReferenceRegistry"/> to find information about discriminator
    /// </summary>
    public interface IDiscriminatorResolver
    {
        /// <summary>
        /// Resolves the discriminator property
        /// </summary>
        /// <param name="parentType">The parent type.</param>
        /// <returns></returns>
        PropertyInfo ResolveProperty(Type parentType);

        /// <summary>
        /// Resolves the mapping key.
        /// </summary>
        /// <param name="discriminatorProperty">The discriminator property.</param>
        /// <param name="childType">The child type.</param>
        /// <returns></returns>
        string ResolveMappingKey(PropertyInfo discriminatorProperty, Type childType);
    }
}
