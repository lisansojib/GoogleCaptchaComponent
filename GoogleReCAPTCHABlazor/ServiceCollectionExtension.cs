using GoogleReCaptchaBlazor.Configuration;
using GoogleReCaptchaBlazor.Models;
using GoogleReCaptchaBlazor.Services;
using GoogleReCaptchaBlazor.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleReCaptchaBlazor;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// Add Needed reCaptcha services for blazor
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">Site key received in developer console</param>
    /// <returns></returns>
    public static IServiceCollection AddGoogleCaptcha(this IServiceCollection services,Action<CaptchaConfiguration> configuration)
    {
        services.Configure<CaptchaConfiguration>(configuration);

        services.AddScoped<CacheContainer>();

        services.AddScoped<IRecaptchaService, RecaptchaService>();

        return services;
    }

}