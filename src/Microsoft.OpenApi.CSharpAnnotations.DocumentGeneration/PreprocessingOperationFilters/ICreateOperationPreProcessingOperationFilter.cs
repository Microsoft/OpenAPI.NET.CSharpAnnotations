// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationFilters;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters
{
    /// <summary>
    /// The class representing the contract of a filter to preprocess the <see cref="OpenApiOperation"/>
    /// objects in <see cref="OpenApiPaths"/> before each <see cref="OpenApiOperation"/> is processed by the
    /// <see cref="IOperationFilter"/>.
    /// </summary>
    public interface ICreateOperationPreProcessingOperationFilter : IPreProcessingOperationFilter
    {
        /// <summary>
        /// Verifies that the annotation XML element contains all data which are required to apply this filter.
        /// </summary>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <returns>True if the filter can be applied, otherwise false.</returns>
        bool IsApplicable(XElement element);
    }
}