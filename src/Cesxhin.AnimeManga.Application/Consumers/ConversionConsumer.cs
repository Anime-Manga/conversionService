﻿using Cesxhin.AnimeManga.Modules.Exceptions;
using Cesxhin.AnimeManga.Modules.Generic;
using Cesxhin.AnimeManga.Modules.NlogManager;
using Cesxhin.AnimeManga.Domain.DTO;
using FFMpegCore;
using FFMpegCore.Enums;
using MassTransit;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cesxhin.AnimeManga.Application.Consumers
{
    public class ConversionConsumer : IConsumer<ConversionDTO>
    {
        //nlog
        private readonly NLogConsole _logger = new(LogManager.GetCurrentClassLogger());

        //temp
        private string pathTemp = Environment.GetEnvironmentVariable("PATH_TEMP") ?? "D:\\TestVideo\\temp";

        //env
        private readonly string MAX_THREAD = Environment.GetEnvironmentVariable("MAX_THREAD") ?? "2";
        public Task Consume(ConsumeContext<ConversionDTO> context)
        {
            try
            {
                var message = context.Message;

                Api<EpisodeDTO> episodeApi = new();
                Api<EpisodeRegisterDTO> episodeRegisterApi = new();

                EpisodeDTO episode = null;
                EpisodeRegisterDTO episodeRegister = null;
                //episode
                try
                {
                    episode = episodeApi.GetOne($"/episode/id/{message.ID}").GetAwaiter().GetResult();
                }
                catch (ApiNotFoundException ex)
                {
                    _logger.Error($"Not found episodeRegister, details error: {ex.Message}");
                }
                catch (ApiGenericException ex)
                {
                    _logger.Fatal($"Impossible error generic get episodeRegister, details error: {ex.Message}");

                }

                //episodeRegister
                try
                {
                    episodeRegister = episodeRegisterApi.GetOne($"/episode/register/episodeid/{episode.ID}").GetAwaiter().GetResult();
                }
                catch (ApiNotFoundException ex)
                {
                    _logger.Error($"Not found episodeRegister, details error: {ex.Message}");
                }
                catch (ApiGenericException ex)
                {
                    _logger.Fatal($"Impossible error generic get episodeRegister, details error: {ex.Message}");
                }

                //check
                if (episode == null)
                {
                    _logger.Fatal($"Get episode ID: {message.ID} not exitis");
                    return null;
                }


                _logger.Info($"Start conversion episode ID: {message.ID}");

                var fileTemp = $"{pathTemp}/joined-{Path.GetFileName(message.FilePath)}.ts";

                if (!File.Exists(fileTemp))
                {
                    //read all bytes
                    List<byte[]> buffer = new();
                    foreach (var path in message.Paths)
                    {
                        buffer.Add(File.ReadAllBytes(path));
                    }

                    //join bytes
                    using (var file = new FileStream(fileTemp, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        foreach (var data in buffer)
                            file.Write(data);
                    }

                    //destroy
                    buffer.Clear();

                    //delete old files
                    foreach (var path in message.Paths)
                        File.Delete(path);
                }

                //send status api
                episode.StateDownload = "conversioning";
                episode.PercentualDownload = 0;
                SendStatusDownloadAPIAsync(episode, episodeApi);

                //convert ts to mp4
                var tempMp4 = $"{pathTemp}/{Path.GetFileName(message.FilePath)}";

                var mediaInfo = FFProbe.Analyse(fileTemp);

                try
                {
                    var process = FFMpegArguments
                        .FromFileInput(fileTemp)
                        .OutputToFile(tempMp4, true, options => options
                            .UsingThreads(int.Parse(MAX_THREAD))
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithAudioCodec(AudioCodec.Aac)
                            .WithFastStart())
                        .NotifyOnError((outLine) =>
                        {
                            if (outLine != null)
                            {
                                if (outLine.Contains("frame="))
                                {
                                    if (DateTime.Now.Second % 5 == 0)
                                    {
                                        var lastFrame = outLine.Split("fps")[0].Split("=")[1].Trim();
                                        var percentual = Math.Round(decimal.Parse(lastFrame) / ((decimal)mediaInfo.Duration.TotalSeconds * (decimal)mediaInfo.VideoStreams[0].FrameRate) * 100);
                                        episode.PercentualDownload = (int)percentual;

                                        _logger.Debug($"episode ID: {episode.ID} percentual: {episode.PercentualDownload}");
                                        SendStatusDownloadAPIAsync(episode, episodeApi);
                                    }
                                }
                            }
                        })
                        .ProcessSynchronously();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Impossible conversion ID: {episode.ID}, details: {ex}");
                    episode.StateDownload = "failed";
                    SendStatusDownloadAPIAsync(episode, episodeApi);
                    return Task.CompletedTask;
                }

                if (episode.StateDownload == "failed")
                    return Task.CompletedTask;

                string pathPublic = Path.GetDirectoryName(message.FilePath);
                
                if(!Directory.Exists(pathPublic))
                    Directory.CreateDirectory(pathPublic);

                File.Move(tempMp4, message.FilePath, true);

                //delete old file
                File.Delete(fileTemp);



                //get hash and update
                _logger.Info($"start calculate hash of episode id: {episode.ID}");
                string hash = Hash.GetHash(episodeRegister.EpisodePath);
                _logger.Info($"end calculate hash of episode id: {episode.ID}");

                episodeRegister.EpisodeHash = hash;

                try
                {
                    episodeRegisterApi.PutOne("/episode/register", episodeRegister).GetAwaiter().GetResult();
                }
                catch (ApiNotFoundException ex)
                {
                    _logger.Error($"Not found episodeRegister id: {episodeRegister.EpisodeId}, details error: {ex.Message}");
                }
                catch (ApiGenericException ex)
                {
                    _logger.Fatal($"Error generic put episodeRegister, details error: {ex.Message}");
                }

                //send status api
                episode.StateDownload = "completed";
                episode.PercentualDownload = 100;
                SendStatusDownloadAPIAsync(episode, episodeApi);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generic, details {ex.Message}");
            }

            return null;
        }

        private void SendStatusDownloadAPIAsync(EpisodeDTO episode, Api<EpisodeDTO> episodeApi)
        {
            try
            {
                episodeApi.PutOne("/video/statusDownload", episode).GetAwaiter().GetResult();
            }
            catch (ApiNotFoundException ex)
            {
                _logger.Error($"Not found episode id: {episode.ID}, details: {ex.Message}");
            }
            catch (ApiGenericException ex)
            {
                _logger.Error($"Error generic api, details: {ex.Message}");
            }
        }
    }
}
