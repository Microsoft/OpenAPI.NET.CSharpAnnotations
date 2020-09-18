// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System.Collections;
using System.Collections.Generic;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.DocumentConfigFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.DocumentFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationConfigFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OperationFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PostProcessingDocumentFilters;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// The filter set that will be used while generating OpenAPI specification document.
    /// </summary>
    public sealed class FilterSet : IEnumerable<IFilter>
    {
        private static FilterSet _defaultFilterSet;
        private readonly IList<IFilter> _filters = new List<IFilter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSet"/> class.
        /// </summary>
        public FilterSet()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSet"/> class.
        /// </summary>
        /// <param name="filterSet">Filter set to be copied from.</param>
        public FilterSet(FilterSet filterSet)
        {
            if (filterSet == null)
            {
                return;
            }

            // We create a new instance of FilterSet per call as a safeguard
            // against unintentional modification of the private _defaultFilterSet.
            foreach (var filter in filterSet)
            {
                Add(filter);
            }
        }

        /// <summary>
        /// Get the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<IFilter> GetEnumerator()
        {
            foreach (var filter in _filters)
            {
                yield return filter;
            }
        }

        /// <summary>
        /// Get the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Add the new filter into the filter set.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public void Add(IFilter filter)
        {
            _filters.Add(filter);
        }

        /// <summary>
        /// Gets the default filter sets.
        /// </summary>
        public static FilterSet GetDefaultFilterSet(FilterSetVersion version)
        {
            if (_defaultFilterSet == null)
            {
                _defaultFilterSet = new FilterSet();

                switch (version)
                {
                    case FilterSetVersion.V1:
                        //Document config filters
                        _defaultFilterSet.Add(new DocumentVariantAttributesFilter());

                        //Document filters
                        _defaultFilterSet.Add(new AssemblyNameToInfoFilter());
                        _defaultFilterSet.Add(new UrlToServerFilter());
                        _defaultFilterSet.Add(new SecurityToSecurityRequirementDocumentFilter());

                        //Operation config filters
                        _defaultFilterSet.Add(new CommonAnnotationFilter());

                        //Operation filters
                        _defaultFilterSet.Add(new GroupToTagFilter());
                        _defaultFilterSet.Add(new ParamToParameterFilter());
                        _defaultFilterSet.Add(new ParamToRequestBodyFilter());
                        _defaultFilterSet.Add(new RemarksToDescriptionFilter());
                        _defaultFilterSet.Add(new ResponseToResponseFilter());
                        _defaultFilterSet.Add(new SummaryToSummaryFilter());
                        _defaultFilterSet.Add(new SecurityToSecurityRequirementOperationFilter());

                        //Pre processing operation filters
                        _defaultFilterSet.Add(new PopulateInAttributeFilter());
                        _defaultFilterSet.Add(new ConvertAlternativeParamTagsFilter());
                        _defaultFilterSet.Add(new ValidateInAttributeFilter());
                        _defaultFilterSet.Add(new CreateOperationMetaFilter());

                        //Post processing document filters
                        _defaultFilterSet.Add(new RemoveFailedGenerationOperationFilter());

                        return _defaultFilterSet;

                    default:
                        throw new FilterSetVersionNotSupportedException(version.ToString());
                }
            }

            return new FilterSet(_defaultFilterSet);
        }
    }
}