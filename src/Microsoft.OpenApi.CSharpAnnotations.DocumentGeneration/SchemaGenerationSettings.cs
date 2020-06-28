// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Specifies the settings for schema generation.
    /// </summary>
    [Serializable]
    public class SchemaGenerationSettings
    {
        /// <summary>
        /// Creates an instance of <see cref="SchemaGenerationSettings"/> class.
        /// </summary>
        /// <param name="propertyNameResolver">The property name resolver.</param>
        /// <param name="schemaIdResolver"></param>
        public SchemaGenerationSettings(IPropertyNameResolver propertyNameResolver, ISchemaIdResolver schemaIdResolver = null)
        {
            PropertyNameResolver = propertyNameResolver
                                   ?? throw new ArgumentNullException(nameof(propertyNameResolver));
            SchemaIdResolver = schemaIdResolver ?? new DefaultSchemaIdResolver();
        }

        /// <summary>
        /// Gets the property name resolver.
        /// </summary>
        public IPropertyNameResolver PropertyNameResolver { get; }

        /// <summary>
        /// Gets the schema identifier resolver.
        /// </summary>
        public ISchemaIdResolver SchemaIdResolver { get; }

        /// <summary>
        /// Gets or sets a value indicating whether generator should support inheritance, i.e add allOf and oneOf attributes.
        /// </summary>
        public bool IncludeInheritanceInformation { get; set; }

        /// <summary>
        /// Gets or sets the discriminator resolver.
        /// </summary>
        public IDiscriminatorResolver DiscriminatorResolver { get; set; }
    }
}