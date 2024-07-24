﻿#if NETSTANDARD1_1_OR_GREATER
#define USE_RUNTIME_INFO
#endif

using System;
using System.Diagnostics;
using DiscordRPC.Logging;
#if USE_RUNTIME_INFO
using System.Runtime.InteropServices;
#endif

namespace DiscordRPC.Registry;

internal class UriSchemeRegister(
    ILogger logger,
    string applicationID,
    string steamAppID = null,
    string executable = null)
{
    /// <summary>
    ///     The ID of the Discord App to register
    /// </summary>
    public string ApplicationID { get; set; } = applicationID.Trim();

    /// <summary>
    ///     Optional Steam App ID to register. If given a value, then the game will launch through steam instead of Discord.
    /// </summary>
    public string SteamAppID { get; set; } = steamAppID?.Trim();

    /// <summary>
    ///     Is this register using steam?
    /// </summary>
    public bool UsingSteamApp => !string.IsNullOrEmpty(SteamAppID) && SteamAppID != "";

    /// <summary>
    ///     The full executable path of the application.
    /// </summary>
    public string ExecutablePath { get; set; } = executable ?? GetApplicationLocation();

    /// <summary>
    ///     Registers the URI scheme, using the correct creator for the correct platform
    /// </summary>
    public bool RegisterUriScheme()
    {
        //Get the creator
        IUriSchemeCreator creator;
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32Windows:
            case PlatformID.Win32S:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
                logger.Trace("Creating Windows Scheme Creator");
                creator = new WindowsUriSchemeCreator(logger);
                break;

            case PlatformID.Unix:
#if USE_RUNTIME_INFO
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        _logger.Trace("Creating MacOSX Scheme Creator");
                        creator = new MacUriSchemeCreator(_logger);
                    }
                    else
                    {
#endif
                logger.Trace("Creating Unix Scheme Creator");
                creator = new UnixUriSchemeCreator(logger);
#if USE_RUNTIME_INFO
                    }
#endif
                break;

#if !USE_RUNTIME_INFO
            case PlatformID.MacOSX:
                logger.Trace("Creating MacOSX Scheme Creator");
                creator = new MacUriSchemeCreator(logger);
                break;
#endif

            default:
                logger.Error("Unknown Platform: {0}", Environment.OSVersion.Platform);
                throw new PlatformNotSupportedException("Platform does not support registration.");
        }

        //Register the app
        if (creator.RegisterUriScheme(this))
        {
            logger.Info("URI scheme registered.");
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets the FileName for the currently executing application
    /// </summary>
    /// <returns></returns>
    private static string GetApplicationLocation()
    {
        var processModule = Process.GetCurrentProcess().MainModule;
        return processModule != null ? processModule.FileName : "";
    }
}