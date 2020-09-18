// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.OpenApiDocumentGeneratorTests
{
    [Collection("DefaultSettings")]
    public class OpenApiDocumentGeneratorTest
    {
        private const string InputDirectory = "OpenApiDocumentGeneratorTests/Input";
        private const string OutputDirectory = "OpenApiDocumentGeneratorTests/Output";

        private readonly ITestOutputHelper _output;

        public OpenApiDocumentGeneratorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForCustomGenerationSettingsShouldReturnCorrectDocument))]
        public void CustomGenerationSettingsShouldReturnCorrectDocument(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            string openApiDocumentVersion,
            OpenApiDocumentGenerationSettings generationSettings,
            int expectedOperationGenerationResultsCount,
            string expectedJsonFile)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(
                documents,
                inputBinaryFiles,
                openApiDocumentVersion,
                new OpenApiGeneratorFilterConfig(FilterSetVersion.V1));

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();
            var openApiDocuments = generator.GenerateDocuments(input, out result, generationSettings);

            result.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Errors.Count.Should().Be(0);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();

            result.OperationGenerationDiagnostics.Count(p => p.Errors.Count > 0).Should().Be(0);
            result.OperationGenerationDiagnostics.Count.Should().Be(expectedOperationGenerationResultsCount);

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }

        public static IEnumerable<object[]> GetTestCasesForCustomGenerationSettingsShouldReturnCorrectDocument()
        {
            // Standard, original valid XML document with camel case setting
            yield return new object[]
            {
                "Standard, original valid XML document with camel case setting",
                new List<string>
                {
                    Path.Combine(InputDirectory, "Annotation.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Newtonsoft.Json.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                new OpenApiDocumentGenerationSettings(
                    new SchemaGenerationSettings(new CamelCasePropertyNameResolver())),
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationCamelCase.Json")
            };

            // Standard, original valid XML document with remove duplicate from param name setting
            yield return new object[]
            {
                "Standard, original valid XML document with remove duplicate from param name setting",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationWithDuplicateStringInParamName.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                new OpenApiDocumentGenerationSettings(true),
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithDuplicateStringInParamName.Json")
            };
        }

        public static IEnumerable<object[]> GetTestCasesForInvalidDocumentationShouldRemoveFailedOperations()
        {
            // Conflicting Path and Query Parameters
            yield return new object[]
            {
                "Conflicting Path and Query Parameters",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationConflictingPathAndQueryParameters.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationConflictingPathAndQueryParameters.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Get.ToString(),
                        Path = "/V1/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(ConflictingPathAndQueryParametersException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.ConflictingPathAndQueryParameters,
                                    "id",
                                    "http://localhost:9000/V1/samples/{id}?queryBool={queryBool}&id={id}")
                            },
                            new GenerationError
                            {
                                ExceptionType = typeof(UndocumentedPathParameterException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.UndocumentedPathParameter,
                                    "id",
                                    "http://localhost:9000/V1/samples/{id}?queryBool={queryBool}&id={id}")
                            }
                        }
                    }
                }
            };

            // Path parameter in the URL is not documented in any param elements.
            yield return new object[]
            {
                "Path Parameter Undocumented",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationUndocumentedPathParam.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationUndocumentedPathParam.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Get.ToString(),
                        Path = "/V1/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(UndocumentedPathParameterException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.UndocumentedPathParameter,
                                    "id",
                                    "http://localhost:9000/V1/samples/{id}?queryBool={queryBool}")
                            }
                        }
                    }
                }
            };

            // Undocumented Generics
            yield return new object[]
            {
                "Undocumented Generics",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationUndocumentedGeneric.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationUndocumentedGeneric.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Get.ToString(),
                        Path = "/V3/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(UndocumentedGenericTypeException).Name,
                                Message = SpecificationGenerationMessages.UndocumentedGenericType
                            }
                        }
                    }
                }
            };

            // Incorrect Order for Generics
            yield return new object[]
            {
                "Incorrect Order for Generics",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationIncorrectlyOrderedGeneric.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationIncorrectlyOrderedGeneric.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Get.ToString(),
                        Path = "/V3/samples/",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(UnorderedGenericTypeException).Name,
                                Message = SpecificationGenerationMessages.UnorderedGenericType
                            }
                        }
                    }
                }
            };

            // Body parameter missing see tag
            yield return new object[]
            {
                "Body parameter missing see tag",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationRequestMissingSeeTag.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationRequestMissingSeeTag.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Post.ToString(),
                        Path = "/V3/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(InvalidRequestBodyException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.MissingSeeCrefTag,
                                    "sampleObject")
                            }
                        }
                    }
                }
            };

            // Type not found in provided contract assemblies
            yield return new object[]
            {
                "Type not found",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationTypeNotFound.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationTypeNotFound.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = OperationType.Post.ToString(),
                        Path = "/V3/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(TypeLoadException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.TypeNotFound,
                                    "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.TestNotFound",
                                    string.Join(" ", new List<string>
                                    {
                                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll",
                                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll"
                                    }))
                            }
                        }
                    }
                }
            };
        }

        public static IEnumerable<object[]> GetTestCasesForInvalidDocumentationShouldYieldFailure()
        {
            // Invalid Verb
            yield return new object[]
            {
                "Invalid Verb",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationInvalidVerb.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationInvalidVerb.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = "Invalid",
                        Path = "/V1/samples/{id}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(InvalidVerbException).Name,
                                Message = string.Format(SpecificationGenerationMessages.InvalidHttpMethod, "Invalid")
                            }
                        }
                    }
                }
            };

            // Invalid Uri
            yield return new object[]
            {
                "Invalid Uri",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationInvalidUri.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationInvalidUri.Json"),
                new DocumentGenerationDiagnostic
                {
                    Errors =
                    {
                        new GenerationError
                        {
                            ExceptionType = typeof(UnableToGenerateAllOperationsException).Name,
                            Message = string.Format(
                                SpecificationGenerationMessages.UnableToGenerateAllOperations,
                                8,
                                9)
                        }
                    }
                },
                new List<OperationGenerationDiagnostic>
                {
                    new OperationGenerationDiagnostic
                    {
                        OperationMethod = SpecificationGenerationMessages.OperationMethodNotParsedGivenUrlIsInvalid,
                        Path = "http://{host}:9000/V1/samples/{id}?queryBool={queryBool}",
                        Errors =
                        {
                            new GenerationError
                            {
                                ExceptionType = typeof(InvalidUrlException).Name,
                                Message = string.Format(
                                    SpecificationGenerationMessages.InvalidUrl,
                                    "http://{host}:9000/V1/samples/{id}?queryBool={queryBool}",
                                    SpecificationGenerationMessages.MalformattedUrl)
                            }
                        }
                    }
                }
            };
        }

        public static IEnumerable<object[]> GetTestCasesForPassANewFilterAndShouldReturnCorrectDocument()
        {
            // Standard, original valid XML document
            yield return new object[]
            {
                "Standard valid XML document",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationNewFilter.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                new UpdateDescriptionFilter(),
                1,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationNewFilter.Json")
            };
        }

        public static IEnumerable<object[]> GetTestCasesForPassingDescriptionShouldReturnCorrectDocument()
        {
            // Description from markdown file
            yield return new object[]
            {
                "Description from markdown file",
                new List<string>
                {
                    Path.Combine(InputDirectory, "Annotation.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                Path.Combine(InputDirectory, "DescriptionMarkdown.md"),
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithDescription.Json")
            };
        }

        public static IEnumerable<object[]> GetTestCasesForValidDocumentationShouldReturnCorrectDocument()
        {
            // Standard, original valid XML document
            yield return new object[]
            {
                "Standard valid XML document",
                new List<string>
                {
                    Path.Combine(InputDirectory, "Annotation.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "Annotation.Json")
            };

            // Standard, original XML document with no response body
            yield return new object[]
            {
                "Standard valid XML document with no response body.",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationWithNoResponseBody.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "2.0.0",
                1,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithNoResponseBody.Json")
            };

            // Valid XML document but with parameters that have no in attributes but are present in the URL.
            yield return new object[]
            {
                "Parameters Without In Attribute But Present In URL",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationParamWithoutInButPresentInUrl.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationParamWithoutInButPresentInUrl.Json")
            };

            // Valid XML document but with one parameter without specified type.
            // The type should simply default to string.
            yield return new object[]
            {
                "Unspecified Type Default to String",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationParamNoTypeSpecified.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationParamNoTypeSpecified.Json")
            };

            // Valid XML document with multiple response types per response code.
            yield return new object[]
            {
                "Multiple Response Types Per Response Code",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationMultipleResponseTypes.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationMultipleResponseTypes.Json")
            };

            // Valid XML document with multiple request types.
            yield return new object[]
            {
                "Multiple Request Types",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationMultipleRequestTypes.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationMultipleRequestTypes.Json")
            };

            // Valid XML document with multiple request content types.
            yield return new object[]
            {
                "Multiple Request Media Types",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationMultipleRequestMediaTypes.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationMultipleRequestMediaTypes.Json")
            };

            // Valid XML document with multiple response content types.
            yield return new object[]
            {
                "Multiple Response Media Types Per Response Code",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationMultipleResponseMediaTypes.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationMultipleResponseMediaTypes.Json")
            };

            // Valid XML document with optional path parameters.
            yield return new object[]
            {
                "Optional Path Parameters",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationOptionalPathParametersBranching.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationOptionalPathParametersBranching.Json")
            };

            // Valid XML document with alternative param tags.
            yield return new object[]
            {
                "Alternative Param Tags (i.e. queryParam, pathParam, header)",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationAlternativeParamTags.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationAlternativeParamTags.Json")
            };

            // Valid XML document with array type in param tags.
            yield return new object[]
            {
                "Array Type in Param Tags",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationArrayInParamTags.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationArrayInParamTags.Json")
            };

            // Valid XML document with summary including tags
            yield return new object[]
            {
                "Summary With Tags (see cref or paramref)",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationSummaryWithTags.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationSummaryWithTags.Json")
            };

            // XML document with examples
            yield return new object[]
            {
                "XML document with examples",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationExample.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                2,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationExample.Json")
            };

            // XML document with contract types derive from base class
            yield return new object[]
            {
                "XML document with contract types derive from base class",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationWithBaseClassProperty.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                2,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithBaseClassProperty.Json")
            };

            // XML document with security tags
            yield return new object[]
            {
                "XML document with security tags",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationWithSecurityTags.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                2,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithSecurityTags.Json")
            };

            // XML document with runtime serialization
            yield return new object[]
            {
                "XML document with runtime serialization",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationWithRuntimeSerialization.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                15,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationWithRuntimeSerialization.Json")
            };

            yield return new object[]
            {
                "All operations have predefined operation Id",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationPredefinedOperationId.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationPredefinedOperationId.Json")
            };

            yield return new object[]
            {
                "A few operations have predefined operation Id, the others are generated",
                new List<string>
                {
                    Path.Combine(InputDirectory, "AnnotationPredefinedAndGeneratedOperationId.xml"),
                    Path.Combine(InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.xml")
                },
                new List<string>
                {
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.SampleApis.dll"),
                    Path.Combine(
                        InputDirectory,
                        "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.Contracts.dll")
                },
                "1.0.0",
                9,
                Path.Combine(
                    OutputDirectory,
                    "AnnotationPredefinedAndGeneratedOperationId.Json")
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForInvalidDocumentationShouldRemoveFailedOperations))]
        public void InvalidDocumentationShouldRemoveFailedOperations(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            int expectedOperationGenerationResultsCount,
            string expectedJsonFile,
            DocumentGenerationDiagnostic expectedDocumentGenerationResult,
            IList<OperationGenerationDiagnostic> expectedFailureOperationGenerationResults)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(documents, inputBinaryFiles, "1.0.0", FilterSetVersion.V1);

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();

            var openApiDocuments = generator.GenerateDocuments(input, out result);

            result.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Should().BeEquivalentTo(expectedDocumentGenerationResult);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();
            result.OperationGenerationDiagnostics.Count.Should().Be(expectedOperationGenerationResultsCount);

            var failedPaths = result.OperationGenerationDiagnostics.Where(
                    p => p.Errors.Count > 0)
                .ToList();

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            failedPaths.Should().BeEquivalentTo(expectedFailureOperationGenerationResults);

            // We are doing serialization and deserialization to force the resulting actual document
            // to have the exact fields we will see in the resulting document based on the contract resolver.
            // Without serialization and deserialization, the actual document may have fields that should
            // not be present, such as empty list fields.
            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForInvalidDocumentationShouldYieldFailure))]
        public void InvalidDocumentationShouldYieldFailure(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            int expectedOperationGenerationResultsCount,
            string expectedJsonFile,
            DocumentGenerationDiagnostic expectedDocumentGenerationResult,
            IList<OperationGenerationDiagnostic> expectedFailureOperationGenerationResults)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(documents, inputBinaryFiles, "1.0.0", FilterSetVersion.V1);

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();

            var openApiDocuments = generator.GenerateDocuments(input, out result);

            openApiDocuments.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Should().BeEquivalentTo(expectedDocumentGenerationResult);
            result.OperationGenerationDiagnostics.Count.Should().Be(expectedOperationGenerationResultsCount);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();

            var failurePaths = result.OperationGenerationDiagnostics.Where(
                    p => p.Errors.Count > 0)
                .ToList();

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);


            _output.WriteLine(actualDocument);

            failurePaths.Should().BeEquivalentTo(expectedFailureOperationGenerationResults);

            // We are doing serialization and deserialization to force the resulting actual document
            // to have the exact fields we will see in the resulting document based on the contract resolver.
            // Without serialization and deserialization, the actual document may have fields that should
            // not be present, such as empty list fields.
            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }

        [Theory]
        [InlineData("1.0.0")]
        public void NoOperationsToParseShouldReturnEmptyDocument(string openApiDocumentVersion)
        {
            var path = Path.Combine(InputDirectory, "AnnotationNoOperationsToParse.xml");

            var document = XDocument.Load(path);

            var input = new OpenApiGeneratorConfig(
                new List<XDocument> {document},
                new List<string>(),
                openApiDocumentVersion,
                FilterSetVersion.V1);

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();
            var openApiDocument = generator.GenerateDocument(input, out result);

            result.Should().NotBeNull();
            openApiDocument.Should().BeNull();
            result.DocumentGenerationDiagnostic.Should()
                .BeEquivalentTo(
                    new DocumentGenerationDiagnostic
                    {
                        Errors =
                        {
                            new GenerationError
                            {
                                Message = SpecificationGenerationMessages.NoOperationElementFoundToParse
                            }
                        }
                    }
                );
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForPassANewFilterAndShouldReturnCorrectDocument))]
        public void PassANewFilterAndShouldReturnCorrectDocument(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            IFilter filter,
            int expectedOperationGenerationResultsCount,
            string expectedJsonFile)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(documents, inputBinaryFiles, "1.0.0", FilterSetVersion.V1);
            input.OpenApiGeneratorFilterConfig.Filters.Add(filter);

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();
            var openApiDocuments = generator.GenerateDocuments(input, out result);

            result.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Errors.Count.Should().Be(0);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();

            result.OperationGenerationDiagnostics.Count(p => p.Errors.Count > 0).Should().Be(0);
            result.OperationGenerationDiagnostics.Count.Should().Be(expectedOperationGenerationResultsCount);

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForPassingDescriptionShouldReturnCorrectDocument))]
        public void PassingDescriptionShouldReturnCorrectDocument(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            string descriptionFile,
            string expectedJsonFile)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(
                annotationXmlDocuments: documents,
                assemblyPaths: inputBinaryFiles,
                openApiDocumentVersion: "1",
                filterSetVersion: FilterSetVersion.V1)
            {
                OpenApiInfoDescription = File.ReadAllText(descriptionFile).Replace("\r", "")
            };

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();
            var openApiDocuments = generator.GenerateDocuments(input, out result);

            result.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Errors.Count.Should().Be(0);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();

            result.OperationGenerationDiagnostics.Count(p => p.Errors.Count > 0).Should().Be(0);

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForValidDocumentationShouldReturnCorrectDocument))]
        public void ValidDocumentationShouldReturnCorrectDocument(
            string testCaseName,
            IList<string> inputXmlFiles,
            IList<string> inputBinaryFiles,
            string openApiDocumentVersion,
            int expectedOperationGenerationResultsCount,
            string expectedJsonFile)
        {
            _output.WriteLine(testCaseName);

            var documents = new List<XDocument>();

            documents.AddRange(inputXmlFiles.Select(XDocument.Load));

            var input = new OpenApiGeneratorConfig(
                documents,
                inputBinaryFiles,
                openApiDocumentVersion,
                FilterSetVersion.V1);

            GenerationDiagnostic result;

            var generator = new OpenApiGenerator();
            var openApiDocuments = generator.GenerateDocuments(input, out result);

            result.Should().NotBeNull();

            result.DocumentGenerationDiagnostic.Errors.Count.Should().Be(0);

            openApiDocuments[DocumentVariantInfo.Default].Should().NotBeNull();

            result.OperationGenerationDiagnostics.Count(p => p.Errors.Count > 0).Should().Be(0);
            result.OperationGenerationDiagnostics.Count.Should().Be(expectedOperationGenerationResultsCount);

            var actualDocument = openApiDocuments[DocumentVariantInfo.Default]
                .SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            var openApiStringReader = new OpenApiStringReader();

            var actualDeserializedDocument = openApiStringReader.Read(
                actualDocument,
                out OpenApiDiagnostic diagnostic);

            diagnostic.Errors.Count.Should().Be(0);

            actualDeserializedDocument
                .Should()
                .BeEquivalentTo(openApiStringReader.Read(expectedDocument, out var _));
        }
    }
}