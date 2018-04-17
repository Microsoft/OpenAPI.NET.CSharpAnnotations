﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration
{
    /// <summary>
    /// Holds the specification generation messages.
    /// </summary>
    public static class SpecificationGenerationMessages
    {
        public const string AddingSchemaReferenceFailed =
            "Adding schema \"{0}\" to the schema reference registry failed with the error: {1}.";

        public const string CannotUniquelyIdentifyType = "Could not uniquely identify type: \"{0}\", " +
            "please use fully qualified namespace. The following types exist: {1}";

        public const string ConflictingDocumentVariantAttributes =
            "The document variant (Categorizer: {0}, Title: {1}) have conflicting attributes: " +
            "{2} | {3}";

        public const string ConflictingPathAndQueryParameters =
            "The parameter \"{0}\" is present in both path and query of the url \"{1}\".";

        public const string DuplicateOperation = "There are duplicates for the operation: \"{0}\" \"{1}\"";

        public const string FilterSetVersionNotSupported = "Provided filter set version: \"{0}\" is not supported.";

        public const string InvalidHttpMethod = "The documented verb \"{0}\" is not a valid http Method.";

        public const string InvalidUrl = "The documented url \"{0}\" is not a valid uri due to the error: {1}.";

        public const string NullUrl = "Url is null";

        public const string MalformattedUrl = "Url is malformatted and cannot be decoded properly.";

        public const string MissingInAttribute = "In attribute is missing from parameter(s): \"{0}\"";

        public const string MissingSeeCrefTag = 
            "Tag \"<see cref=\"(RequestBodyType)\"\\>\" is missing from body parameter: \"{0}\". ";

        public const string MoreThanOneVariantNameNotAllowed =
            "More than one document variant is not allowed. Variant used for categorization is: {0}";

        public const string NoOperationElementFoundToParse =
            "No valid operation elements containing tag \"<url>\" and \"<verb>\" have been found in the annotation xml.";

        public const string OperationMethodNotParsedGivenUrlIsInvalid =
            "Operation method has not been parsed given that the url is invalid.";

        public const string SuccessfulPathGeneration = "The specification for path has been generated successfully.";

        public const string TypeNotFound = "Type \"{0}\" could not be found. " +
            "Ensure that it exists in one of the following assemblies: {1}";

        public const string UndocumentedGenericType =
            "The request or response type contains a generic type that is undocumented. " +
            "The correct way to document a generic type is as follows: <responseType>" +
            "<see cref=\"Response{T}\"/> where T is <see cref=\"GenericType\"/></responseType>";

        public const string UndocumentedPathParameter = "Path paramater: {0} is undocumented in {1}";

        public const string UnableToGenerateAllOperations = "Generated {0}/{1} documented operations successfully.";

        public const string UnexpectedError = "Unexpected error occurred during generation: {0}";

        public const string UnorderedGenericType =
            "The request or response type contains a generic type that is unordered. " +
            "The correct way to document a generic type is as follows: <![CDATA[ <responseType>" +
            "<see cref=\"Response{T}\"/> where T is <see cref=\"GenericType\"/></responseType> ]]>";
    }
}