using System;
using System.Linq;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;
using FluentValidation.Results;

namespace NzbDrone.Core.Indexers.KickassTorrents
{
    public class KickassTorrents : RssIndexerBase<KickassTorrentsSettings>
    {
        public override DownloadProtocol Protocol { get { return DownloadProtocol.Torrent; } }
        public override Int32 PageSize { get { return 25; } }

        public KickassTorrents(IHttpClient httpClient, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, configService, parsingService, logger)
        {

        }

         public override IIndexerRequestGenerator GetRequestGenerator()
         {
             return new KickassTorrentsRequestGenerator() { Settings = Settings, PageSize = PageSize };
         }

         public override IParseIndexerResponse GetParser()
         {
             throw new NotImplementedException();
         }
    }
}