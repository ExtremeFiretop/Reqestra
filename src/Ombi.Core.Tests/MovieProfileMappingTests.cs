using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.TheMovieDb.Models;
using Ombi.Mapping.Profiles;
using Ombi.TheMovieDbApi.Models;

namespace Ombi.Core.Tests
{
    [TestFixture]
    public class MovieProfileMappingTests
    {
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<MovieProfile>(),
                NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
        }

        [Test]
        public void SearchResult_Uses_Movie_Release_Date_When_Present()
        {
            var result = _mapper.Map<MovieDbSearchResult>(new SearchResult
            {
                release_date = "2026-10-01",
                first_air_date = "2026-09-01"
            });

            Assert.That(result.ReleaseDate, Is.EqualTo("2026-10-01"));
        }

        [Test]
        public void SearchResult_Uses_Tv_First_Air_Date_When_Movie_Date_Is_Missing()
        {
            var result = _mapper.Map<MovieDbSearchResult>(new SearchResult
            {
                first_air_date = "2026-11-15"
            });

            Assert.That(result.ReleaseDate, Is.EqualTo("2026-11-15"));
        }
    }
}
