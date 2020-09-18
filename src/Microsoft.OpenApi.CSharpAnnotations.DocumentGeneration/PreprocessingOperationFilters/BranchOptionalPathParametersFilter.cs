// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters
{
    /// <summary>
    /// Parses the value of the URL and creates multiple operations in the Paths object when
    /// there are optional path parameters.
    /// </summary>
    public class BranchOptionalPathParametersFilter : ICreateOperationPreProcessingOperationFilter
    {
        /// <summary>
        /// Verifies that the annotation XML element contains all data which are required to apply this filter.
        /// </summary>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <returns>Always true (this filter can be always applied).</returns>
        public bool IsApplicable(XElement element)
        {
            return true;
        }

        /// <summary>
        /// Fetches the URL value and creates multiple operations based on optional parameters.
        /// </summary>
        /// <param name="paths">The paths to be updated.</param>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <param name="settings">The operation filter settings.</param>
        /// <returns>The list of generation errors, if any produced when processing the filter.</returns>
        public IList<GenerationError> Apply(
            OpenApiPaths paths,
            XElement element,
            PreProcessingOperationFilterSettings settings)
        {
            var generationErrors = new List<GenerationError>();

            try
            {
                var paramPathElements = element.Elements()
                                .Where(
                                    p => p.Name == KnownXmlStrings.Param)
                                .Where(
                                    p => p.Attribute(KnownXmlStrings.In)?.Value == KnownXmlStrings.Path)
                                .ToList();

                var absolutePath = OperationHandler.GetUrl(element);

                var allGeneratedPathStrings = GeneratePossiblePaths(absolutePath, paramPathElements);

                var operationMethod = OperationHandler.GetOperationMethod(element);

                foreach (var pathString in allGeneratedPathStrings)
                {
                    if (!paths.ContainsKey(pathString))
                    {
                        paths[pathString] = new OpenApiPathItem();
                    }

                    paths[pathString].Operations[operationMethod] =
                        new OpenApiOperation
                        {
                            OperationId = OperationHandler.GenerateOperationId(pathString, operationMethod)
                        };
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

        /// <summary>
        /// Generates possible paths from a given path that may contain optional parameters.
        /// </summary>
        /// <param name="fullPath">The full path containing all optional and required parameters.</param>
        /// <param name="pathParams">The path parameters, used to determine whether a path segment is optional.</param>
        /// <returns>A list of possible paths.</returns>
        public static List<string> GeneratePossiblePaths(string fullPath, IList<XElement> pathParams)
        {
            var savedGeneratedPaths = new List<List<int>>
            {
                new List<int>()
            };

            var paths = new List<string>();
            var fullpathSegments = fullPath.Trim().Split('/');

            // Track indices of segments in fullPathSegments that make up a possible path instead of 
            // the string path itself to make equality comparisons between segments straightforward
            // E.g. fullpathSegments = ['v0', 'locales', '{tenant}', '{business}', '{app}', 'regions']
            // then a possible generated path could be [0, 1, 2, 5]
            // and the reconstructed path would be 'v0/locales/{tenant}/regions'.
            for (var i = 0; i < fullpathSegments.Length; i++)
            {
                var generatedPaths = new List<List<int>>();

                foreach (var path in savedGeneratedPaths)
                {
                    // We want to add this segment to the current path if the path segment is not optional or
                    // if the last segment added to the path is also the last segment we saw.
                    if (!IsPathSegmentOptional(fullpathSegments[i], pathParams, fullPath) ||
                        path.LastOrDefault() == i - 1)
                    {
                        var newPath = new List<int>(path);
                        newPath.Add(i);

                        generatedPaths.Add(newPath);
                    }

                    // If the path segment is optional, then we need to include paths without it.
                    if (IsPathSegmentOptional(fullpathSegments[i], pathParams, fullPath))
                    {
                        generatedPaths.Add(new List<int>(path));
                    }
                }

                savedGeneratedPaths = generatedPaths;
            }

            // Reconstruct the string path from each list of indices.
            foreach (var pathSegmentIndices in savedGeneratedPaths)
            {
                var path = string.Empty;

                foreach (var pathSegmentIndex in pathSegmentIndices)
                {
                    path += '/' + fullpathSegments[pathSegmentIndex];
                }

                // Remove the extra '/' in the front.
                paths.Add(path.Substring(1));
            }

            return paths;
        }

        /// <summary>
        /// Returns true if the segment of the path is a path parameter and is marked as optional
        /// </summary>
        /// <param name="pathSegment">
        /// A segment of the path. For example, in the path
        /// /v0/locales/{tenant}/{business}/{app}/regions,
        /// v0, locales, {tenant}, {business}, {app}, and regions are all path segments.
        /// </param>
        /// <param name="pathParams">List of documented path parameters</param>
        /// <param name="path">Full operation path</param>
        /// <returns>Boolean representation of whether or not path segment is optional</returns>
        private static bool IsPathSegmentOptional(string pathSegment, IList<XElement> pathParams, string path)
        {
            // Regex remove brackets from {pathParamNames} in segment
            // Examples:
            //  {pathSegment} -> [pathSegment]
            //  productId:{productId}-skuId:{skuId} -> [productId, skuId]
            var matches = new Regex(@"\{(.*?)\}").Matches(pathSegment);

            // If no path params are found, the path segment is required
            if (matches.Count == 0)
            {
                return false;
            }

            foreach (Match match in matches)
            {
                var pathParamName = match.Groups[1].Value;

                // Find the path param
                var pathParam = pathParams.FirstOrDefault(
                    p => string.Equals(
                        p.Attribute(KnownXmlStrings.Name)?.Value.Trim(),
                        pathParamName,
                        StringComparison.OrdinalIgnoreCase));

                // All path params must be documented, so this is a mistake in the documentation.
                // We will simply bail here and let the exception be thrown when
                // PopulateInAttributeFilter is processed.
                if (pathParam == null)
                {
                    return false;
                }

                // If required attribute is not included, the segment defaults to required.
                if (pathParam.Attribute(KnownXmlStrings.Required) == null)
                {
                    return false;
                }

                bool required;
                var parseSuccess = bool.TryParse(
                    pathParam.Attribute(KnownXmlStrings.Required)?.Value.Trim(), 
                    out required);

                // If any path parameter in the segment is marked as required, the entire segment is required.
                if (!parseSuccess || required)
                {
                    return false;
                }
            }

            return true;
        }
    }
}