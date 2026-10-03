using System;
using System.IO;
using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;

namespace Ombi.Notifications
{
    /// <summary>
    /// Identifies failures that are reasonable to retry for queued email notifications.
    /// Permanent SMTP responses (5xx), authentication/configuration failures and invalid
    /// message data are intentionally not retried.
    /// </summary>
    public static class EmailRetryPolicy
    {
        public static bool IsTransient(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is SmtpCommandException commandException)
                {
                    var statusCode = (int)commandException.StatusCode;
                    return statusCode >= 400 && statusCode < 500;
                }

                if (current is SmtpProtocolException ||
                    current is ServiceNotConnectedException ||
                    current is IOException ||
                    current is TimeoutException)
                {
                    return true;
                }

                if (current is SocketException socketException && IsTransientSocketError(socketException.SocketErrorCode))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTransientSocketError(SocketError error)
        {
            // GenericEmailProvider already has its own DNS-resolution retry loop, so do not
            // multiply those retries again here after that policy has been exhausted.
            return error == SocketError.ConnectionAborted ||
                   error == SocketError.ConnectionRefused ||
                   error == SocketError.ConnectionReset ||
                   error == SocketError.HostDown ||
                   error == SocketError.HostUnreachable ||
                   error == SocketError.NetworkDown ||
                   error == SocketError.NetworkReset ||
                   error == SocketError.NetworkUnreachable ||
                   error == SocketError.Shutdown ||
                   error == SocketError.TimedOut;
        }
    }
}
