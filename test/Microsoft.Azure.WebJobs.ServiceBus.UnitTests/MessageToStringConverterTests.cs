﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.ServiceBus.Triggers;
using Microsoft.Azure.ServiceBus;
using Xunit;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus.InteropExtensions;

namespace Microsoft.Azure.WebJobs.ServiceBus.UnitTests
{
    public class MessageToStringConverterTests
    {
        private const string TestString = "This is a test!";
        private const string TestJson = "{ value: 'This is a test!' }";

        [Theory]
        [InlineData(ContentTypes.TextPlain, TestString)]
        [InlineData(ContentTypes.ApplicationJson, TestJson)]
        [InlineData(ContentTypes.ApplicationOctetStream, TestString)]
        [InlineData(null, TestJson)]
        [InlineData("application/xml", TestJson)]
        public async Task ConvertAsync_ReturnsExpectedResult_WithBinarySerializer(string contentType, string value)
        {
            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractBinarySerializer<string>.Instance.WriteObject(ms, value);
                bytes = ms.ToArray();
            }

            Message message = new Message(bytes);
            message.ContentType = contentType;

            MessageToStringConverter converter = new MessageToStringConverter();
            string result = await converter.ConvertAsync(message, CancellationToken.None);

            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(ContentTypes.TextPlain, TestString)]
        [InlineData(ContentTypes.ApplicationJson, TestJson)]
        [InlineData(ContentTypes.ApplicationOctetStream, TestString)]
        [InlineData(null, TestJson)]
        [InlineData("application/xml", TestJson)]
        public async Task ConvertAsync_ReturnsExpectedResult_WithSerializedString(string contentType, string value)
        {
            Message message = new Message(Encoding.UTF8.GetBytes(value));
            message.ContentType = contentType;

            MessageToStringConverter converter = new MessageToStringConverter();
            string result = await converter.ConvertAsync(message, CancellationToken.None);
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task ConvertAsync_Throws_WithSerializedObject()
        {

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractBinarySerializer<TestObject>.Instance.WriteObject(ms, new TestObject() { Text = "Test" });
                bytes = ms.ToArray();
            }
            
            Message message = new Message(bytes);

            MessageToStringConverter converter = new MessageToStringConverter();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => converter.ConvertAsync(message, CancellationToken.None));

            Assert.IsType<SerializationException>(exception.InnerException);
            Assert.StartsWith("The Message with ContentType 'null' failed to deserialize to a string with the message:", exception.Message);
        }

        [Serializable]
        public class TestObject
        {
            public string Text { get; set; }
        }
    }
}
