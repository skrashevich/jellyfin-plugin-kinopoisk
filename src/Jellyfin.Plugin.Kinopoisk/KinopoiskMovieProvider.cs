using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk
{
    public class KinopoiskMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        private readonly ILogger<KinopoiskMovieProvider> _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly KinopoiskApiProxy _apiProxy;

        public string Name => Utils.ProviderName;

        public KinopoiskMovieProvider(ILogger<KinopoiskMovieProvider> logger,
                                       IHttpClient httpClient,
                                       IJsonSerializer jsonSerializer)
        {
            if (logger is null)
            {
                throw new System.ArgumentNullException(nameof(logger));
            }

            if (httpClient is null)
            {
                throw new System.ArgumentNullException(nameof(httpClient));
            }

            if (jsonSerializer is null)
            {
                throw new System.ArgumentNullException(nameof(jsonSerializer));
            }

            this._logger = logger;
            this._httpClient = httpClient;
            this._jsonSerializer = jsonSerializer;
            this._apiProxy = new KinopoiskApiProxy(logger, httpClient, jsonSerializer);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                QueriedById = true,
                Provider = Utils.ProviderName,
                ResultLanguage = Utils.ProviderMetadataLanguage
            };

            if (!Utils.TryGetKinopoiskId(info, _logger, out var kinopoiskId))
                return result;

            var film = await _apiProxy.GetSingleFilm(kinopoiskId, cancellationToken);

            result.Item = film.ToMovie();
            if (result.Item != null)
                result.HasMetadata = true;

            cancellationToken.ThrowIfCancellationRequested();

            var staff = await _apiProxy.GetStaff(kinopoiskId, cancellationToken);

            foreach(var item in staff.ToPersonInfos())
                result.AddPerson(item);

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            if (Utils.TryGetKinopoiskId(searchInfo, _logger, out var kinopoiskId))
            {
                var film = await _apiProxy.GetSingleFilm(kinopoiskId, cancellationToken);
                var singleResult = new RemoteSearchResult() {
                        Name = film.Data.NameRu ?? film.Data.NameEn,
                        ImageUrl = film.Data.PosterUrl,
                        ProductionYear = Utils.GetFirstYear(film.Data.Year),
                        Overview = film.Data.Description,
                        SearchProviderName = Utils.ProviderName
                    };
                    singleResult.SetProviderId(Utils.ProviderId, Convert.ToString(film.Data.FilmId));
                return Enumerable.Repeat(singleResult, 1);
            }
            else
            {
                var searchResults = await _apiProxy.SearchByKeyword(searchInfo.Name, cancellationToken: cancellationToken);
                return searchResults.Films.Select(f => {
                    var res = new RemoteSearchResult() {
                        Name = f.NameRu ?? f.NameEn,
                        ImageUrl = f.PosterUrl,
                        ProductionYear = Utils.GetFirstYear(f.Year),
                        Overview = f.Description,
                        SearchProviderName = Utils.ProviderName
                    };
                    res.SetProviderId(Utils.ProviderId, Convert.ToString(f.FilmId));
                    return res;
                });
            }
        }
    }
}