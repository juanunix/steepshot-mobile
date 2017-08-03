﻿using System;
using System.IO;
using NUnit.Framework;
using Steepshot.Core.Authority;
using Steepshot.Core.Models.Requests;

namespace Sweetshot.Tests
{
    [TestFixture]
    public class UnitTests
    {
        [Test]
        public void Vote_Empty_Identifier()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new VoteRequest(new UserInfo() { SessionId = "sessionId", PostingKey = "" }, true, "");
            });
            Assert.That(ex.ParamName, Is.EqualTo("identifier"));
        }

        [Test]
        public void Follow_Empty_Username()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new FollowRequest(new UserInfo() { SessionId = "sessionId", PostingKey = "" }, FollowType.Follow, "");
            });
            Assert.That(ex.ParamName, Is.EqualTo("username"));
        }

        [Test]
        public void Comments_Empty_Url()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new GetCommentsRequest("");
            });
            Assert.That(ex.ParamName, Is.EqualTo("url"));
        }

        [Test]
        public void CreateComment_Empty_Url()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new CreateCommentRequest(new UserInfo() { SessionId = "sessionId", PostingKey = "" }, "", "test", "test");
            });
            Assert.That(ex.ParamName, Is.EqualTo("url"));
        }

        [Test]
        public void Upload_Base64_Equals_ByteArray()
        {
            // Arrange
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var path = Path.Combine(dir.Parent.Parent.FullName, @"Data/cat.jpg");
            var file = File.ReadAllBytes(path);

            // Act
            var requestArray = new UploadImageRequest(new UserInfo() { SessionId = "sessionId", PostingKey = "" }, "cat" + DateTime.UtcNow.Ticks, file, "cat1", "cat2", "cat3", "cat4");
            var base64 = Convert.ToBase64String(file);
            var requestBase64 = new UploadImageRequest(new UserInfo() { SessionId = "sessionId", PostingKey = "" }, "cat" + DateTime.UtcNow.Ticks, base64, "cat1", "cat2", "cat3", "cat4");

            // Assert
            Assert.That(requestArray.Photo, Is.EqualTo(requestBase64.Photo));
            Assert.That(requestArray.Photo.Length, Is.EqualTo(requestBase64.Photo.Length));
        }
    }
}