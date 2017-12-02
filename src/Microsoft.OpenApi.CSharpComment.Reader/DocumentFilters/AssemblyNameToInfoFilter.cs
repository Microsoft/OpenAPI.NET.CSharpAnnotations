﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpComment.Reader.DocumentFilters
{
    /// <summary>
    /// Parses the value of assembly tag in xml documentation and apply that as info in Open Api V3 specification document.
    /// </summary>
    public class AssemblyNameToInfoFilter : IDocumentFilter
    {
        /// <summary>
        /// Fetches the value of "assembly" tag from xml documentation and use it to populate
        /// Info object of Open Api V3 specification document.
        /// </summary>
        /// <param name="specificationDocument">The Open Api V3 specification document to be updated.</param>
        /// <param name="xmlDocument">The document representing annotation xml.</param>
        /// <param name="settings">Settings for document filters.</param>
        public void Apply(OpenApiDocument specificationDocument, XDocument xmlDocument, DocumentFilterSettings settings)
        {
            specificationDocument.Info = new OpenApiInfo
            {
                Title = xmlDocument.XPathSelectElement("//doc/assembly/name")?.Value,

                // Assign version as 1.0.0 for the time being.
                // TODO: Customer should be able to input this in the Config file
                Version = "1.0.0"
            };
        }
    }
}