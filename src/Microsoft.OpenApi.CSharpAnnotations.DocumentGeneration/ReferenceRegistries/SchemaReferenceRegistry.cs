// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries
{
    /// <summary>
    /// Reference Registry for <see cref="OpenApiSchema"/>
    /// </summary>
    public class SchemaReferenceRegistry : ReferenceRegistry<Type, OpenApiSchema>
    {
        private readonly SchemaGenerationSettings _schemaGenerationSettings;
        private IPropertyNameResolver _propertyNameResolver;
        private readonly Dictionary<string, string> _propertyDescriptionMap = new Dictionary<string, string>();

        /// <summary>
        /// Creates an instance of <see cref="SchemaReferenceRegistry"/>.
        /// </summary>
        /// <param name="schemaGenerationSettings">The schema generation settings.</param>
        public SchemaReferenceRegistry(SchemaGenerationSettings schemaGenerationSettings)
        {
            _schemaGenerationSettings = schemaGenerationSettings
                ?? throw new ArgumentNullException(nameof(schemaGenerationSettings));
            _propertyNameResolver = schemaGenerationSettings.PropertyNameResolver;
        }

        /// <summary>
        /// Creates an instance of <see cref="SchemaReferenceRegistry"/>.
        /// </summary>
        /// <param name="schemaGenerationSettings">The schema generation settings.</param>
        /// <param name="propertyDescriptionMap">The property description map.</param>
        public SchemaReferenceRegistry( SchemaGenerationSettings schemaGenerationSettings,
            Dictionary<string, string> propertyDescriptionMap )
        {
            _schemaGenerationSettings = schemaGenerationSettings
                ?? throw new ArgumentNullException( nameof( schemaGenerationSettings ) );
            _propertyNameResolver = schemaGenerationSettings.PropertyNameResolver;
            _propertyDescriptionMap = propertyDescriptionMap
                ?? throw new ArgumentNullException( nameof( propertyDescriptionMap ) );
        }

        /// <summary>
        /// The dictionary containing all references of the given type.
        /// </summary>
        public override IDictionary<string, OpenApiSchema> References { get; } =
            new Dictionary<string, OpenApiSchema>();

        /// <summary>
        /// Finds the existing reference object based on the key from the input or creates a new one.
        /// </summary>
        /// <returns>The existing or created reference object.</returns>
        internal override OpenApiSchema FindOrAddReference(Type input)
        {
            // Return empty schema when the type does not have a name. 
            // This can occur, for example, when a generic type without the generic argument specified
            // is passed in.
            if (input == null || input.FullName == null)
            {
                return new OpenApiSchema();
            }

            var key = GetKey(input);

            // If the schema already exists in the References, simply return.
            if (References.ContainsKey(key))
            {
                return new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Id = key,
                        Type = ReferenceType.Schema
                    }
                };
            }

            try
            {
                // There are multiple cases for input types that should be handled differently to match the OpenAPI spec.
                //
                // 1. Simple Type
                // 2. Enum Type
                // 3. Dictionary Type
                // 4. Enumerable Type
                // 5. Object Type
                var schema = new OpenApiSchema();

                if (input.IsSimple())
                {
                    schema = input.MapToOpenApiSchema();

                    // Certain simple types yield more specific information.
                    if (input == typeof(char))
                    {
                        schema.MinLength = 1;
                        schema.MaxLength = 1;
                    }
                    else if (input == typeof(Guid))
                    {
                        schema.Example = new OpenApiString(Guid.Empty.ToString());
                    }

                    return schema;
                }

                if (input == typeof(Stream) || input.BaseType == typeof(Stream))
                {
                    schema.Type = "string";
                    schema.Format = "binary";

                    return schema;
                }

                if (input.IsEnum)
                {
                    schema.Type = "string";
                    foreach (var name in Enum.GetNames(input))
                    {
                        schema.Enum.Add(new OpenApiString(name));
                    }

                    return schema;
                }

                if (input.IsDictionary())
                {
                    schema.Type = "object";
                    schema.AdditionalProperties = FindOrAddReference(input.GetGenericArguments()[1]);

                    return schema;
                }

                if (input.IsEnumerable())
                {
                    schema.Type = "array";

                    schema.Items = FindOrAddReference(input.GetEnumerableItemType());

                    return schema;
                }

                var nullableUnderlyingType = Nullable.GetUnderlyingType(input);

                if (nullableUnderlyingType?.IsEnum == true)
                {
                    schema.Type = "string";
                    schema.Nullable = true;

                    foreach (var name in nullableUnderlyingType.GetEnumNames())
                    {
                        schema.Enum.Add(new OpenApiString(name));
                    }

                    return schema;
                }

                schema.Type = "object";

                // Note this assignment is necessary to allow self-referencing type to finish
                // without causing stack overflow.
                // We can also assume that the schema is an object type at this point.
                References[key] = schema;

                var propertyNameDeclaringTypeMap = new Dictionary<string, Type>();
                var typeAttributes = input.GetCustomAttributes(false);

                foreach (var typeAttribute in typeAttributes)
                {
                    if (typeAttribute.GetType().FullName == "Newtonsoft.Json.JsonObjectAttribute")
                    {
                        var type = typeAttribute.GetType();
                        var namingStrategyInfo = type.GetProperty("NamingStrategyType");
                        if (namingStrategyInfo != null)
                        {
                            var namingStrategyValue = namingStrategyInfo.GetValue(typeAttribute, null);

                            if (namingStrategyValue?.ToString()
                                == "Newtonsoft.Json.Serialization.CamelCaseNamingStrategy")
                            {
                                _propertyNameResolver = new CamelCasePropertyNameResolver();
                            }
                        }
                    }
                }

                var a = input.FullName;
                    
                PropertyInfo[] propertyInfos;
                if (_schemaGenerationSettings.IncludeInheritanceInformation)
                {
                    propertyInfos = input.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                
                    if (input.BaseType != typeof(object))
                    {
                        var baseClassSchema = FindOrAddReference(input.BaseType);

                        schema.AllOf.Add(baseClassSchema);
                    }

                    if (_schemaGenerationSettings.DiscriminatorResolver != null)
                    {
                        var xmlIncludeAttributes = input.GetCustomAttributes<XmlIncludeAttribute>(false).ToArray();

                        if (xmlIncludeAttributes.Length > 0)
                        {
                            var discriminatorProperty = _schemaGenerationSettings.DiscriminatorResolver.ResolveProperty(input);

                            if (discriminatorProperty != null)
                            {
                                var openApiDiscriminator = new OpenApiDiscriminator()
                                {
                                    PropertyName = discriminatorProperty.Name,
                                    Mapping = new Dictionary<string, string>()
                                };

                                schema.Discriminator = openApiDiscriminator;

                                foreach (var xmlIncludeAttribute in xmlIncludeAttributes)
                                {
                                    var childSchema = FindOrAddReference(xmlIncludeAttribute.Type);
                                    
                                    schema.OneOf.Add(childSchema);

                                    var mappingKey = _schemaGenerationSettings.DiscriminatorResolver.ResolveMappingKey(discriminatorProperty, xmlIncludeAttribute.Type);

                                    openApiDiscriminator.Mapping[mappingKey] = childSchema.Reference.ReferenceV3;
                                }

                            }
                        }
                    }
                }
                else
                {
                    propertyInfos = input.GetProperties();
                }

                foreach (var propertyInfo in propertyInfos)
                {
                    var ignoreProperty = false;

                    var innerSchema = FindOrAddReference(propertyInfo.PropertyType);

                    var propertyName = _propertyNameResolver.ResolvePropertyName(propertyInfo);

                    // Construct property name like it shows up in documentation xml.
                    var propertyFullName = propertyInfo.DeclaringType.Namespace + "." +
                        propertyInfo.DeclaringType?.Name + "." + propertyInfo.Name;

                    if ( this._propertyDescriptionMap.ContainsKey( propertyFullName ) )
                    {
                        innerSchema.Description = this._propertyDescriptionMap[propertyFullName];
                    }

                    var attributes = propertyInfo.GetCustomAttributes(false);

                    foreach (var attribute in attributes)
                    {
                        if (attribute.GetType().FullName == "Newtonsoft.Json.JsonPropertyAttribute")
                        {
                            var type = attribute.GetType();
                            var requiredPropertyInfo = type.GetProperty("Required");

                            if (requiredPropertyInfo != null)
                            {
                                var requiredValue = Enum.GetName(
                                    requiredPropertyInfo.PropertyType,
                                    requiredPropertyInfo.GetValue(attribute, null));

                                if (requiredValue == "Always")
                                {
                                    schema.Required.Add(propertyName);
                                }
                            }
                        }

                        if (attribute.GetType().FullName == "Newtonsoft.Json.JsonIgnoreAttribute")
                        {
                            ignoreProperty = true;
                        }
                    }

                    if (ignoreProperty)
                    {
                        continue;
                    }

                    var propertyDeclaringType = propertyInfo.DeclaringType;

                    if (propertyNameDeclaringTypeMap.ContainsKey(propertyName))
                    {
                        var existingPropertyDeclaringType = propertyNameDeclaringTypeMap[propertyName];
                        var duplicateProperty = true;

                        if (existingPropertyDeclaringType != null && propertyDeclaringType != null)
                        {
                            if (propertyDeclaringType.IsSubclassOf(existingPropertyDeclaringType)
                                || (existingPropertyDeclaringType.IsInterface
                                && propertyDeclaringType.ImplementInterface(existingPropertyDeclaringType)))
                            {
                                // Current property is on a derived class and hides the existing
                                schema.Properties[propertyName] = innerSchema;
                                duplicateProperty = false;
                            }

                            if (existingPropertyDeclaringType.IsSubclassOf(propertyDeclaringType)
                                || (propertyDeclaringType.IsInterface
                                && existingPropertyDeclaringType.ImplementInterface(propertyDeclaringType)))
                            {
                                // current property is hidden by the existing so don't add it
                                continue;
                            }
                        }

                        if (duplicateProperty)
                        {
                            throw new AddingSchemaReferenceFailedException(
                                key,
                                string.Format(
                                    SpecificationGenerationMessages.DuplicateProperty,
                                    propertyName,
                                    input));
                        }
                    }

                    schema.Properties[propertyName] = innerSchema;
                    propertyNameDeclaringTypeMap.Add(propertyName, propertyDeclaringType);
                }

                References[key] = schema;

                return new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Id = key,
                        Type = ReferenceType.Schema
                    }
                };
            }
            catch (Exception e)
            {
                // Something went wrong while fetching schema, so remove the key if exists from the references.
                if (References.ContainsKey(key))
                {
                    References.Remove(key);
                }

                throw new AddingSchemaReferenceFailedException(key, e.Message);
            }
        }

        /// <summary>
        /// Gets the key from the input object to use as reference string.
        /// </summary>
        /// <remarks>
        /// This must match the regular expression ^[a-zA-Z0-9\.\-_]+$ due to OpenAPI V3 spec.
        /// </remarks>
        internal override string GetKey(Type input)
        {
            return _schemaGenerationSettings.SchemaIdResolver.ResolveSchemaId(input);
        }
    }
}