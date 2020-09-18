// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters
{
    /// <summary>
    /// Filter to initialize OpenApi operation based on the annotation XML element.
    /// It does not do the creation itself but forwards the call to the real generator filters.
    /// The first filter which is applicable is executed in order to generate the operation.
    /// </summary>
    public class CreateOperationMetaFilter : IPreProcessingOperationFilter
    {
        private List<ICreateOperationPreProcessingOperationFilter> createOperationFilters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOperationMetaFilter"/>.
        /// </summary>
        /// <remarks>
        /// Using this constructor, the following filter list is used:
        /// If the XML element contains 'operationId' tag, it is used as unique identifier
        /// of the operation. Otherwise, the id is generated using the path of the operation.
        /// In the latter case, multiple operation could be generated, if the path has optional
        /// parameters.
        /// </remarks>
        public CreateOperationMetaFilter() :
            this(new List<ICreateOperationPreProcessingOperationFilter> {
                new UsePredefinedOperationIdFilter(), new BranchOptionalPathParametersFilter()})
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOperationMetaFilter"/>.
        /// </summary>
        /// <param name="createOperationFilters">List of generator filters.</param>
        internal CreateOperationMetaFilter(
            List<ICreateOperationPreProcessingOperationFilter> createOperationFilters)
        {
            this.createOperationFilters = createOperationFilters;
        }

        /// <summary>
        /// Initializes the OpenApi operation based on the annotation XML element.
        /// It applies the first applicable filter to create operations.
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
            foreach (var filter in this.createOperationFilters)
            {
                if (filter.IsApplicable(element))
                {
                    return filter.Apply(paths, element, settings);
                }
            }

            // If none of the filters could be applied --> error
            return new List<GenerationError>
            {
                new GenerationError
                {
                    Message = "Failed to apply any operation creation filter.",
                    ExceptionType = nameof(InvalidOperationException)
                }
            };
        }
    }
}