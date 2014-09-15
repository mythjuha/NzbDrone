﻿using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Fanzub;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Test.IndexerTests.FanzubTests
{
    [TestFixture]
    public class FanzubFixture : CoreTest<Fanzub>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
                {
                    Name = "Fanzub",
                    Settings = new NullConfig()
                };
        }

        [Test]
        public void should_parse_recent_feed_from_fanzub()
        {
            var recentFeed = ReadAllText(@"Files/RSS/Fanzub.xml");

            Mocker.GetMock<IHttpClient>()
                .Setup(o => o.Get(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), recentFeed));
            
            var releases = Subject.FetchRecent();

            releases.Should().HaveCount(3);

            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("[Vivid] Hanayamata - 10 [A33D6606]");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
            releaseInfo.DownloadUrl.Should().Be("http://fanzub.com/nzb/296464/Vivid%20Hanayamata%20-%2010.nzb");
            releaseInfo.InfoUrl.Should().BeNullOrEmpty();
            releaseInfo.CommentUrl.Should().BeNullOrEmpty();
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2014/09/13 12:56:53"));
            releaseInfo.Size.Should().Be(556246858);
        }
    }
}
