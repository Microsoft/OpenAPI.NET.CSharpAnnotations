// ------------------------------------------------------------
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

        public const string DuplicateOperation = "There are duplicates for the operation: \"{0}\" \"{1}\". " +
                                                 "Only the first occurrence of it added to the generated document, " +
                                                 "rest all are omitted from the generated document.";

        public const string DuplicateOperationWithVariantInfo =
            "There are duplicates for the operation: \"{0}\" \"{1}\" for variant: \"{2}\". " +
            "Only the first occurrence of it added to the generated document, " +
            "rest all are omitted from the generated document.";

        public const string DuplicateProperty = "A property with the name \"{0}\" already exists on \"{1}\".";

        public const string FieldNotFound = "Field \"{0}\" could not be found for type: \"{1}\".";

        public const string FilterSetVersionNotSupported = "Provided filter set version: \"{0}\" is not supported.";

        public const string InvalidHttpMethod = "The documented verb \"{0}\" is not a valid http Method.";

        public const string InvalidUrl = "The documented url \"{0}\" is not a valid uri due to the error: {1}.";

        public const string NullUrl = "Url is null";

        public const string MalformattedUrl = "Url is malformatted and cannot be decoded properly.";

        public const string MissingInAttribute = "In attribute is missing from parameter(s): \"{0}\"";

        public const string UndocumentedName = "\"name\" attribute is not documented for tag: \"<{0}>\".";

        public const string MissingSeeCrefTag =
            "Tag \"<see cref=\"(RequestBodyType)\"\\>\" is missing from body parameter: \"{0}\".";

        public const string UndocumentedScopeForFlow =
            "Scope is not documented for flow type: \"{0}\". Document it using Tag \"<scope>\".";

        public const string UndocumentedScopeForSecurity =
            "Scope is not documented for security type: \"{0}\". Document it using Tag \"<scope>\".";

        public const string UndocumentedAuthorizationUrl =
            "Authorization Url is not documented for flow type: \"{0}\". Document it using Tag \"<authorizationUrl>\".";

        public const string UndocumentedOpenIdConnectUrl =
            "Open Id connectuUrl is not documented for security type: \"{0}\". Document it using Tag \"<openIdConnectUrl>\".";

        public const string UndocumentedTokenUrl =
            "Token Url is not documented for flow type: \"{0}\". Document it using Tag \"<tokenUrl>\".";

        public const string UndocumentedDescription =
            "Description is not documented for tag: \"<{0}>\". Document it using Tag \"<description>\".";

        public const string UndocumentedType = "Attribute \"type\" is not documented for tag: \"<{0}>\".";

        public const string UndocumentedScheme =
            "Scheme is not documented for security type: \"{0}\". Document it using Tag \"<scheme>\".";

        public const string UndocumentedIn =
            "In is not documented under tag \"<{0}>\". Document it using Tag \"<in>\". Allowed values: \"query\", \"header\", \"cookie\".";

        public const string UndocumentedFlow =
            "Flow is not documented for security type: \"{0}\". Document it using Tag \"<flow>\".";

        public const string MoreThanOneVariantNameNotAllowed =
            "More than one document variant is not allowed. Variant used for categorization is: {0}";

        public const string NoOperationElementFoundToParse =
            "No valid operation elements containing tag \"<url>\" and \"<verb>\" have been found in the annotation xml.";

        public const string NotSupportedInAttributeValue =
            "Parameter(s): \"{0}\" contain unsupported \"in\" attribute value(s): \"{1}\". Supported values are: {2}.";

        public const string OperationMethodNotParsedGivenUrlIsInvalid =
            "Operation method has not been parsed given that the url is invalid.";

        public const string ProvideEitherValueOrUrlTag =
            "Tag \"example\" cannot contain both \"value\" and \"url\" tags. Please provide either one of them.";

        public const string ProvideValueForExample =
            "Tag \"value\" for example is empty. Please populate it with example value.";

        public const string SuccessfulPathGeneration = "The specification for path has been generated successfully.";

        public const string TypeNotFound = "Type \"{0}\" could not be found. " +
                                           "Ensure that it exists in one of the following assemblies: {1}";

        public const string UndocumentedGenericType =
            "The request or response type contains a generic type that is undocumented. " +
            "The correct way to document a generic type is as follows: " +
            "<see cref=\"Response{T}\"/> where T is <see cref=\"GenericType\"/>";

        public const string UndocumentedPathParameter = "Path paramater: {0} is undocumented in {1}";

        public const string UnableToGenerateAllOperations = "Generated {0}/{1} documented operations successfully.";

        public const string UnexpectedError = "Unexpected error occurred during generation: {0}";

        public const string UnorderedGenericType =
            "The request or response type contains a generic type that is unordered. " +
            "The correct way to document a generic type is as follows: " +
            "<see cref=\"Response{T}\"/> where T is <see cref=\"GenericType\"/>";

        public const string NotSupportedTypeAttributeValue =
            "Documented type: \"{0}\" is not supported for \"type\" attribute value in tag: \"{1}\". Supported values are: {2}.";

        public const string NoOperationId = "No operationId element is present in the operation.";

        public const string MultipleOperationId = "More than one operationId element is not accepted in the operation.";
    }
}