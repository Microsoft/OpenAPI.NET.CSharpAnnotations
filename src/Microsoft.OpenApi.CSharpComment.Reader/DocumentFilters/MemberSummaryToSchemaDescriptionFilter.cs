﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.OpenApi.CSharpComment.Reader.Extensions;
using Microsoft.OpenApi.CSharpComment.Reader.Models.KnownStrings;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Microsoft.OpenApi.CSharpComment.Reader.DocumentFilters
{
    /// <summary>
    /// Filter to parse the values from the summary tags for all the properties
    /// to populate descriptions in the schema.
    /// </summary>
    public class MemberSummaryToSchemaDescriptionFilter : IDocumentFilter
    {
        /// <summary>
        /// Parses the values from the summary for all the properties to populate descriptions in the schema.
        /// </summary>
        /// <param name="specificationDocument">The Open Api V3 specification document to be updated.</param>
        /// <param name="xmlDocument">The document representing annotation xml.</param>
        /// <param name="settings">Settings for document filters.</param>
        public void Apply(OpenApiDocument specificationDocument, XDocument xmlDocument, DocumentFilterSettings settings)
        {
            var propertyMembers = xmlDocument.XPathSelectElements("//doc/members/member")
                .Where(
                    m => m.Attribute(KnownXmlStrings.Name) != null &&
                        m.Attribute(KnownXmlStrings.Name).Value.StartsWith("P:"))
                .ToList();

            foreach (var propertyMember in propertyMembers)
            {
                var fullPropertyName = propertyMember.Attribute(KnownXmlStrings.Name).Value;

                var splitPropertyName = fullPropertyName.Split('.');

                // Take everything before the last period and remove the "P:" prefix.
                var className =
                    string.Join(".", splitPropertyName.Take(splitPropertyName.Length - 1))
                        .Substring(startIndex: 2);

                // We need to sanitize class name to match the format in the schema reference registry.
                // Note that this class may also match several classes in the registry given that generics
                // with different types are treated as different schemas.
                // For example, summary information for properties in class name A 
                // should apply to those properties in schema A, A_B_, and A_B_C__ as well.
                var sanitizedClassName = className.SanitizeClassName();

                var schemas = specificationDocument.Components.Schemas.Where(
                        s => s.Key == sanitizedClassName ||
                            s.Key.StartsWith(sanitizedClassName + "_"))
                    .ToList();

                if (!schemas.Any())
                {
                    continue;
                }

                var propertyName =
                    splitPropertyName[splitPropertyName.Length - 1];

                var propertyInfo = settings.TypeFetcher.LoadType(className)
                    ?.GetProperties()
                    .FirstOrDefault(p => p.Name == propertyName);

                if (propertyInfo != null)
                {
                    var jsonPropertyAttributes =
                        (JsonPropertyAttribute[])propertyInfo.GetCustomAttributes(
                            typeof(JsonPropertyAttribute),
                            inherit: false);
                    if (jsonPropertyAttributes.Any())
                    {
                        // Extract the property name in JsonProperty if given.
                        if (jsonPropertyAttributes[0].PropertyName != null)
                        {
                            propertyName = jsonPropertyAttributes[0].PropertyName;
                        }
                    }
                }

                foreach (var schema in schemas)
                {
                    if (schema.Value.Properties.ContainsKey(propertyName))
                    {
                        schema.Value.Properties[propertyName].Description =
                            propertyMember.Element(KnownXmlStrings.Summary)?.Value.RemoveBlankLines();
                    }
                }
            }
        }
    }
}