using System;
using System.Linq;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Nyaa
{
    public class Nyaa : RssIndexerBase<NyaaSettings>
    {
         public override DownloadProtocol Protocol { get { return DownloadProtocol.Usenet; } }

         public Nyaa(IHttpClient httpClient, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, configService, parsingService, logger)
        {

        }

         public override IIndexerRequestGenerator GetRequestGenerator()
         {
             return new NyaaRequestGenerator() { Settings = Settings };
         }

         public override IParseIndexerResponse GetParser()
         {
             return new NyaaRssParser();
         }
    }
}