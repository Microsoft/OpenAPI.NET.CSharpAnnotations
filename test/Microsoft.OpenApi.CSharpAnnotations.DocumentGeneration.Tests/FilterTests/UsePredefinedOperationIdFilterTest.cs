// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.PreprocessingOperationFilters;
using Microsoft.OpenApi.Models;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.FilterTests
{
    [Collection("DefaultSettings")]
    public class UsePredefinedOperationIdFilterTest
    {
        private readonly ITestOutputHelper _output;

        public UsePredefinedOperationIdFilterTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForIsApplicableShouldReferToTheHasOperationIdPredicate))]
        public void IsApplicableShouldReferToTheHasOperationIdPredicate(
            string testName,
            bool shouldBeApplicable)
        {
            _output.WriteLine(testName);

            // Prepare
            XElement calledElement = null;
            bool mockHasOperationIdFunc(XElement e)
            {
                calledElement = e;
                return shouldBeApplicable;
            }

            var filter = new UsePredefinedOperationIdFilter(
                DefaultGetUrlFunc,
                DefaultGetOperationMethodFunc,
                DefaultGetOperationIdFunc,
                mockHasOperationIdFunc);

            var element = XElement.Parse("<xml>Not used in this test due of the mocked function.</xml>");

            // Action
            var result = filter.IsApplicable(element);

            // Assert
            result.Should().Be(shouldBeApplicable);
        }

        [Fact]
        public void ApplyShouldCreateTheExpectedOpenApiPath()
        {
            // Prepare
            var expectedUrl = "/expected/operation/path";
            var expectedOperationType = OperationType.Post;
            var expectedOperationId = "Expected_Operation_Id";

            string MockGetUrlFunc(XElement e)
            {
                return (string)expectedUrl.Clone();
            }

            OperationType MockGetOperationTypeFunc(XElement e)
            {
                return expectedOperationType;
            }

            string MockGetOperationIdFunc(XElement e)
            {
                return (string)expectedOperationId.Clone();
            }


            var filter = new UsePredefinedOperationIdFilter(
                MockGetUrlFunc,
                MockGetOperationTypeFunc,
                MockGetOperationIdFunc,
                DefaultHasOperationIdFunc);

            var element = XElement.Parse("<xml>Not used in this test due of the mocked functions.</xml>");

            // Action
            var openApiPaths = new OpenApiPaths();
            var result = filter.Apply(openApiPaths, element, new PreProcessingOperationFilterSettings());

            // Assert
            result.Should().BeEmpty();

            openApiPaths.Keys.Count.Should().Be(1);
            openApiPaths.Should().ContainKey(expectedUrl);

            openApiPaths[expectedUrl].Operations.Keys.Count.Should().Be(1);
            openApiPaths[expectedUrl].Operations.Should().ContainKey(expectedOperationType);

            openApiPaths[expectedUrl].Operations[expectedOperationType]
                .OperationId.Should().BeEquivalentTo(expectedOperationId);
        }

        [Fact]
        public void ApplyShouldReturnTheExpectedErrorIfGetUrlThrowsException()
        {
            // Prepare
            string MockGetUlrFunc(XElement e)
            {
                throw new InvalidUrlException();
            }

            var filter = new UsePredefinedOperationIdFilter(
                MockGetUlrFunc,
                DefaultGetOperationMethodFunc,
                DefaultGetOperationIdFunc,
                DefaultHasOperationIdFunc);

            var element = XElement.Parse("<xml>Not used in this test due of the mocked functions.</xml>");

            // Action
            var openApiPaths = new OpenApiPaths();
            var result = filter.Apply(openApiPaths, element, new PreProcessingOperationFilterSettings());

            // Assert
            result.Count.Should().Be(1);
            result[0].ExceptionType.Should().Be("InvalidUrlException");
            openApiPaths.Keys.Count.Should().Be(0);
        }

        [Fact]
        public void ApplyShouldReturnTheExpectedErrorIfGetOperationMethodThrowsException()
        {
            // Prepare
            OperationType MockGetOperationMethodFunc(XElement e)
            {
                throw new InvalidVerbException();
            }

            var filter = new UsePredefinedOperationIdFilter(
                DefaultGetUrlFunc,
                MockGetOperationMethodFunc,
                DefaultGetOperationIdFunc,
                DefaultHasOperationIdFunc);

            var element = XElement.Parse("<xml>Not used in this test due of the mocked functions.</xml>");

            // Action
            var openApiPaths = new OpenApiPaths();
            var result = filter.Apply(openApiPaths, element, new PreProcessingOperationFilterSettings());

            // Assert
            result.Count.Should().Be(1);
            result[0].ExceptionType.Should().Be("InvalidVerbException");
            openApiPaths.Keys.Count.Should().Be(0);
        }

        [Fact]
        public void ApplyShouldReturnTheExpectedErrorIfGetOperationIdThrowsException()
        {
            // Prepare
            string MockGetOperationIdFunc(XElement e)
            {
                throw new InvalidOperationIdException();
            }

            var filter = new UsePredefinedOperationIdFilter(
                DefaultGetUrlFunc,
                DefaultGetOperationMethodFunc,
                MockGetOperationIdFunc,
                DefaultHasOperationIdFunc);

            var element = XElement.Parse("<xml>Not used in this test due of the mocked functions.</xml>");

            // Action
            var openApiPaths = new OpenApiPaths();
            var result = filter.Apply(openApiPaths, element, new PreProcessingOperationFilterSettings());

            // Assert
            result.Count.Should().Be(1);
            result[0].ExceptionType.Should().Be("InvalidOperationIdException");
            openApiPaths.Keys.Count.Should().Be(0);
        }

        public static IEnumerable<object[]> GetTestCasesForIsApplicableShouldReferToTheHasOperationIdPredicate()
        {
            yield return new object[]
            {
                "Applicable is HasOperationId returns true",
                true
            };

            yield return new object[]
            {
                "Not applicable is HasOperationId returns false",
                false
            };
        }

        [Fact]
        public void ApplyShouldProcessTheXmlTagProperlyEndToEnd()
        {
            // Prepare
            var element = XElement.Parse(@"
                    <member>
                        <summary>Assign Role</summary>
                        <param name=""role"">Role</param>
                        <url>https://localhost/v2/role/{role}/assign</url>
                        <verb>put</verb>
                        <operationId>AccessControl_AssignRole</operationId>
                    </member>
                ");

            var openApiPaths = new OpenApiPaths();
            var settings = new PreProcessingOperationFilterSettings();

            var filter = new UsePredefinedOperationIdFilter();

            // Action
            filter.Apply(openApiPaths, element, settings);

            // Assert
            string url = "/v2/role/{role}/assign";
            OperationType operationType = OperationType.Put;
            var expectedOperationId = "AccessControl_AssignRole";

            openApiPaths.Should().ContainKey(url);
            openApiPaths[url].Operations.Should().ContainKey(operationType);

            openApiPaths[url].Operations[operationType]
                .OperationId.Should().BeEquivalentTo(expectedOperationId);
        }

        private static string DefaultGetUrlFunc(XElement e)
        {
            return "/default/url";
        }

        private static OperationType DefaultGetOperationMethodFunc(XElement e)
        {
            return OperationType.Post;
        }

        private static string DefaultGetOperationIdFunc(XElement e)
        {
            return "Default_Operation_Id";
        }

        private static bool DefaultHasOperationIdFunc(XElement e)
        {
            return true;
        }
    }
}