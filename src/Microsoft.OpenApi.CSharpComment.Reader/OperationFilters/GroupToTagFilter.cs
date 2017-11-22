// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpComment.Reader.Models.KnownStrings;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpComment.Reader.OperationFilters
{
    /// <summary>
    /// Parses the value of group tag in xml documentation and apply that as tag in operation.
    /// </summary>
    public class GroupToTagFilter : IOperationFilter
    {
        /// <summary>
        /// Fetches the value of "group" tag from xml documentation and populates operation's tag.
        /// </summary>
        /// <param name="operation">The operation to be updated.</param>
        /// <param name="element">The xml element representing an operation in the annotation xml.</param>
        /// <param name="settings">The operation filter settings.</param>
        /// <remarks>
        /// Care should be taken to not overwrite the existing value in Operation if already present.
        /// This guarantees the predictable behavior that the first tag in the XML will be respected.
        /// It also guarantees that common annotations in the config file do not overwrite the
        /// annotations in the main documentation.
        /// </remarks>
        public void Apply(OpenApiOperation operation, XElement element, OperationFilterSettings settings)
        {
            var groupElement = element.Descendants().FirstOrDefault(i => i.Name == KnownXmlStrings.Group);

            var groupValue = groupElement?.Value.Trim();

            if (string.IsNullOrWhiteSpace(groupValue))
            {
                return;
            }

            if (!operation.Tags.Select(t => t.Name).Contains(groupValue))
            {
                operation.Tags.Add(
                    new OpenApiTag
                    {
                        Name = groupValue
                    }
                );
            }
        }
    }
}