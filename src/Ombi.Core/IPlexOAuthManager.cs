using System;
using System.Threading.Tasks;
using Ombi.Api.External.MediaServers.Plex.Models;
using Ombi.Api.External.MediaServers.Plex.Models.OAuth;

namespace Ombi.Core.Authentication
{
    public interface IPlexOAuthManager
    {
        Task<OAuthContainer> CreatePin();
        Task<string> GetAccessTokenFromPollToken(string pollToken);
        Task<Uri> GetOAuthUrl(string pollToken, string websiteAddress = null);
        Task<Uri> GetWizardOAuthUrl(string pollToken, string websiteAddress);
        Task<PlexAccount> GetAccount(string accessToken);
    }
}