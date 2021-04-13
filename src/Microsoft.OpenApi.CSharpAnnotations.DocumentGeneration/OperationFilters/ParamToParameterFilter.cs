// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationFilters
{
    /// <summary>
    /// Parses the value of param tag in xml documentation and apply that as parameter in operation.
    /// </summary>
    public class ParamToParameterFilter : IOperationFilter
    {
        /// <summary>
        /// Fetches the value of "param" tags from xml documentation and populates operation's parameters values.
        /// </summary>
        /// <param name="operation">The operation to be updated.</param>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <param name="settings">The operation filter settings.</param>
        /// <returns>The list of generation errors(if any) produced when processing the filter.</returns>
        /// <remarks>
        /// Care should be taken to not overwrite the existing value in Operation if already present.
        /// This guarantees the predictable behavior that the first tag in the XML will be respected.
        /// It also guarantees that common annotations in the config file do not overwrite the
        /// annotations in the main documentation.
        /// </remarks>
        public IList<GenerationError> Apply(
            OpenApiOperation operation,
            XElement element,
            OperationFilterSettings settings)
        {
            var generationErrors = new List<GenerationError>();

            try
            {
                var paramElements = element.Elements()
                    .Where(
                        p => p.Name == KnownXmlStrings.Param)
                    .ToList();

                // Query paramElements again to get all the parameter elements that have "in" attribute.
                // This will include those whose "in" attribute were just populated in PopulateInAttributeFilter, but exclude
                // those that have "in" attribute being "body" since they will be handled as a request body.
                var paramElementsWithIn = paramElements.Where(
                        p =>
                            KnownXmlStrings.InValuesTranslatableToParameter.Contains(
                                p.Attribute(KnownXmlStrings.In)?.Value))
                    .ToList();

                var generationContext = settings.GenerationContext;

                foreach (var paramElement in paramElementsWithIn)
                {
                    var inValue = paramElement.Attribute(KnownXmlStrings.In)?.Value.Trim();
                    var name = paramElement.Attribute(KnownXmlStrings.Name)?.Value.Trim();
                    var mediaType = paramElement.Attribute(KnownXmlStrings.Type)?.Value ?? "";

                    if (settings.RemoveRoslynDuplicateStringFromParamName)
                    {
                        name = name.RemoveRoslynDuplicateString();
                    }

                    if (inValue == KnownXmlStrings.Path &&
                        !settings.Path.Contains($"{{{name}}}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var isRequired = paramElement.Attribute(KnownXmlStrings.Required)?.Value.Trim();
                    var cref = paramElement.Attribute(KnownXmlStrings.Cref)?.Value.Trim();

                    var description = paramElement.GetDescriptionTextFromLastTextNode();

                    var allListedTypes = paramElement.GetListedTypes();

                    OpenApiSchema schema = new OpenApiSchema();

                    if (!allListedTypes.Any())
                    {
                        // Set default schema as string.
                        schema = new OpenApiSchema()
                        {
                            Type = "string"
                        };
                    }

                    var crefKey = allListedTypes.ToCrefKey();

                    if (generationContext.CrefToSchemaMap.ContainsKey(crefKey))
                    {
                        var schemaInfo = generationContext.CrefToSchemaMap[crefKey];

                        if (schemaInfo.Error != null)
                        {
                            generationErrors.Add(schemaInfo.Error);

                            return generationErrors;
                        }

                        schemaInfo.Schema.CopyInto(schema);
                    }

                    var parameterLocation = GetParameterKind(inValue);

                    var examples = paramElement.ToOpenApiExamples(
                        generationContext.CrefToFieldValueMap,
                        generationErrors);
                   
                    var schemaReferenceDefaultVariant = generationContext
                        .VariantSchemaReferenceMap[DocumentVariantInfo.Default];

                    if (examples.Any())
                    {
                        var firstExample = examples.First().Value?.Value;

                        if (firstExample != null)
                        {
                            if (schema.Reference != null)
                            {
                                if (schemaReferenceDefaultVariant.ContainsKey(schema.Reference.Id))
                                {
                                    schemaReferenceDefaultVariant[schema.Reference.Id].Example = firstExample;
                                }
                            }
                            else
                            {
                                schema.Example = firstExample;
                            }
                        }
                    }

                    var openApiParameter = new OpenApiParameter
                    {
                        Name = name,
                        In = parameterLocation,
                        Description = description,
                        Required = parameterLocation == ParameterLocation.Path || Convert.ToBoolean(isRequired),
                        Examples = examples.Any() ? examples : null
                    };

                    // If media type is specified add parameter as content for complex serialization
                    if (!string.IsNullOrEmpty(mediaType)) {
                        openApiParameter.Content = new Dictionary<string, OpenApiMediaType>
                        {
                            [mediaType] = new OpenApiMediaType { Schema = schema }
                        };
                    }
                    else {
                        openApiParameter.Schema = schema;
                    }

                    operation.Parameters.Add(openApiParameter);
                }
            }
            catch(Exception ex)
            {
                generationErrors.Add(
                    new GenerationError
                    {
                        Message = ex.Message,
                        ExceptionType = ex.GetType().Name
                    });
            }

            return generationErrors;
        }

        private static ParameterLocation GetParameterKind(string parameterKind)
        {
            switch (parameterKind)
            {
                case KnownXmlStrings.Header:
                    return ParameterLocation.Header;

                case KnownXmlStrings.Query:
                    return ParameterLocation.Query;

                default:
                    return ParameterLocation.Path;
            }
        }
    }
}