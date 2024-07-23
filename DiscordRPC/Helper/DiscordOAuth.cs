using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordRPC.Helper;

/// <summary>
///     Helper class for Discord OAuth2 authorization.
/// </summary>
public static class DiscordOAuth
{
    private const string ApiEndpoint = "https://discord.com/api/v10";
    private const string RedirectUri = "https://localhost";
    private const string OauthFilePath = "discordOauth.json";
    private static string _clientId = "";
    private static string _clientSecret = "";
    private static Dictionary<string, object> _oauthData;

    /// <summary>
    ///     Sets the client data for the Discord application.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    public static void SetClientData(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    ///     OAuth2 authorization code is now exchanged for the user's access token by making a POST request to the token URL
    ///     with the following parameters:
    ///     <para>grant_type - must be set to authorization_code</para>
    ///     redirect_uri - the redirect_uri associated with this authorization, usually from your authorization URL
    /// </summary>
    /// <param name="code">the OAuth2 authorization code from the querystring</param>
    /// <returns></returns>
    public static async Task<bool> ExchangeCodeAsync(string code)
    {
        using var client = new HttpClient();
        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", RedirectUri }
        };

        var requestContent = new FormUrlEncodedContent(data);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoint}/oauth2/token")
        {
            Content = requestContent
        };

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _oauthData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            SaveOAuthData(_oauthData, OauthFilePath);
            return true;
        }

        Console.WriteLine("Discord Network error (refresh): " + response.ReasonPhrase);
        return false;
    }

    /// <summary>
    ///     Saves the OAuth data to a file.
    /// </summary>
    /// <param name="newOAuthData">The OAuth data to save.</param>
    /// <param name="filePath">The file path where the data should be saved.</param>
    private static void SaveOAuthData(Dictionary<string, object> newOAuthData, string filePath)
    {
        var jsonData = JsonSerializer.Serialize(newOAuthData);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new Utf8JsonWriter(fileStream);
        writer.WriteRawValue(jsonData);
    }

    /// <summary>
    ///     Loads OAuth data from a specified file path.
    /// </summary>
    /// <param name="filePath">The file path from which to load the OAuth data.</param>
    /// <returns>
    ///     An <see cref="Option{Dictionary&lt;string, object&gt;}" /> indicating the success or failure of the operation.
    ///     Returns <see cref="Some{Dictionary&lt;string, object&gt;}" /> if the data is successfully loaded, otherwise returns
    ///     <see cref="None{Dictionary&lt;string, object&gt;}" />.
    /// </returns>
    public static Option<Dictionary<string, object>> LoadOAuthData(string filePath = OauthFilePath)
    {
        if (!File.Exists(filePath)) return Option<Dictionary<string, object>>.None();

        var jsonData = File.ReadAllText(filePath);
        var oauthData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);

        return oauthData != null
            ? Option<Dictionary<string, object>>.Some(oauthData)
            : Option<Dictionary<string, object>>.None();
    }

    /// <summary>
    ///     Tries to refresh the token if the refresh token is available.
    /// </summary>
    /// <returns>True if the token was successfully refreshed, otherwise false.</returns>
    public static async Task<bool> TryRefreshToken()
    {
        if (_oauthData == null)
        {
            Console.WriteLine("TryRefreshToken: No OAuth data available. Attempting to load from file.");
            var oauthDataOption = LoadOAuthData();
            if (oauthDataOption.IsNone)
            {
                Console.WriteLine("TryRefreshToken: No file exists or no OAuth data available.");
                return false;
            }

            _oauthData = oauthDataOption.Value;
        }

        object refreshTokenValue = null;
        var success = _oauthData?.TryGetValue("refresh_token", out refreshTokenValue) == true;
        if (!success || refreshTokenValue == null)
        {
            Console.WriteLine("TryRefreshToken: No refresh token available.");
            return false;
        }

        using var client = new HttpClient();
        var data = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshTokenValue.ToString() }
        };

        var requestContent = new FormUrlEncodedContent(data);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoint}/oauth2/token")
        {
            Content = requestContent
        };

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _oauthData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            SaveOAuthData(_oauthData, OauthFilePath);
            return true;
        }

        Console.WriteLine("Discord Network error (refresh): " + response.ReasonPhrase);
        return false;
    }
}