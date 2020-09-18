// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.FilterTests
{
    [Collection("DefaultSettings")]
    public class OperationHandlerOperationIdTests
    {
        private readonly ITestOutputHelper _output;

        public OperationHandlerOperationIdTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForHasOperationIdShouldReferToTheOperationIdXmlTag))]
        public void HasOperationIdShouldReferToTheOperationIdXmlTag(
            string testName,
            XElement element,
            bool expectedResult)
        {
            _output.WriteLine(testName);
            
            // Action
            var result = OperationHandler.HasOperationId(element);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void HasOperationIdShouldReturnFalseByNullInput()
        {
            // Action
            var result = OperationHandler.HasOperationId(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetOperationIdShouldUseTheOperationIdXmlTagAsOperationId()
        {
            // Prepare
            var element = XElement.Parse(@"
                    <member>
                        <operationId>AccessControl_AssignRole</operationId>
                    </member>
                ");

            // Action
            var result = OperationHandler.GetOperationId(element);

            // Assert
            var expectedOperationId = "AccessControl_AssignRole";
            result.Should().BeEquivalentTo(expectedOperationId);
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForGetOperationIdShouldThrowInvalidOperationIdExceptionByWrongXml))]
        public void GetOperationIdShouldThrowInvalidOperationIdExceptionByWrongXml(
            string testName,
            XElement element)
        {
            _output.WriteLine(testName);

            // Action
            Action action = () => { OperationHandler.GetOperationId(element); };

            // Assert
            action.Should().ThrowExactly<InvalidOperationIdException>();
        }

        public static IEnumerable<object[]> GetTestCasesForHasOperationIdShouldReferToTheOperationIdXmlTag()
        {
            yield return new object[]
            {
                "Return false if no operationId XML tag is present",
                XElement.Parse(@"
                    <member>
                        <!--operationId>Missing tag!</operationId-->
                    </member>
                "),
                false
            };

            yield return new object[]
            {
                "Return true if one operationId XML tag is present",
                XElement.Parse(@"
                    <member>
                        <operationId>AccessControl_AssignRole</operationId>
                    </member>
                "),
                true
            };

            yield return new object[]
            {
                "Return true if multiple operationId XML tags are present",
                XElement.Parse(@"
                    <member>
                        <operationId>AssignRole</operationId>
                        <operationId>AccessControl_AssignRole</operationId>
                    </member>
                "),
                true
            };
        }

        public static IEnumerable<object[]> GetTestCasesForGetOperationIdShouldThrowInvalidOperationIdExceptionByWrongXml()
        {
            yield return new object[]
            {
                "Missing operationId tag",
                XElement.Parse(@"
                    <member>
                        <!--operationId>Missing operationId tag</operationId-->
                    </member>
                ")
            };

            yield return new object[]
            {
                "More than one operationId tag",
                XElement.Parse(@"
                    <member>
                        <operationId>First operation id</operationId>
                        <operationId>Second operation id</operationId>
                    </member>
                ")
            };
        }
    }
}