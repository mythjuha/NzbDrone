using System;
using System.Collections.Generic;
using NzbDrone.Core.ThingiProvider;
using FluentValidation.Results;
using System.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NLog;

namespace NzbDrone.Core.Indexers.IPTorrents
{
    public class IPTorrents : RssIndexerBase<IPTorrentsSettings>
    {
        public override DownloadProtocol Protocol { get { return DownloadProtocol.Torrent; } }
        public override Boolean SupportsSearch { get { return false; } }
        public override Int32 PageSize { get { return 0; } }

        public IPTorrents(IHttpClient httpClient, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, configService, parsingService, logger)
        {

        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new IPTorrentsRequestGenerator();
        }

        public override IParseIndexerResponse GetParser()
        {
            throw new NotImplementedException();
        }
    }
}