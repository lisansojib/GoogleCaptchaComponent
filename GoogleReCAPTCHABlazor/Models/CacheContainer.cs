using System.Collections.Generic;
using GoogleReCaptchaBlazor.Configuration;

namespace GoogleReCaptchaBlazor.Models;

internal class CacheContainer
{
    public HashSet<string> LoadedScripts { get;  }
    public CaptchaConfiguration.Version CurrentVersion { get; set; }

    public CacheContainer()
    {
        LoadedScripts = new();
    }
}