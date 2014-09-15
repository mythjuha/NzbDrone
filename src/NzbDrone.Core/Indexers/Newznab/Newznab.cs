﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class Newznab : RssIndexerBase<NewznabSettings>
    {
        public override DownloadProtocol Protocol { get { return DownloadProtocol.Usenet; } }
        public override Int32 PageSize { get { return 100; } }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NewznabRequestGenerator()
            {
                PageSize = PageSize, 
                Settings = Settings
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NewznabRssParser();
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                yield return GetDefinition("Nzbs.org", GetSettings("http://nzbs.org", 5000));
                yield return GetDefinition("Nzb.su", GetSettings("https://api.nzb.su"));
                yield return GetDefinition("Dognzb.cr", GetSettings("https://api.dognzb.cr"));
                yield return GetDefinition("OZnzb.com", GetSettings("https://www.oznzb.com"));
                yield return GetDefinition("nzbplanet.net", GetSettings("https://nzbplanet.net"));
                yield return GetDefinition("NZBgeek", GetSettings("https://api.nzbgeek.info"));
            }
        }

        public Newznab(IHttpClient httpClient, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, configService, parsingService, logger)
        {

        }

        private IndexerDefinition GetDefinition(String name, NewznabSettings settings)
        {
            return new IndexerDefinition
                   {
                       EnableRss = false,
                       EnableSearch = false,
                       Name = name,
                       Implementation = GetType().Name,
                       Settings = settings,
                       Protocol = DownloadProtocol.Usenet,
                       SupportsRss = SupportsRss,
                       SupportsSearch = SupportsSearch
                   };
        }

        private NewznabSettings GetSettings(String url, params int[] categories)
        {
            var settings = new NewznabSettings { Url = url };

            if (categories.Any())
            {
                settings.Categories = categories;
            }

            return settings;
        }
    }
}
