using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security;
using System.Threading.Tasks;

namespace Bitwarden.AutoType.Desktop.Services;

public sealed record OperationFailureInfo(string Summary, string Detail);

public static class OperationFailureFormatter
{
    public static OperationFailureInfo Format(string fallbackSummary, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackSummary);
        ArgumentNullException.ThrowIfNull(exception);

        var rootException = GetRootException(exception);

        if (rootException is InvalidOperationException invalidOperationException
            && invalidOperationException.Message.Contains("invalidated", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationFailureInfo(
                "Re-authorization required",
                "Bitwarden account security settings changed. Re-authorize this device and try again.");
        }

        if (rootException.Message.Contains("Not configured", StringComparison.OrdinalIgnoreCase))
        {
            return new OperationFailureInfo(
                "Settings incomplete",
                "Complete account settings and authorization before trying again.");
        }

        if (rootException is HttpRequestException
            || rootException is TaskCanceledException
            || rootException is TimeoutException)
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The Bitwarden server could not be reached. Check your network connection, server URL, and SSL settings, then try again.");
        }

        if (rootException is OperationCanceledException)
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The operation was canceled before it finished. Try again if you still need to complete it.");
        }

        if (rootException is CryptographicException)
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The password is incorrect or the encrypted file is corrupted.");
        }

        if (rootException is DirectoryNotFoundException)
        {
            return new OperationFailureInfo(fallbackSummary, rootException.Message);
        }

        if (rootException is UnauthorizedAccessException || rootException is SecurityException)
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The app could not access the selected file or folder. Check the path and Windows permissions, then try again.");
        }

        if (rootException is IOException)
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The file operation could not be completed. Check whether the file or folder is in use, then try again.");
        }

        if (rootException is ArgumentNullException argumentNullException
            && (argumentNullException.ParamName == "accessToken"
                || argumentNullException.ParamName == "revisonDate"
                || argumentNullException.ParamName == "syncResponse"
                || argumentNullException.ParamName == "_syncResponse"))
        {
            return new OperationFailureInfo(
                fallbackSummary,
                "The Bitwarden server returned an unexpected or incomplete response. Try again in a moment.");
        }

        return new OperationFailureInfo(fallbackSummary, rootException.Message);
    }

    private static Exception GetRootException(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }
}