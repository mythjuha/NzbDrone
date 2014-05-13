using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using MonoTorrent;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NLog;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download
{
    public abstract class TorrentClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        protected readonly ITorrentFileInfoReader _torrentFileInfoReader;

        protected TorrentClientBase(ITorrentFileInfoReader torrentFileInfoReader,
                                    IHttpClient httpClient,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    IParsingService parsingService,
                                    IRemotePathMappingService remotePathMappingService,
                                    Logger logger)
            : base(configService, diskProvider, parsingService, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
            _torrentFileInfoReader = torrentFileInfoReader;
        }
        
        public override DownloadProtocol Protocol
        {
            get
            {
                return DownloadProtocol.Torrent;
            }
        }

        protected abstract String AddFromMagnetLink(RemoteEpisode remoteEpisode, String hash, String magnetLink);
        protected abstract String AddFromTorrentFile(RemoteEpisode remoteEpisode, String hash, String filename, Byte[] fileContent);

        public override String Download(RemoteEpisode remoteEpisode)
        {
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            String magnetUrl = null;
            String torrentUrl = null;
            
            if (remoteEpisode.Release.DownloadUrl.StartsWith("magnet:"))
            {
                magnetUrl = remoteEpisode.Release.DownloadUrl;
            }
            else
            {
                torrentUrl = remoteEpisode.Release.DownloadUrl;
            }

            if (torrentInfo != null && !torrentInfo.MagnetUrl.IsNullOrWhiteSpace())
            {
                magnetUrl = torrentInfo.MagnetUrl;
            }

            String hash = null;
            String actualHash = null;

            if (!magnetUrl.IsNullOrWhiteSpace())
            {
                try
                {
                    hash = new MagnetLink(magnetUrl).InfoHash.ToHex();
                }
                catch (FormatException ex)
                {
                    _logger.ErrorException(String.Format("Failed to parse magnetlink for episode '{0}': '{1}'",
                        remoteEpisode.Release.Title, torrentInfo.MagnetUrl), ex);
                }

                if (hash != null)
                {
                    actualHash = AddFromMagnetLink(remoteEpisode, hash, magnetUrl);
                }
            }
            
            if (hash == null && !torrentUrl.IsNullOrWhiteSpace())
            {
                Byte[] torrentFile = null;

                try
                {
                    torrentFile = _httpClient.Get(new HttpRequest(torrentUrl)).ResponseData;
                }
                catch (WebException ex)
                {
                    _logger.ErrorException(String.Format("Downloading torrentfile for episode '{0}' failed ({1})",
                        remoteEpisode.Release.Title, torrentUrl), ex);

                    throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading torrent failed", ex);
                }
                
                
                var filename = String.Format("{0}.torrent", FileNameBuilder.CleanFileName(remoteEpisode.Release.Title));

                hash = _torrentFileInfoReader.GetHashFromTorrentFile(torrentFile);

                actualHash = AddFromTorrentFile(remoteEpisode, hash, filename, torrentFile);
            }

            if (hash == null || actualHash == null)
            {
                throw new ReleaseDownloadException(remoteEpisode.Release, "Downloading torrent failed");
            }

            if (hash != actualHash)
            {
                _logger.Warn(
                    "{0} did not return the expected InfoHash for '{1}', NzbDrone could potential lose track of the download in progress.",
                    Definition.Implementation, remoteEpisode.Release.DownloadUrl);
            }

            return hash;
        }
    }
}
