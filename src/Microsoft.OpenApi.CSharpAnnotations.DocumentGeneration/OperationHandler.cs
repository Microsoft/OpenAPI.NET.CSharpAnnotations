// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Extensions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    internal static class OperationHandler
    {
        /// <summary>
        /// Checks whether the optional operation id tag is present in the operation element.
        /// </summary>
        /// <param name="operationElement"></param>
        /// <returns>True if the operationId tag is in the operation element, otherwise false.</returns>
        public static bool HasOperationId(XElement operationElement)
        {
            return operationElement?.Elements().Where(p => p.Name == KnownXmlStrings.OperationId).Any() ?? false;
        }

        /// <summary>
        /// Extracts the operation id from the operation element.
        /// </summary>
        /// <param name="operationElement"></param>
        /// <returns>Operation id.</returns>
        /// <exception cref="InvalidOperationIdException">Thrown if the operationId tag is missing or
        /// there are more than one tags.</exception>
        public static string GetOperationId(XElement operationElement)
        {
            var operationIdList = operationElement?.Elements().Where(p => p.Name == KnownXmlStrings.OperationId).ToList();
            if (operationIdList?.Count == 1)
            {
                return operationIdList[0].Value;
            }

            string error = operationIdList.Count > 1
                ? SpecificationGenerationMessages.MultipleOperationId
                : SpecificationGenerationMessages.NoOperationId;
            throw new InvalidOperationIdException(error);
        }

        /// <summary>
        /// Generates the operation id by parsing segments out of the absolute path.
        /// </summary>
        public static string GenerateOperationId(string absolutePath, OperationType operationMethod)
        {
            var operationId = new StringBuilder(operationMethod.ToString().ToLowerInvariant());

            foreach (var segment in absolutePath.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                var current = string.Empty;

                // In order to build an operation id, extract the path parameters
                // and prepend By to these before adding them to the operationId.
                // e.g. for GET /v6/products/{productId} -> getV6ProductsByProductId 
                var matches = new Regex(@"\{(.*?)\}").Matches(segment);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        current += "By" + match.Groups[1].Value.ToTitleCase();
                    }
                }
                else
                {
                    current = segment.ToTitleCase();
                }

                // OpenAPI spec recommends following "programming naming conventions" 
                // for operationId to allow tools to utilize this property.
                // To be safe, we only allow alphanumeric characters.
                operationId.Append(Regex.Replace(current, "[^a-zA-Z0-9]", string.Empty));
            }

            return operationId.ToString();
        }

        /// <summary>
        /// Extracts the operation method from the operation element
        /// </summary>
        /// <param name="operationElement">The xml element representing an operation in the annotation xml.</param>
        /// <exception cref="InvalidVerbException">Thrown if the verb is missing or has invalid format.</exception>
        public static OperationType GetOperationMethod(XElement operationElement)
        {
            var verbElement = operationElement.Descendants().FirstOrDefault(i => i.Name == KnownXmlStrings.Verb);

            var verb = verbElement?.Value.Trim();

            OperationType operationMethod;

            if (Enum.TryParse(verb, true, out operationMethod))
            {
                return operationMethod;
            }

            throw new InvalidVerbException(verb);
        }

        /// <summary>
        /// Extracts the URL from the operation element
        /// </summary>
        /// <param name="operationElement">The xml element representing an operation in the annotation xml.</param>
        /// <exception cref="InvalidUrlException">Thrown if the URL is missing or has invalid format.</exception>
        public static string GetUrl(
            XElement operationElement)
        {
            var urls = operationElement.Elements(KnownXmlStrings.Url).Select(i => i.Value);

            // Can't process further if no url is documented, so skip the operation.
            var url = urls.FirstOrDefault();

            if (url == null)
            {
                throw new InvalidUrlException(
                    url,
                    SpecificationGenerationMessages.NullUrl);
            }

            try
            {
                url = WebUtility.UrlDecode(new Uri(WebUtility.UrlDecode(url)).AbsolutePath);
            }
            catch (UriFormatException)
            {
                throw new InvalidUrlException(
                    url,
                    SpecificationGenerationMessages.MalformattedUrl);
            }
            catch (Exception e)
            {
                throw new InvalidUrlException(
                    url,
                    e.Message);
            }

            return url;
        }
    }
}