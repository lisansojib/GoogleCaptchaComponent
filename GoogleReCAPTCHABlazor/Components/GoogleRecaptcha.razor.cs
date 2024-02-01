﻿using System.ComponentModel;
using GoogleReCaptchaBlazor.Configuration;
using GoogleReCaptchaBlazor.Events;
using GoogleReCaptchaBlazor.Exceptions;
using GoogleReCaptchaBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace GoogleReCaptchaBlazor.Components;


/// <summary>
/// Main google captcha component to use in razor files
/// </summary>
public partial class GoogleRecaptcha
{
    [Inject] public IJSRuntime Js { get; set; }
    [Inject] internal IOptions<CaptchaConfiguration> CaptchaConfiguration { get; set; }
    [Inject] internal CacheContainer CacheContainer { get; set; }

    /// <summary>
    /// Success captcha validation event
    /// </summary>
    [Parameter, EditorRequired]
    public EventCallback<CaptchaSuccessEventArgs> SuccessCallBack { get; set; }

    /// <summary>
    /// Specify the version to be used for this specific component.If null the default version will be used
    /// </summary>
    [Parameter] 
    public CaptchaConfiguration.Version? Version { get; set; }


    /// <summary>
    /// Specify the theme to be used for this specific component. If null the default theme will be used
    /// </summary>
    [Parameter] 
    public CaptchaConfiguration.Theme? Theme { get; set; }


    /// <summary>
    /// captcha validation Timeout event
    /// </summary>
    [Parameter, EditorRequired]
    public EventCallback<CaptchaTimeOutEventArgs> TimeOutCallBack { get; set; }

    /// <summary>
    ///  captcha validation error event
    /// </summary>
    [Parameter]
    public EventCallback<CaptchaServerSideValidationErrorEventArgs> ServerValidationErrorCallBack { get; set; }

    /// <summary>
    /// Handler for implementing server side validation
    /// </summary>
    [Parameter]
    public Func<ServerSideCaptchaValidationRequestModel, Task<ServerSideCaptchaValidationResultModel>> ServerSideValidationHandler { get; set; }

    /// <summary>
    /// Specified configuration in startup
    /// </summary>
    public CaptchaConfiguration CurrentConfiguration => CaptchaConfiguration.Value;

    /// <summary>
    /// Specified the default language for this specific component. If its not specified the default will be used.
    /// </summary>
    [Parameter]
    public CaptchaLanguages Language { get; set; }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {


        if (firstRender)
        {
            await InitializeScripts();
            await HandleRecaptchaCallBackFunctions();
        }



        //await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleRecaptchaCallBackFunctions()
    {
        try
        {
            

            if (Version == Configuration.CaptchaConfiguration.Version.V2)
                await Js.InvokeVoidAsync("render_recaptcha_v2", DotNetObjectReference.Create(this), "recaptcha_container",
                    CurrentConfiguration.V2SiteKey, Theme.ToString()?.ToLower(),Language.Language);
            else
                await Js.InvokeVoidAsync("render_recaptcha_v3", DotNetObjectReference.Create(this),
                    CurrentConfiguration.V3SiteKey, Theme.ToString()?.ToLower());
        }
        catch (Exception e)
        {
            throw new CaptchaLoadScriptException(
                "Invalid site key or wrong reCaptcha version. Make sure your site key is valid and is for proper version",
                e);
        }
    }

    private async Task InitializeScripts()
    {
        Version ??= CurrentConfiguration.DefaultVersion;
        Theme ??= CurrentConfiguration.DefaultTheme;
        Language ??= CurrentConfiguration.DefaultLanguage;

        if (Version.Value == Configuration.CaptchaConfiguration.Version.V2 &&
            string.IsNullOrEmpty(CurrentConfiguration.V2SiteKey))
            throw new CaptchaLoadScriptException("No site key is configured for V2");

        if (Version.Value == Configuration.CaptchaConfiguration.Version.V3
            && string.IsNullOrEmpty(CurrentConfiguration.V3SiteKey))
            throw new CaptchaLoadScriptException("No site key is configured for V3");

        CacheContainer.CurrentVersion = Version.Value;

        await LoadScript("_content/GoogleReCaptchaBlazor/Scripts/JsOfReCAPTCHA.js");

        if (Version == Configuration.CaptchaConfiguration.Version.V3)
            await LoadScript($"https://www.google.com/recaptcha/api.js?render={CurrentConfiguration.V3SiteKey}");

        else
            await LoadScript($"https://www.google.com/recaptcha/api.js");
    }

    private async Task LoadScript(string scriptPath)
    {
        if (CacheContainer.LoadedScripts.Contains(scriptPath))
            return;

        await Js.InvokeVoidAsync("loadScript", scriptPath);
        CacheContainer.LoadedScripts.Add(scriptPath);
    }

    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public virtual async Task CallbackOnSuccess(string response)
    {

        try
        {
            if (ServerSideValidationHandler is null)
                throw new CallBackDelegateException("There is no handler related to server validation");

            var serverSideValidationResult = await ServerSideValidationHandler(new ServerSideCaptchaValidationRequestModel(response));


            if (!serverSideValidationResult.IsSuccess)
            {
                if (!ServerValidationErrorCallBack.HasDelegate)
                    throw new CallBackDelegateException(
                        $"Server side reCaptcha validation is failed but no handler found for {nameof(ServerValidationErrorCallBack)}");

                await ServerValidationErrorCallBack.InvokeAsync(
                    new CaptchaServerSideValidationErrorEventArgs(serverSideValidationResult.ValidationMessage));
            }

            if (!SuccessCallBack.HasDelegate)
                throw new CallBackDelegateException(
                    $"no Callback handler found for {nameof(SuccessCallBack)}");

            await SuccessCallBack.InvokeAsync(new CaptchaSuccessEventArgs(response));
        }

        catch (Exception e) when(e is not CallBackDelegateException)
        {
            throw new CallBackDelegateException("Error invoking related server validation callback", e);
        }
    }

    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public virtual async Task CallbackOnExpired()
    {
        if (!TimeOutCallBack.HasDelegate)
            throw new CallBackDelegateException(
                $"no Callback handler found for {nameof(TimeOutCallBack)}");

        await InvokeAsync(StateHasChanged);

        await TimeOutCallBack.InvokeAsync(new CaptchaTimeOutEventArgs("captcha validation expired"));
    }

    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public virtual async Task CallBackError(string message)
    {
        if (!ServerValidationErrorCallBack.HasDelegate)
            throw new CallBackDelegateException(
                $"no Callback handler found for {nameof(ServerValidationErrorCallBack)}");

        await InvokeAsync(StateHasChanged);

        await ServerValidationErrorCallBack.InvokeAsync(new CaptchaServerSideValidationErrorEventArgs(message));
    }



}