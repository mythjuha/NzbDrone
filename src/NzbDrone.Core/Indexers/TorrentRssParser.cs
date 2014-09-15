using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public class TorrentRssParser : RssParser
    {
        public Boolean ParseSeedersFromDescription { get; set; }

        public TorrentRssParser()
        {

        }

        protected override ReleaseInfo CreateNewReleaseInfo()
        {
            return new TorrentInfo();
        }

        protected override ReleaseInfo ProcessItem(XElement item, ReleaseInfo releaseInfo)
        {
            var result = base.ProcessItem(item, releaseInfo) as TorrentInfo;

            result.InfoHash = GetInfoHash(item);
            result.MagnetUrl = GetMagnetUrl(item);
            result.Seeds = GetSeeders(item);
            result.Peers = GetPeers(item);

            return result;
        }

        protected virtual String GetInfoHash(XElement item)
        {
            return null;
        }

        protected virtual String GetMagnetUrl(XElement item)
        {
            return null;
        }

        protected virtual Int32? GetSeeders(XElement item)
        {
            if (ParseSeedersFromDescription)
            {
                // TODO: Implement. Need a better property name too.
            }

            return null;
        }

        protected virtual Int32? GetPeers(XElement item)
        {
            if (ParseSeedersFromDescription)
            {
                // TODO 
            }

            return null;
        }
    }
}
