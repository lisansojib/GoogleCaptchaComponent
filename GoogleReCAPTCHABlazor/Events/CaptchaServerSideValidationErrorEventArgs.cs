namespace GoogleReCaptchaBlazor.Events;

/// <summary>
/// Fires when there is an error in captcha validation
/// </summary>
public class CaptchaServerSideValidationErrorEventArgs:EventArgs
{
    public string ErrorMessage { get; }

    public CaptchaServerSideValidationErrorEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}