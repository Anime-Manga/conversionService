﻿using Cesxhin.AnimeManga.Application.Interfaces.Repositories;
using Cesxhin.AnimeManga.Application.Interfaces.Services;
using Cesxhin.AnimeManga.Application.NlogManager;
using Cesxhin.AnimeManga.Domain.DTO;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cesxhin.AnimeManga.Application.Services
{
    public class DescriptionVideoService : IDescriptionVideoService
    {
        //log
        private readonly NLogConsole _logger = new(LogManager.GetCurrentClassLogger());

        //interfaces
        private readonly IDescriptionRepository _descriptionRepository;
        private readonly IEpisodeRepository _episodeRepository;
        private readonly IEpisodeRegisterRepository _episodeRegisterRepository;

        public DescriptionVideoService(IDescriptionRepository descriptionRepository, IEpisodeRepository episodeRepository, IEpisodeRegisterRepository episodeRegisterRepository)
        {
            _descriptionRepository = descriptionRepository;
            _episodeRepository = episodeRepository;
            _episodeRegisterRepository = episodeRegisterRepository;
        }
        public async Task<string> DeleteNameByIdAsync(string nameCfg, string name)
        {
            //check all finish downloaded

            //get anime
            var anime = await _descriptionRepository.GetNameByNameAsync(nameCfg, name);

            if (anime == null)
                return null;

            //get episodes
            var episodes = await _episodeRepository.GetObjectsByNameAsync(name);

            foreach (var episode in episodes)
            {
                if (!(episode.StateDownload == "completed" || episode.StateDownload == null))
                    return "-1";
            }

            var rs = await _descriptionRepository.DeleteNameAsync(nameCfg, name);
            var rsEpisode = await _episodeRepository.DeleteByNameAsync(name);

            if (rs <= 0 || rsEpisode <= 0)
                return null;

            return name;
        }

        public async Task<IEnumerable<JObject>> GetMostNameByNameAsync(string nameCfg, string name)
        {
            return await _descriptionRepository.GetMostNameByNameAsync(nameCfg, name);
        }

        public async Task<IEnumerable<JObject>> GetNameAllAsync(string nameCfg)
        {
            return await _descriptionRepository.GetNameAllAsync(nameCfg);
        }

        public async Task<IEnumerable<GenericVideoDTO>> GetNameAllWithAllAsync(string nameCfg)
        {
            List<GenericVideoDTO> listGenericDTO = new();

            List<EpisodeDTO> listEpisodeDTO = new();
            List<EpisodeRegisterDTO> listEpisodeRegisterDTO = new();

            var listDescriptions = await _descriptionRepository.GetNameAllAsync(nameCfg);
            if (listDescriptions == null)
                return null;

            //anime
            foreach (var description in listDescriptions)
            {
                var episodes = await _episodeRepository.GetObjectsByNameAsync(description.GetValue("name_id").ToString());

                //episodes
                foreach (var episode in episodes)
                {
                    var episodesRegisters = await _episodeRegisterRepository.GetObjectsRegisterByObjectId(episode.ID);

                    //get first episodeRegister
                    foreach (var episodeRegister in episodesRegisters)
                        listEpisodeRegisterDTO.Add(EpisodeRegisterDTO.EpisodeRegisterToEpisodeRegisterDTO(episodeRegister));

                    listEpisodeDTO.Add(EpisodeDTO.EpisodeToEpisodeDTO(episode));
                }

                var objectAll = JObject.FromObject(new
                {
                    description,
                    listEpisodeDTO,
                    listEpisodeRegisterDTO
                });

                listGenericDTO.Add(new GenericVideoDTO()
                {
                    Video = description.ToString(),
                    Episodes = listEpisodeDTO,
                    EpisodesRegister = listEpisodeRegisterDTO
                });

                //reset
                listEpisodeDTO = new();
                listEpisodeRegisterDTO = new();
            }

            return listGenericDTO;
        }

        public async Task<JObject> GetNameByNameAsync(string nameCfg, string name)
        {
            return await _descriptionRepository.GetNameByNameAsync(nameCfg, name);
        }

        public async Task<JObject> InsertNameAsync(string nameCfg, JObject description)
        {
            if(description.ContainsKey("name_id"))
            {
                var find = await _descriptionRepository.GetNameByNameAsync(nameCfg, description.GetValue("name_id").ToString());

                if (find == null)
                    return await _descriptionRepository.InsertNameAsync(nameCfg, description);
                else
                    return null;
            }else
            {
                _logger.Error("Not found field 'name_id'");
                throw new Exception();
            }
        }
    }
}