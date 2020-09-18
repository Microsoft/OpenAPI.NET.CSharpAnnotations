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
    /// Filter to creates operation using the 'operationId' tag of the operation.
    /// It does not consider optional parameters, it create one operation for the full path.
    /// </summary>
    public class UsePredefinedOperationIdFilter : ICreateOperationPreProcessingOperationFilter
    {
        private readonly Func<XElement, string> GetUrlFunc;
        private readonly Func<XElement, OperationType> GetOperationMethodFunc;
        private readonly Func<XElement, string> GetOperationIdFunc;
        private readonly Predicate<XElement> HasOperationIdFunc;

        /// <summary>
        /// Initializes a new production instance of the <see cref="UsePredefinedOperationIdFilter"/>.
        /// </summary>
        public UsePredefinedOperationIdFilter()
            : this(
                  OperationHandler.GetUrl,
                  OperationHandler.GetOperationMethod,
                  OperationHandler.GetOperationId,
                  OperationHandler.HasOperationId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsePredefinedOperationIdFilter"/>.
        /// </summary>
        /// <param name="GetUrlFunc">Get url function.</param>
        /// <param name="GetOperationMethodFunc">Get operation method function.</param>
        /// <param name="GetOperationIdFunc">Get operation id function.</param>
        /// <param name="HasOperationIdFunc">Has operation id function.</param>
        internal UsePredefinedOperationIdFilter(
            Func<XElement, string> GetUrlFunc,
            Func<XElement, OperationType> GetOperationMethodFunc,
            Func<XElement, string> GetOperationIdFunc,
            Predicate<XElement> HasOperationIdFunc)
        {
            this.GetUrlFunc = GetUrlFunc;
            this.GetOperationMethodFunc = GetOperationMethodFunc;
            this.GetOperationIdFunc = GetOperationIdFunc;
            this.HasOperationIdFunc = HasOperationIdFunc;
        }

        /// <summary>
        /// Verifies whether the annotation XML element contains operationId XML tag.
        /// </summary>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <returns></returns>
        public bool IsApplicable(XElement element)
        {
            return this.HasOperationIdFunc(element);
        }

        /// <summary>
        /// Creates the operation using the operationId tag of the operation. It does not consider optional
        /// parameters, it creates one operation for the full path.
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
                var absolutePath = this.GetUrlFunc(element);
                var operationMethod = this.GetOperationMethodFunc(element);
                string operationId = this.GetOperationIdFunc(element);

                if (!paths.ContainsKey(absolutePath))
                {
                    paths[absolutePath] = new OpenApiPathItem();
                }

                paths[absolutePath].Operations[operationMethod] =
                    new OpenApiOperation
                    {
                        OperationId = operationId
                    };
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
    }
}