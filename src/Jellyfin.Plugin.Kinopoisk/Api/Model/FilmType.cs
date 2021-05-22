using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Kinopoisk.Api.Model
{
    public enum FilmType
    {
        [EnumMember(Value = "UNKNOWN")]
        Unknown,

        [EnumMember(Value = "FILM")]
        Film,

        [EnumMember(Value = "TV_SHOW")]
        TvShow
    }
}