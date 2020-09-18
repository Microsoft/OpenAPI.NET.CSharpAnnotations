// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.AssemblyLoader;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.DocumentConfigFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.DocumentFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationConfigFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PostProcessingDocumentFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Provides functionality to parse xml documentation and contract assemblies into OpenAPI documents.
    /// </summary>
    internal class InternalOpenApiGenerator
    {
        private readonly IList<IDocumentConfigFilter> _documentConfigFilters;
        private readonly IList<IDocumentFilter> _documentFilters;
        private readonly OpenApiDocumentGenerationSettings _openApiDocumentGenerationSettings;
        private readonly IList<IOperationConfigFilter> _operationConfigFilters;
        private readonly IList<IOperationFilter> _operationFilters;
        private readonly IList<IPostProcessingDocumentFilter> _postProcessingDocumentFilters;
        private readonly IList<IPreProcessingOperationFilter> _preProcessingOperationFilters;
        private readonly OpenApiStringReader _openApiStringReader = new OpenApiStringReader();

        /// <summary>
        /// Creates a new instance of <see cref="InternalOpenApiGenerator"/>.
        /// </summary>
        /// <param name="openApiGeneratorFilterConfig">The configuration encapsulating all the filters
        /// that will be applied while generating/processing OpenAPI document from C# annotations.</param>
        /// <param name="openApiDocumentGenerationSettings">The settings to use for Open API document generation.
        /// </param>
        public InternalOpenApiGenerator(
            OpenApiGeneratorFilterConfig openApiGeneratorFilterConfig,
            OpenApiDocumentGenerationSettings openApiDocumentGenerationSettings)
        {
            if (openApiGeneratorFilterConfig == null)
            {
                throw new ArgumentNullException(nameof(openApiGeneratorFilterConfig));
            }
            _openApiDocumentGenerationSettings = openApiDocumentGenerationSettings
                ?? throw new ArgumentNullException(nameof(openApiDocumentGenerationSettings));

            _documentFilters = TypeCastFilters<IDocumentFilter>(openApiGeneratorFilterConfig.Filters);
            _documentConfigFilters = TypeCastFilters<IDocumentConfigFilter>(openApiGeneratorFilterConfig.Filters);
            _operationFilters = TypeCastFilters<IOperationFilter>(openApiGeneratorFilterConfig.Filters);
            _operationConfigFilters = TypeCastFilters<IOperationConfigFilter>(openApiGeneratorFilterConfig.Filters);
            _preProcessingOperationFilters = TypeCastFilters<IPreProcessingOperationFilter>(
                openApiGeneratorFilterConfig.Filters);
            _postProcessingDocumentFilters = TypeCastFilters<IPostProcessingDocumentFilter>(
                openApiGeneratorFilterConfig.Filters);
        }

        /// <summary>
        /// Add operation and update the operation filter settings based on the given document variant info.
        /// </summary>
        private void AddOperation(
            IDictionary<DocumentVariantInfo, OpenApiDocument> specificationDocuments,
            IDictionary<DocumentVariantInfo, ReferenceRegistryManager> referenceRegistryManagerMap,
            List<GenerationError> operationGenerationErrors,
            DocumentVariantInfo documentVariantInfo,
            XElement operationElement,
            XElement operationConfigElement,
            GenerationContext generationContext)
        {
            var paths = new OpenApiPaths();

            foreach (var preprocessingOperationFilter in _preProcessingOperationFilters)
            {
                var generationErrors = preprocessingOperationFilter.Apply(
                        paths,
                        operationElement,
                        new PreProcessingOperationFilterSettings());

                operationGenerationErrors.AddRange(generationErrors);
            }

            if (!referenceRegistryManagerMap.ContainsKey(documentVariantInfo))
            {
                referenceRegistryManagerMap[documentVariantInfo] =
                    new ReferenceRegistryManager(_openApiDocumentGenerationSettings);
            }

            foreach (var pathToPathItem in paths)
            {
                var path = pathToPathItem.Key;
                var pathItem = pathToPathItem.Value;

                foreach (var operationMethodToOperation in pathItem.Operations)
                {
                    var operationMethod = operationMethodToOperation.Key;
                    var operation = operationMethodToOperation.Value;

                    var operationFilterSettings = new OperationFilterSettings
                    {
                        GenerationContext = generationContext,
                        ReferenceRegistryManager = referenceRegistryManagerMap[documentVariantInfo],
                        Path = path,
                        OperationMethod = operationMethod.ToString(),
                        RemoveRoslynDuplicateStringFromParamName = _openApiDocumentGenerationSettings
                            .RemoveRoslynDuplicateStringFromParamName
                    };

                    // Apply all the operation-related filters to extract information related to the operation.
                    // It is important that these are applied before the config filters below
                    // since the config filters may rely on information generated from operation filters.
                    foreach (var operationFilter in _operationFilters)
                    {
                        var generationErrors = operationFilter.Apply(
                                operation,
                                operationElement,
                                operationFilterSettings);

                        operationGenerationErrors.AddRange(generationErrors);
                    }

                    if (operationConfigElement != null)
                    {
                        // Apply the config-related filters to extract information from the config xml
                        // that can be applied to the operations.
                        foreach (var configFilter in _operationConfigFilters)
                        {
                            var generationErrors = configFilter.Apply(
                                    operation,
                                    operationConfigElement,
                                    new OperationConfigFilterSettings
                                    {
                                        OperationFilterSettings = operationFilterSettings,
                                        OperationFilters = _operationFilters
                                    });

                            operationGenerationErrors.AddRange(generationErrors);
                        }
                    }

                    // Add the processed operation to the specification document.
                    if (!specificationDocuments.ContainsKey(documentVariantInfo))
                    {
                        specificationDocuments.Add(
                            documentVariantInfo,
                            new OpenApiDocument
                            {
                                Components = new OpenApiComponents(),
                                Paths = new OpenApiPaths()
                            });
                    }

                    // Copy operations from local Paths object to the Paths in the specification document.
                    var documentPaths = specificationDocuments[documentVariantInfo].Paths;

                    if (!documentPaths.ContainsKey(path))
                    {
                        documentPaths.Add(
                            path,
                            new OpenApiPathItem
                            {
                                Operations =
                                {
                                    [operationMethod] = operation
                                }
                            });
                    }
                    else
                    {
                        if (documentPaths[path].Operations.ContainsKey(operationMethod))
                        {
                            throw new DuplicateOperationException(
                                path,
                                operationMethod.ToString(),
                                documentVariantInfo.Title);
                        }

                        documentPaths[path].Operations.Add(operationMethod, operation);
                    }
                }
            }
        }

        /// <summary>
        /// Takes in annotation xml document and returns the OpenAPI document generation result
        /// which contains OpenAPI specification document(s).
        /// </summary>
        /// <param name="annotationXmlDocuments">The list of XDocuments representing annotation xmls.</param>
        /// <param name="contractAssemblyPaths">The contract assembly paths.</param>
        /// <param name="configurationXml">The serialized XDocument representing the generation configuration.</param>
        /// <param name="openApiDocumentVersion">The version of the OpenAPI document.</param>
        /// <param name="openApiInfoDescription">The description to use while populating OpenApiInfo.</param>
        /// <param name="generationDiagnostic">A string representing serialized version of
        /// <see cref="GenerationDiagnostic"/>>
        /// </param>
        /// <returns>
        /// Dictionary mapping document variant metadata to their respective OpenAPI document.
        /// </returns>
        public IDictionary<DocumentVariantInfo, OpenApiDocument> GenerateOpenApiDocuments(
            IList<XDocument> annotationXmlDocuments,
            IList<string> contractAssemblyPaths,
            string configurationXml,
            string openApiDocumentVersion,
            string openApiInfoDescription,
            out GenerationDiagnostic generationDiagnostic)
        {
            IDictionary<DocumentVariantInfo, OpenApiDocument> openApiDocuments
                = new Dictionary<DocumentVariantInfo, OpenApiDocument>();

            var operationElements = new List<XElement>();
            var propertyElements = new List<XElement>();

            foreach (var annotationXmlDocument in annotationXmlDocuments)
            {
                operationElements.AddRange(
                    annotationXmlDocument.XPathSelectElements("//doc/members/member[url and verb]"));

                propertyElements.AddRange(annotationXmlDocument.XPathSelectElements("//doc/members/member")
                    .Where(
                        m => m.Attribute(KnownXmlStrings.Name) != null &&
                             m.Attribute(KnownXmlStrings.Name).Value.StartsWith("P:")));
            }

            XElement operationConfigElement = null;

            XElement documentConfigElement = null;

            var documentVariantElementNames = new List<string>();

            if (!string.IsNullOrWhiteSpace(configurationXml))
            {
                var configurationXmlDocument = XDocument.Parse(configurationXml);

                operationConfigElement = configurationXmlDocument.XPathSelectElement("//configuration/operation");
                documentConfigElement = configurationXmlDocument.XPathSelectElement("//configuration/document");
                documentVariantElementNames = configurationXmlDocument
                    .XPathSelectElements("//configuration/document/variant/name")
                    .Select(variantName => variantName.Value)
                    .ToList();
            }

            if (!operationElements.Any())
            {
                generationDiagnostic = new GenerationDiagnostic
                {
                    DocumentGenerationDiagnostic = new DocumentGenerationDiagnostic
                    {
                        Errors =
                        {
                            new GenerationError
                            {
                                Message = SpecificationGenerationMessages.NoOperationElementFoundToParse
                            }
                        }
                    }
                };

                return openApiDocuments;
            }

            try
            {
                var propertyNameResolverTypeName = _openApiDocumentGenerationSettings.SchemaGenerationSettings
                    .PropertyNameResolver.GetType().FullName;

                var internalGenerationContext = new InternalGenerationContext();
                var internalSchemaGenerationSettings = new InternalSchemaGenerationSettings()
                {
                    PropertyNameResolverName = propertyNameResolverTypeName
                };

                generationDiagnostic = new GenerationDiagnostic();
                var documentGenerationDiagnostic = new DocumentGenerationDiagnostic();

                if (documentVariantElementNames?.Count > 1)
                {
                    documentGenerationDiagnostic.Errors.Add(new GenerationError
                    {
                        Message = string.Format(
                            SpecificationGenerationMessages.MoreThanOneVariantNameNotAllowed,
                            documentVariantElementNames.First())
                    });
                }

                IList<string> serializedOperationElements = operationElements.Select(i => i.ToString()).ToList();

                // Operation config elements can contain the types that needs to be fetched too,
                // so add it to the list of operation elements which will be used to fetch type information.
                if (operationConfigElement!=null)
                {
                    serializedOperationElements.Add(operationConfigElement.ToString());
                }

#if !NETFRAMEWORK
                var assemblyLoader = new AssemblyLoader.AssemblyLoader();
                assemblyLoader.RegisterAssemblyPaths(contractAssemblyPaths);
                var internalGenerationContextAsString = new AssemblyLoader.AssemblyLoader().BuildInternalGenerationContext(
                    contractAssemblyPaths,
                    serializedOperationElements,
                    propertyElements.Select(i => i.ToString()).ToList(),
                    documentVariantElementNames.FirstOrDefault(),
                    internalSchemaGenerationSettings);

                internalGenerationContext =
                      (InternalGenerationContext)JsonConvert.DeserializeObject(
                          internalGenerationContextAsString,
                          typeof(InternalGenerationContext));
#else
                using (var isolatedDomain = new AppDomainCreator<AssemblyLoader.AssemblyLoader>())
                {
                    isolatedDomain.Object.RegisterAssemblyPaths(contractAssemblyPaths);
                    var internalGenerationContextAsString = isolatedDomain.Object.BuildInternalGenerationContext(
                        contractAssemblyPaths,
                        serializedOperationElements,
                        propertyElements.Select(i => i.ToString()).ToList(),
                        documentVariantElementNames.FirstOrDefault(),
                        internalSchemaGenerationSettings);

                    internalGenerationContext =
                        (InternalGenerationContext)JsonConvert.DeserializeObject(
                            internalGenerationContextAsString,
                            typeof(InternalGenerationContext));
                }              
#endif

                GenerationContext generationContext = internalGenerationContext.ToGenerationContext();

                var operationGenerationDiagnostics = GenerateSpecificationDocuments(
                    generationContext,
                    operationElements,
                    operationConfigElement,
                    documentVariantElementNames.FirstOrDefault(),
                    out var documents);

                foreach (var operationGenerationDiagnostic in operationGenerationDiagnostics)
                {
                    generationDiagnostic.OperationGenerationDiagnostics.Add(
                        new OperationGenerationDiagnostic(operationGenerationDiagnostic));
                }

                var referenceRegistryManager = new ReferenceRegistryManager(_openApiDocumentGenerationSettings);

                foreach (var variantInfoDocumentValuePair in documents)
                {
                    var openApiDocument = variantInfoDocumentValuePair.Value;

                    foreach (var documentFilter in _documentFilters)
                    {
                        var generationErrors = documentFilter.Apply(
                                openApiDocument,
                                annotationXmlDocuments,
                                new DocumentFilterSettings
                                {
                                    OpenApiDocumentVersion = openApiDocumentVersion,
                                    OpenApiInfoDescription = openApiInfoDescription,
                                    ReferenceRegistryManager = referenceRegistryManager
                                },
                                _openApiDocumentGenerationSettings);

                        foreach(var error in generationErrors)
                        {
                            documentGenerationDiagnostic.Errors.Add(error);
                        }
                    }

                    foreach (var filter in _postProcessingDocumentFilters)
                    {
                        var generationErrors = filter.Apply(
                            openApiDocument,
                            new PostProcessingDocumentFilterSettings
                            {
                                OperationGenerationDiagnostics = operationGenerationDiagnostics
                            });

                        foreach (var error in generationErrors)
                        {
                            documentGenerationDiagnostic.Errors.Add(error);
                        }
                    }

                    referenceRegistryManager.SecuritySchemeReferenceRegistry.References.CopyInto(
                        openApiDocument.Components.SecuritySchemes);
                }

                if (documentConfigElement != null)
                {
                    foreach (var documentConfigFilter in _documentConfigFilters)
                    {
                        var generationErrors = documentConfigFilter.Apply(
                                documents,
                                documentConfigElement,
                                annotationXmlDocuments,
                                new DocumentConfigFilterSettings());

                        foreach (var error in generationErrors)
                        {
                            documentGenerationDiagnostic.Errors.Add(error);
                        }
                    }
                }

                var failedOperations = generationDiagnostic.OperationGenerationDiagnostics
                    .Where(i => i.Errors.Count > 0);

                if (failedOperations.Any())
                {
                    var totalOperationsCount = generationDiagnostic.OperationGenerationDiagnostics.Count();

                    var exception = new UnableToGenerateAllOperationsException(
                        totalOperationsCount - failedOperations.Count(), totalOperationsCount);

                    documentGenerationDiagnostic.Errors.Add(
                        new GenerationError
                        {
                            ExceptionType = exception.GetType().Name,
                            Message = exception.Message
                        });
                }

                generationDiagnostic.DocumentGenerationDiagnostic = documentGenerationDiagnostic;
                return documents;
            }
            catch (Exception e)
            {
                generationDiagnostic = new GenerationDiagnostic
                {
                    DocumentGenerationDiagnostic =
                        new DocumentGenerationDiagnostic
                        {
                            Errors =
                            {
                                new GenerationError
                                {
                                    ExceptionType = e.GetType().Name,
                                    Message = string.Format(SpecificationGenerationMessages.UnexpectedError, e)
                                }
                            }
                        }
                };

                return openApiDocuments;
            }
        }

        /// <summary>
        /// Populate the specification documents for all document variant infos.
        /// </summary>
        /// <returns>The operation generation results from populating the specification documents.</returns>
        private IList<OperationGenerationDiagnostic> GenerateSpecificationDocuments(
            GenerationContext generationContext,
            IList<XElement> operationElements,
            XElement operationConfigElement,
            string documentVariantElementName,
            out IDictionary<DocumentVariantInfo, OpenApiDocument> specificationDocuments)
        {
            specificationDocuments = new Dictionary<DocumentVariantInfo, OpenApiDocument>();

            var operationGenerationResults = new List<OperationGenerationDiagnostic>();

            var referenceRegistryManagerMap = new Dictionary<DocumentVariantInfo, ReferenceRegistryManager>();

            foreach (var operationElement in operationElements)
            {
                string url;
                OperationType operationMethod;

                try
                {
                    url = OperationHandler.GetUrl(operationElement);
                }
                catch (InvalidUrlException e)
                {
                    operationGenerationResults.Add(
                        new OperationGenerationDiagnostic
                        {
                            Errors =
                            {
                                new GenerationError
                                {
                                    ExceptionType = e.GetType().Name,
                                    Message = e.Message
                                }
                            },
                            OperationMethod = SpecificationGenerationMessages.OperationMethodNotParsedGivenUrlIsInvalid,
                            Path = e.Url
                        });

                    continue;
                }

                try
                {
                    operationMethod = OperationHandler.GetOperationMethod(operationElement);
                }
                catch (InvalidVerbException e)
                {
                    operationGenerationResults.Add(
                        new OperationGenerationDiagnostic
                        {
                            Errors =
                            {
                                new GenerationError
                                {
                                    ExceptionType = e.GetType().Name,
                                    Message = e.Message
                                }
                            },
                            OperationMethod = e.Verb,
                            Path = url
                        });

                    continue;
                }

                var operationGenerationErrors = new List<GenerationError>();

                try
                {
                    AddOperation(
                        specificationDocuments,
                        referenceRegistryManagerMap,
                        operationGenerationErrors,
                        DocumentVariantInfo.Default,
                        operationElement,
                        operationConfigElement,
                        generationContext);
                }
                catch (Exception e)
                {
                    operationGenerationErrors.Add(
                        new GenerationError
                        {
                            ExceptionType = e.GetType().Name,
                            Message = e.Message
                        });
                }

                var customElements = operationElement.Descendants(documentVariantElementName);
                foreach (var customElement in customElements)
                {
                    try
                    {
                        var documentVariantInfo = new DocumentVariantInfo
                        {
                            Categorizer = customElement.Name.LocalName.Trim(),
                            Title = customElement.Value.Trim()
                        };

                        AddOperation(
                            specificationDocuments,
                            referenceRegistryManagerMap,
                            operationGenerationErrors,
                            documentVariantInfo,
                            operationElement,
                            operationConfigElement,
                            generationContext);
                    }
                    catch (Exception e)
                    {
                        operationGenerationErrors.Add(
                            new GenerationError
                            {
                                ExceptionType = e.GetType().Name,
                                Message = e.Message
                            });
                    }
                }

                var operationGenerationResult = new OperationGenerationDiagnostic
                {
                    OperationMethod = operationMethod.ToString(),
                    Path = url
                };

                if (operationGenerationErrors.Any())
                {
                    foreach (var error in operationGenerationErrors)
                    {
                        operationGenerationResult.Errors.Add(new GenerationError(error));
                    }
                }

                operationGenerationResults.Add(operationGenerationResult);
            }

            foreach (var documentVariantInfo in generationContext.VariantSchemaReferenceMap.Keys)
            {
                var references = generationContext.VariantSchemaReferenceMap[documentVariantInfo].ToDictionary(
                    k => k.Key,
                    k => k.Value);

                if (specificationDocuments.ContainsKey(documentVariantInfo))
                {
                    references.CopyInto(specificationDocuments[documentVariantInfo].Components.Schemas);
                }
            }

            foreach (var documentVariantInfo in specificationDocuments.Keys)
            {
                referenceRegistryManagerMap[documentVariantInfo]
                    .SecuritySchemeReferenceRegistry.References.CopyInto(
                        specificationDocuments[documentVariantInfo].Components.SecuritySchemes);
            }

            return operationGenerationResults;
        }

        private List<T> TypeCastFilters<T>(FilterSet filtersToTypeCast) where T : IFilter
        {
            return filtersToTypeCast.OfType<T>().ToList();
        }
    }
}