// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.ReferenceRegistries;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;

#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.AssemblyLoader
{
#if !NETFRAMEWORK
    /// <summary>
    /// Class is used to load assemblies for .net framework and .net core runtimes.
    /// </summary>
    /// <remarks>
    /// For .net framework, the class is loaded in seperate app domain and CurrentDomain.AssemblyResolve event
    /// handler is used to resolve the assembly conflict.
    /// For .net core custom assembly load context is used to resolve the assemlby conflict.
    /// </remarks>
    internal class AssemblyLoader
    {
        public CustomAssemblyLoadContext Context { get; }

        public AssemblyLoader()
        {
            Context = new CustomAssemblyLoadContext();
            AssemblyLoadContext.Default.Resolving += (context, name) => Context.Resolve(name);
        }
#else
    /// <summary>
    /// Class is used to load assemblies for .net framework and .net core runtimes.
    /// </summary>
    /// <remarks>
    /// For .net framework, the class is loaded in seperate app domain and CurrentDomain.AssemblyResolve event
    /// handler is used to resolve the assembly conflict.
    /// For .net core custom assembly load context is used to resolve the assemlby conflict.
    /// </remarks>
    internal class AssemblyLoader : MarshalByRefObject
    {
        
#endif

        internal void RegisterAssemblyPaths(IList<string> assemblyPaths)
        {
#if !NETFRAMEWORK
            Context.AssemblyPaths = assemblyPaths;
#else
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                Version version = null;

                var separatorIndex = args.Name.IndexOf(",", StringComparison.Ordinal);

                // Fetch assembly name(Newtonsoft.Json) from args value like below
                // Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed
                var assemblyName = separatorIndex > 0 ? args.Name.Substring(0, separatorIndex) : args.Name;

                if (separatorIndex > 0)
                {
                    // Fetch assembly version(11.0.0.0) from args value like below
                    // Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed
                    separatorIndex = args.Name.IndexOf("=", separatorIndex, StringComparison.Ordinal);
                    if (separatorIndex > 0)
                    {
                        var endIndex = args.Name.IndexOf(",", separatorIndex, StringComparison.Ordinal);
                        if (endIndex > 0)
                        {
                            var parts = args.Name
                                .Substring(separatorIndex + 1, endIndex - separatorIndex - 1)
                                .Split('.');

                            version = new Version(int.Parse(parts[0]),
                                int.Parse(parts[1]),
                                int.Parse(parts[2]),
                                int.Parse(parts[3]));
                        }
                    }
                }

                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName == args.Name);
                if (existingAssembly != null)
                {
                    return existingAssembly;
                }

                if (version != null)
                {
                    var assemblyByVersion = AssemblyLoadUtility.TryLoadByNameOrVersion(
                        assemblyPaths,
                        assemblyName,
                        version.Major + "." + version.Minor + "." + version.Build + ".");

                    if (assemblyByVersion != null)
                    {
                        return assemblyByVersion;
                    }

                    assemblyByVersion = AssemblyLoadUtility.TryLoadByNameOrVersion(
                        assemblyPaths,
                        assemblyName,
                        version.Major + "." + version.Minor + ".");

                    if (assemblyByVersion != null)
                    {
                        return assemblyByVersion;
                    }

                    assemblyByVersion =
                        AssemblyLoadUtility.TryLoadByNameOrVersion(assemblyPaths, assemblyName, version.Major + ".");

                    if (assemblyByVersion != null)
                    {
                        return assemblyByVersion;
                    }
                }

                var assembly = AssemblyLoadUtility.TryLoadByNameOrVersion(assemblyPaths, assemblyName);
                if (assembly != null)
                {
                    return assembly;
                }

                return null;
            };
#endif
        }

        /// <summary>
        /// Builds <see cref="InternalGenerationContext"/> by reflecting into contract assemblies.
        /// </summary>
        /// <param name="contractAssembliesPaths">The contract assemlby paths.</param>
        /// <param name="operationElements">The operation xelements.</param>
        /// <param name="propertyElements">The property xelements</param>
        /// <param name="documentVariantElementName">The document variant element name.</param>
        /// <param name="schemaGenerationSettings"><see cref="SchemaGenerationSettings"/></param>
        /// <returns>Serialized <see cref="InternalGenerationContext"/></returns>
        public string BuildInternalGenerationContext(
            IList<string> contractAssembliesPaths,
            IList<string> operationElements,
            IList<string> propertyElements,
            string documentVariantElementName,
            SchemaGenerationSettings schemaGenerationSettings)
        {
            var crefSchemaMap = new Dictionary<string, InternalSchemaGenerationInfo>();

            List<XElement> xPropertyElements = propertyElements.Select(XElement.Parse).ToList();

            var propertyMap = new Dictionary<string, string>();

            foreach (var xPropertyElement in xPropertyElements)
            {
                var name = xPropertyElement
                    .Attributes(KnownXmlStrings.Name)?.FirstOrDefault()?.Value?.Split(':')[1];
                var description = xPropertyElement.Element(KnownXmlStrings.Summary)?.Value.RemoveBlankLines();

                if (!propertyMap.ContainsKey(name))
                {
                    propertyMap.Add(name, description);
                }
            }

            var referenceRegistryMap = new Dictionary<DocumentVariantInfo, SchemaReferenceRegistry>();

            var internalGenerationContext = new InternalGenerationContext();

#if !NETFRAMEWORK
            var typeFetcher = new TypeFetcher(contractAssembliesPaths, Context);
#else
            var typeFetcher = new TypeFetcher(contractAssembliesPaths);           
#endif

            List<XElement> xOperationElements = operationElements.Select(XElement.Parse).ToList();

            foreach (var xOperationElement in xOperationElements)
            {
                if(!referenceRegistryMap.ContainsKey(DocumentVariantInfo.Default))
                {
                    referenceRegistryMap.Add(
                        DocumentVariantInfo.Default,
                        new SchemaReferenceRegistry(schemaGenerationSettings, propertyMap));
                }

                // Recursively build the various cref-schema, cref-fieldValue map.
                BuildMap(
                    xOperationElement,
                    crefSchemaMap,
                    internalGenerationContext.CrefToFieldValueMap,
                    typeFetcher,
                    referenceRegistryMap[DocumentVariantInfo.Default]);

                var customElements = xOperationElement.Descendants(documentVariantElementName);

                // Build the various cref-schema, cref-fieldValue map for each customElement.
                foreach (var customElement in customElements)
                {
                    var documentVariantInfo = new DocumentVariantInfo
                    {
                        Categorizer = customElement.Name.LocalName.Trim(),
                        Title = customElement.Value.Trim()
                    };

                    if (!referenceRegistryMap.ContainsKey(documentVariantInfo))
                    {
                        referenceRegistryMap.Add(
                            documentVariantInfo,
                            new SchemaReferenceRegistry(schemaGenerationSettings, propertyMap));
                    }

                    BuildMap(
                        xOperationElement,
                        crefSchemaMap,
                        internalGenerationContext.CrefToFieldValueMap,
                        typeFetcher,
                        referenceRegistryMap[documentVariantInfo]);
                }          
            }

            foreach(var key in referenceRegistryMap.Keys)
            {
                var references = referenceRegistryMap[key].References;

                internalGenerationContext.VariantSchemaReferenceMap.Add(
                    key.ToString(),
                    references.ToDictionary(k => k.Key, k => k.Value.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0)));
            }

            internalGenerationContext.CrefToSchemaMap = crefSchemaMap;

            // Serialize the context to transfer across app domain.
            var document = JsonConvert.SerializeObject(internalGenerationContext);

            return document;
        }

        /// <summary>
        /// Fetches all the cref's from the provided operation element and build cref --> schema, cref --> field value
        /// maps.
        /// </summary>
        /// <param name="currentXElement">The XElement to fetch cref and see tags for.</param>
        /// <param name="crefSchemaMap">The cref to <see cref="InternalSchemaGenerationInfo"/> map.</param>
        /// <param name="crefFieldMap">The cref to <see cref="FieldValueInfo"/> map.</param>
        /// <param name="typeFetcher">The type fetcher used to fetch type, field information.</param>
        /// <param name="schemaReferenceRegistry"><see cref="SchemaReferenceRegistry"/>.</param>
        private void BuildMap(
            XElement currentXElement,
            Dictionary<string, InternalSchemaGenerationInfo> crefSchemaMap,
            Dictionary<string, FieldValueInfo> crefFieldMap,
            TypeFetcher typeFetcher,
            SchemaReferenceRegistry schemaReferenceRegistry)
        {
            if (currentXElement == null)
            {
                return;
            }

            var cref = currentXElement.Attribute(KnownXmlStrings.Cref)?.Value.Trim();

            if (!string.IsNullOrWhiteSpace(cref))
            {
                var allListedTypes = new List<string>() { cref };

                if (allListedTypes.Where(i => i.StartsWith("F:")).Any())
                {
                    BuildCrefFieldValueMap(allListedTypes.FirstOrDefault(), crefFieldMap, typeFetcher);
                }
                else
                {
                    BuildCrefSchemaMap(allListedTypes, crefSchemaMap, typeFetcher, schemaReferenceRegistry);
                }
            }

            if (!currentXElement.Elements().Any())
            {
                return;
            }

            var seeNodes = currentXElement.Elements().Where(i => i.Name == KnownXmlStrings.See);

            if (seeNodes.Any())
            {
                var allListedTypes = seeNodes
                    .Select(node => node.Attribute(KnownXmlStrings.Cref)?.Value)
                    .Where(crefValue => crefValue != null).ToList();

                if (allListedTypes.Where(i => i.StartsWith("F:")).Any())
                {
                    BuildCrefFieldValueMap(allListedTypes.FirstOrDefault(), crefFieldMap, typeFetcher);
                }
                else
                {
                    BuildCrefSchemaMap(allListedTypes, crefSchemaMap, typeFetcher, schemaReferenceRegistry);
                }
            }

            var remainingNodes = currentXElement.Elements().Where(i => i.Name != KnownXmlStrings.See);

            foreach (var remainingNode in remainingNodes)
            {
                BuildMap(
                    remainingNode,
                    crefSchemaMap,
                    crefFieldMap,
                    typeFetcher,
                    schemaReferenceRegistry);
            }
        }

        /// <summary>
        /// Generates schema for the provided list of types and store them in the provided dictionary.
        /// </summary>
        /// <param name="allListedTypes">The listed types to fetch schema for.</param>
        /// <param name="crefSchemaMap">The cref to <see cref="InternalSchemaGenerationInfo"/> map.</param>
        /// <param name="typeFetcher">The type fetcher used to fetch type information using reflection.</param>
        /// <param name="schemaReferenceRegistry"><see cref="SchemaReferenceRegistry"/>.</param>
        private void BuildCrefSchemaMap(
            IList<string> allListedTypes,
            Dictionary<string, InternalSchemaGenerationInfo> crefSchemaMap,
            TypeFetcher typeFetcher,
            SchemaReferenceRegistry schemaReferenceRegistry)
        {
            var key = allListedTypes.ToCrefKey();
            
            var schemaInfo = new InternalSchemaGenerationInfo();
            try
            {
                var type = typeFetcher.LoadTypeFromCrefValues(allListedTypes);
                var schema = schemaReferenceRegistry.FindOrAddReference(type);
                schemaInfo.Schema = schema.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);             
            }
            catch (Exception e)
            {
                var error = new GenerationError
                {
                    ExceptionType = e.GetType().Name,
                    Message = e.Message
                };

                schemaInfo.Error = error;
            }

            if (!crefSchemaMap.ContainsKey(key))
            {
                crefSchemaMap.Add(key, schemaInfo);
            }
        }

        /// <summary>
        /// Provided a field cref value, fetches the field value using reflection.
        /// </summary>
        /// <param name="crefValue">The cref value for the field.</param>
        /// <param name="crefFieldMap">The cref value to <see cref="FieldValueInfo"/> map.</param>
        /// <param name="typeFetcher">The type fetcher used to fetch field value from the loaded assemblies.</param>
        private void BuildCrefFieldValueMap(
            string crefValue,
            Dictionary<string, FieldValueInfo> crefFieldMap,
            TypeFetcher typeFetcher)
        {
            if (string.IsNullOrWhiteSpace(crefValue) || crefFieldMap.ContainsKey(crefValue))
            {
                return;
            }

            var fieldValueInfo = new FieldValueInfo();

            try
            {
                var typeName = crefValue.ExtractTypeNameFromFieldCref();
                var type = typeFetcher.LoadTypeFromCrefValues(new List<string> { typeName });
                var fieldName = crefValue.ExtractFieldNameFromCref();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                var field = fields.FirstOrDefault(f => f.Name == fieldName);

                if (field == null)
                {
                    var errorMessage = string.Format(
                        SpecificationGenerationMessages.FieldNotFound,
                        fieldName,
                        typeName);

                    throw new TypeLoadException(errorMessage);
                }

                fieldValueInfo.Value = field.GetValue(null).ToString();
            }
            catch (Exception e)
            {
                var error = new GenerationError
                {
                    ExceptionType = e.GetType().Name,
                    Message = e.Message
                };

                fieldValueInfo.Error = error;
            }

            crefFieldMap.Add(crefValue, fieldValueInfo);
        }
    }
}