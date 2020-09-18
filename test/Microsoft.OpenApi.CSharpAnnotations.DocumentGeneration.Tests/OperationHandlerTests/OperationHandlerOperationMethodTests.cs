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
using Microsoft.OpenApi.Models;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Tests.FilterTests
{
    [Collection("DefaultSettings")]
    public class OperationHandlerOperationMethodTests
    {
        private readonly ITestOutputHelper _output;

        public OperationHandlerOperationMethodTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForGetOperationMethodShouldUseTheVerbXmlTagAsOperationType))]
        public void GetOperationMethodShouldUseTheVerbXmlTagAsOperationType(
            string testName,
            XElement element,
            OperationType expectedOperationType)
        {
            _output.WriteLine(testName);

            // Action
            var result = OperationHandler.GetOperationMethod(element);

            // Assert
            result.Should().BeEquivalentTo(expectedOperationType);
        }

        [Fact]
        public void GetOperationMethodShouldThrowInvalidVerbExceptionByMissingVerbTag()
        {
            var element = XElement.Parse($@"
                <member>
                    <!--verb>Missing tag!</verb-->
                </member>
            ");

            // Action
            Action action = () => { OperationHandler.GetOperationMethod(element); };

            // Assert
            action.Should().ThrowExactly<InvalidVerbException>();
        }

        public static IEnumerable<object[]> GetTestCasesForGetOperationMethodShouldUseTheVerbXmlTagAsOperationType()
        {
            var operationTypeStringList = new string[]
            {
                "get", "put", "post", "delete", "options", "head", "patch", "trace"
            };

            var expectedOperationTypeList = new OperationType[]
            {
                    OperationType.Get, OperationType.Put, OperationType.Post, OperationType.Delete,
                    OperationType.Options, OperationType.Head, OperationType.Patch, OperationType.Trace
            };

            var operationTypeList = operationTypeStringList.Zip(expectedOperationTypeList, (s, t) => new { Input = s, Exp = t });
            foreach (var item in operationTypeList)
            {
                var element = XElement.Parse($@"
                    <member>
                        <verb>{item.Input}</verb>
                    </member>
                ");

                yield return new object[]
                {
                    "Test for " + item.Input,
                    element,
                    item.Exp
                };
            }
        }
    }
}