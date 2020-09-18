// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.KnownStrings
{
    /// <summary>
    /// Class to store known xml tags that will be processed by the generation engine.
    /// </summary>
    public class KnownXmlStrings
    {
        public const string Annotation = "annotation";
        public const string Body = "body";
        public const string Code = "code";
        public const string Cref = "cref";
        public const string Default = "default";
        public const string Description = "description";
        public const string Example = "example";
        public const string Group = "group";
        public const string Header = "header";
        public const string In = "in";
        public const string Name = "name";
        public const string Option = "option";
        public const string Options = "options";
        public const string Para = "para";
        public const string Param = "param";
        public const string Paramref = "paramref";
        public const string Path = "path";
        public const string PathParam = "pathParam";
        public const string Query = "query";
        public const string QueryParam = "queryParam";
        public const string Remarks = "remarks";
        public const string RequestType = "requestType";
        public const string Required = "required";
        public const string Response = "response";
        public const string ResponseType = "responseType";
        public const string See = "see";
        public const string Seealso = "seealso";
        public const string Summary = "summary";
        public const string T = "T";
        public const string Typeparamref = "Typeparamref";
        public const string Tag = "tag";
        public const string Type = "type";
        public const string Url = "url";
        public const string Value = "value";
        public const string Variant = "variant";
        public const string Verb = "verb";
        public const string ApiKey = "apiKey";
        public const string Http = "http";
        public const string OAuth2 = "oauth2";
        public const string OpenIdConnect = "openIdConnect";
        public const string Security = "security";
        public const string Cookie = "cookie";
        public const string Scheme = "scheme";
        public const string BearerFormat = "bearerFormat";
        public const string OpenIdConnectUrl = "openIdConnectUrl";
        public const string TokenUrl = "tokenUrl";
        public const string RefreshUrl = "refreshUrl";
        public const string AuthorizationUrl = "authorizationUrl";
        public const string Flow = "flow";
        public const string ImplicitFlow = "implicit";
        public const string Password = "password";
        public const string AuthorizationCode = "authorizationCode";
        public const string ClientCredentials = "clientCredentials";
        public const string Scope = "scope";
        public const string OperationId = "operationId";
        public static string[] AllowedAppKeyInValues => new[] {Header, Query, Cookie};

        public static string[] AllowedFlowTypeValues => new[]
            {ImplicitFlow, Password, AuthorizationCode, ClientCredentials};

        public static string[] AllowedInValues => new[] {Header, Path, Query, Body};

        public static string[] AllowedSecurityTypeValues => new[]
            {ApiKey, Http, OpenIdConnect, OAuth2};

        // "body" in attribute is translated to a requestBody instead of a parameter.
        public static string[] InValuesTranslatableToParameter => new[] {Header, Path, Query};
    }
}