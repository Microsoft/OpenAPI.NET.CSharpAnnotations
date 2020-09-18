// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters;
using Microsoft.OpenApi.Models;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.FilterTests
{
    [Collection("DefaultSettings")]
    public class CreateOperationMetaFilterTest
    {
        private readonly ITestOutputHelper _output;

        public CreateOperationMetaFilterTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForApplyShouldCallOnlyTheFirstFilterWhichIsApplicable))]
        public void ApplyShouldCallOnlyTheFirstFilterWhichIsApplicable(
            string testName,
            List<Mock<ICreateOperationPreProcessingOperationFilter>> mockFilters,
            int firstApplicableFilterIndex)
        {
            _output.WriteLine(testName);

            // Prepare
            var openApiPaths = new OpenApiPaths();
            XElement element = new XElement("elem");
            var settings = new PreProcessingOperationFilterSettings();

            List<ICreateOperationPreProcessingOperationFilter> filters =
                mockFilters.Select(mockFilter => mockFilter.Object).ToList();

            var filter = new CreateOperationMetaFilter(filters);

            // Action
            filter.Apply(openApiPaths, element, settings);

            // Assert
            for(int i = 0; i < firstApplicableFilterIndex - 1; ++i)
            {
                mockFilters[i].Verify(mock => mock.Apply(
                    It.IsAny<OpenApiPaths>(), It.IsAny<XElement>(), It.IsAny<PreProcessingOperationFilterSettings>()),
                    Times.Never());
            }

            for (int i = firstApplicableFilterIndex + 1; i < mockFilters.Count; ++i)
            {
                mockFilters[i].Verify(mock => mock.Apply(
                    It.IsAny<OpenApiPaths>(), It.IsAny<XElement>(), It.IsAny<PreProcessingOperationFilterSettings>()),
                    Times.Never());
            }

            mockFilters[firstApplicableFilterIndex].Verify(mock => mock.Apply(
                openApiPaths, element, settings), Times.Once());
        }

        [Fact]
        public void ApplyShouldReturnErrorIfNoneOfTheFiltersCouldBeApplied()
        {
            // Prepare
            var openApiPaths = new OpenApiPaths();
            XElement element = new XElement("elem");
            var settings = new PreProcessingOperationFilterSettings();

            var mockFilters = new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(false),
                    CreateMockFilter(false),
                    CreateMockFilter(false)};

            List<ICreateOperationPreProcessingOperationFilter> filters =
                mockFilters.Select(mockFilter => mockFilter.Object).ToList();

            var filter = new CreateOperationMetaFilter(filters);

            // Action
            var errorList = filter.Apply(openApiPaths, element, settings);

            // Assert
            errorList.Should().OnlyContain(error => error.ExceptionType == "InvalidOperationException");
        }

        [Fact]
        public void ApplyShouldNotReturnErrorIfTheAppliedFilterDoesNotReturnError()
        {
            // Prepare
            var openApiPaths = new OpenApiPaths();
            XElement element = new XElement("elem");
            var settings = new PreProcessingOperationFilterSettings();

            var successfullFilter = CreateMockFilter(true);

            var mockFilters = new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    successfullFilter};

            List<ICreateOperationPreProcessingOperationFilter> filters =
                mockFilters.Select(mockFilter => mockFilter.Object).ToList();

            var filter = new CreateOperationMetaFilter(filters);

            // Action
            var errorList = filter.Apply(openApiPaths, element, settings);

            // Assert
            errorList.Should().BeEmpty();
        }

        [Fact]
        public void ApplyShouldReturnTheErrorListOfTheAppliedFilter()
        {
            // Prepare
            var openApiPaths = new OpenApiPaths();
            XElement element = new XElement("elem");
            var settings = new PreProcessingOperationFilterSettings();

            var mockFilterWithError = new Mock<ICreateOperationPreProcessingOperationFilter>();

            var expectedErrorList = new List<GenerationError>
            {
                new GenerationError
                {
                    Message = "Message_1",
                    ExceptionType = "ExceptionType_1"
                },
                new GenerationError
                {
                    Message = "Message_2",
                    ExceptionType = "ExceptionType_2"
                }
            };

            mockFilterWithError.Setup(
                mock => mock.Apply(
                    It.IsAny<OpenApiPaths>(), It.IsAny<XElement>(), It.IsAny<PreProcessingOperationFilterSettings>()))
                .Returns(expectedErrorList);

            mockFilterWithError.Setup(mock => mock.IsApplicable(It.IsAny<XElement>())).Returns(true);

            var mockFilters = new List<Mock<ICreateOperationPreProcessingOperationFilter>>
            {
                mockFilterWithError
            };

            List<ICreateOperationPreProcessingOperationFilter> filters =
                mockFilters.Select(mockFilter => mockFilter.Object).ToList();

            // Action
            var filter = new CreateOperationMetaFilter(filters);
            var errorList = filter.Apply(openApiPaths, element, settings);

            // Assert
            errorList.Should().ContainInOrder(expectedErrorList);
        }

        public static IEnumerable<object[]> GetTestCasesForApplyShouldCallOnlyTheFirstFilterWhichIsApplicable()
        {
            yield return new object[]
            {
                "Only the first filter is applicable",
                new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(true),
                    CreateMockFilter(false),
                    CreateMockFilter(false)
                },
                0
            };

            yield return new object[]
{
                "Only the last filter is applicable",
                new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(false),
                    CreateMockFilter(false),
                    CreateMockFilter(true)
                },
                2
            };

            yield return new object[]
            {
                "Only one filter in the middle is applicable",
                new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(false),
                    CreateMockFilter(true),
                    CreateMockFilter(false)
                },
                1
            };

            yield return new object[]
            {
                "The first applicable filter masks the other applicable filters",
                new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(false),
                    CreateMockFilter(true),
                    CreateMockFilter(true)
                },
                1
            };

            yield return new object[]
            {
                "If all filters are applicable, only the first one is called",
                new List<Mock<ICreateOperationPreProcessingOperationFilter>>{
                    CreateMockFilter(true),
                    CreateMockFilter(true),
                    CreateMockFilter(true)
                },
                0
            };
        }

        private static Mock<ICreateOperationPreProcessingOperationFilter> CreateMockFilter(bool applicable)
        {
            var mockFilter = new Mock<ICreateOperationPreProcessingOperationFilter>();

            mockFilter.Setup(
                mock => mock.Apply(
                    It.IsAny<OpenApiPaths>(), It.IsAny<XElement>(), It.IsAny<PreProcessingOperationFilterSettings>()))
                .Returns(new List<GenerationError>());

            mockFilter.Setup(mock => mock.IsApplicable(It.IsAny<XElement>())).Returns(applicable);

            return mockFilter;
        }
    }
}