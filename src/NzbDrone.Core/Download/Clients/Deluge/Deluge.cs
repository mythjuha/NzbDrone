﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Validation;
using NLog;
using Omu.ValueInjecter;
using FluentValidation.Results;
using System.Net;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.Deluge
{
    public class Deluge : TorrentClientBase<DelugeSettings>
    {
        private readonly IDelugeProxy _proxy;

        public Deluge(IDelugeProxy proxy,
                      ITorrentFileInfoReader torrentFileInfoReader,
                      IHttpClient httpClient,
                      IConfigService configService,
                      IDiskProvider diskProvider,
                      IParsingService parsingService,
                      IRemotePathMappingService remotePathMappingService,
                      Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, parsingService, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        protected override String AddFromMagnetLink(RemoteEpisode remoteEpisode, String hash, String magnetLink)
        {
            var actualHash = _proxy.AddTorrentFromMagnet(magnetLink, Settings);

            if (!Settings.TvCategory.IsNullOrWhiteSpace())
            {
                _proxy.SetLabel(actualHash, Settings.TvCategory, Settings);
            }

            var isRecentEpisode = remoteEpisode.IsRecentEpisode();

            if (isRecentEpisode && Settings.RecentTvPriority == (int)DelugePriority.First ||
                !isRecentEpisode && Settings.OlderTvPriority == (int)DelugePriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(actualHash, Settings);
            }

            return actualHash.ToUpper();
        }

        protected override String AddFromTorrentFile(RemoteEpisode remoteEpisode, String hash, String filename, Byte[] fileContent)
        {
            var actualHash = _proxy.AddTorrentFromFile(filename, fileContent, Settings);

            if (!Settings.TvCategory.IsNullOrWhiteSpace())
            {
                _proxy.SetLabel(actualHash, Settings.TvCategory, Settings);
            }

            var isRecentEpisode = remoteEpisode.IsRecentEpisode();

            if (isRecentEpisode && Settings.RecentTvPriority == (int)DelugePriority.First ||
                !isRecentEpisode && Settings.OlderTvPriority == (int)DelugePriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(actualHash, Settings);
            }

            return actualHash.ToUpper();
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            IEnumerable<DelugeTorrent> torrents;

            try
            {
                if (!Settings.TvCategory.IsNullOrWhiteSpace())
                {
                    torrents = _proxy.GetTorrentsByLabel(Settings.TvCategory, Settings);
                }
                else
                {
                    torrents = _proxy.GetTorrents(Settings);
                }
            }
            catch (DownloadClientException ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return Enumerable.Empty<DownloadClientItem>();
            }

            var items = new List<DownloadClientItem>();

            foreach (var torrent in torrents)
            {
                var remoteEpisode = GetRemoteEpisode(torrent.Name);
                if (remoteEpisode == null || remoteEpisode.Series == null) continue;

                var item = new DownloadClientItem();
                item.DownloadClientId = torrent.Hash.ToUpper();
                item.Title = torrent.Name;
                item.RemoteEpisode = remoteEpisode;
                item.Category = Settings.TvCategory;

                item.DownloadClient = Definition.Name;
                item.DownloadTime = TimeSpan.FromSeconds(torrent.SecondsDownloading);

                item.OutputPath = Path.Combine(torrent.DownloadPath, torrent.Name);
                item.RemainingSize = torrent.Size - torrent.BytesDownloaded;
                item.RemainingTime = TimeSpan.FromSeconds(torrent.Eta);
                item.TotalSize = torrent.Size;

                if (torrent.State == DelugeTorrentStatus.Error)
                {
                    item.Status = DownloadItemStatus.Failed;
                }
                else if (torrent.IsFinished && torrent.State != DelugeTorrentStatus.Checking)
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (torrent.State == DelugeTorrentStatus.Queued)
                {
                    item.Status = DownloadItemStatus.Queued;
                }
                else if (torrent.State == DelugeTorrentStatus.Paused)
                {
                    item.Status = DownloadItemStatus.Paused;
                }
                else
                {
                    item.Status = DownloadItemStatus.Downloading;
                }

                item.IsReadOnly = torrent.State != DelugeTorrentStatus.Paused;

                items.Add(item);
            }

            return items;
        }

        public override void RemoveItem(String hash)
        {
            _proxy.RemoveTorrent(hash.ToLower(), false, Settings);
        }

        public override String RetryDownload(String hash)
        {
            throw new NotSupportedException();
        }

        public override DownloadClientStatus GetStatus()
        {
            var config = _proxy.GetConfig(Settings);

            var destDir = config.GetValueOrDefault("download_location") as string;

            if (config.GetValueOrDefault("move_completed", false).ToString() == "True")
            {
                destDir = config.GetValueOrDefault("move_completed_path") as string;
            }

            var status = new DownloadClientStatus
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost"
            };

            if (destDir != null)
            {
                status.OutputRootFolders = new List<string> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, destDir) };
            }
            
            return status;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.Any())
                return;
            failures.AddIfNotNull(TestCategory());
            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersion(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return new NzbDroneValidationFailure("Password", "Authentication failed");
            }
            catch (WebException ex)
            {
                _logger.ErrorException(ex.Message, ex);
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    return new NzbDroneValidationFailure("Host", "Unable to connect")
                    {
                        DetailedDescription = "Please verify the hostname and port."
                    };
                }
                else if (ex.Status == WebExceptionStatus.ConnectionClosed)
                {
                    return new NzbDroneValidationFailure("UseSsl", "Verify SSL settings")
                    {
                        DetailedDescription = "Please verify your SSL configuration on both Deluge and NzbDrone."
                    };
                }
                else if (ex.Status == WebExceptionStatus.SecureChannelFailure)
                {
                    return new NzbDroneValidationFailure("UseSsl", "Unable to connect through SSL")
                    {
                        DetailedDescription = "Drone is unable to connect to Deluge using SSL. This problem could be computer related. Please try to configure both drone and Deluge to not use SSL."
                    };
                }
                else
                {
                    return new NzbDroneValidationFailure(String.Empty, "Unknown exception: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return new NzbDroneValidationFailure(String.Empty, "Unknown exception: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            if (Settings.TvCategory.IsNullOrWhiteSpace())
            {
                return null;
            }
            
            var enabledPlugins = _proxy.GetEnabledPlugins(Settings);

            if (!enabledPlugins.Contains("Label"))
            {
                return new NzbDroneValidationFailure("TvCategory", "Label plugin not activated")
                {
                    DetailedDescription = "You must have the Label plugin enabled in Deluge to use categories."
                };
            }

            var labels = _proxy.GetAvailableLabels(Settings);

            if (!labels.Contains(Settings.TvCategory))
            {
                _proxy.AddLabel(Settings.TvCategory, Settings);
                labels = _proxy.GetAvailableLabels(Settings);

                if (!labels.Contains(Settings.TvCategory))
                {
                    return new NzbDroneValidationFailure("TvCategory", "Configuration of label failed")
                    {
                        DetailedDescription = "NzbDrone as unable to add the label to Deluge."
                    };
                }
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return new NzbDroneValidationFailure(String.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
