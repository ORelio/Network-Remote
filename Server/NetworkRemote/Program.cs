using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using SharpTools;
using System.Diagnostics;
using System.IO;

namespace NetworkRemote
{
    /// <summary>
    /// Network Remote Control - Like a TV remote, but for a PC, from the network - By ORelio (c) 2024
    /// "A simple remote-control service to run a predefined set of commands from the network"
    /// Access control is done using a simple challenge/response system:
    ///  1) Client connects to the server and sends a static "client hello" string to prove they know the protocol
    ///  2) Server sends a challenge (64char string) and computes the exhaustive list of valid responses for this challenge
    ///  3) Client chooses a command and computes SHA256(challenge + apikey + lowercase name of command), then send it back to server
    ///  4) If the response matches a valid command, the server runs it and replies "OK" before closing the socket
    /// This protocol should:
    ///  a) not leak anything if a random person stumbles upon the service (need to capture an actual exchange to retrieve "client hello")
    ///  b) provide a reasonably secure authentication mechanism (SHA256 + 10-second delay + 64 chars challenge + 64 chars API keys = Hard to break)
    ///  c) be robust against session hijacking/relay attacks (nothing happens after authentication since chosen command was part of the challenge)
    /// </summary>
    class Program
    {
        const string Version = "1.0.0";

        /// <summary>
        /// Main entry point of the program
        /// </summary>
        static void Main(string[] args)
        {
            LogWithTimestamp(typeof(Program).Namespace + " v" + Version + " - By ORelio");
            Settings settings = Settings.FromDefaultFile();
            LogWithTimestamp("Listening on socket: " + settings.BindAddress + ":" + settings.BindPort);
            TcpListener listener = new TcpListener(settings.BindAddress, settings.BindPort);
            listener.Start(10);
            while (true)
            {
                try
                {
                    // Wait for a new client
                    while (!listener.Pending()) { Thread.Sleep(100); }
                    Socket clientSocket = listener.AcceptSocket();
                    LogWithTimestamp("New client: " + clientSocket.RemoteEndPoint);

                    // Wait for client to request challenge
                    string clientHello = null;
                    AutoTimeout.Perform(() => { clientHello = clientSocket.ReadLine(); }, 10);
                    if (String.IsNullOrEmpty(clientHello) || clientHello != settings.HelloString)
                    {
                        LogWithTimestamp("Disconnecting client (ClientHello = " + clientHello + ")");
                        clientSocket.Close();
                        continue;
                    }
                    else LogWithTimestamp("Client hello: " + clientHello);

                    // Send challenge
                    string challenge = ChallengeResponse.GetChallenge();
                    LogWithTimestamp("Sending challenge: " + challenge);
                    clientSocket.WriteLine(challenge);

                    // Check response
                    string response = null;
                    string clientName = null;
                    string commandName = null;
                    AutoTimeout.Perform(() => { response = clientSocket.ReadLine(); }, 10);
                    ProcessStartInfo chosenCommand = ChallengeResponse.CheckResponse(settings, challenge, response, ref clientName, ref commandName);
                    LogWithTimestamp("Got response: " + response);
                    if (chosenCommand != null)
                    {
                        LogWithTimestamp("Response is valid for client=" + clientName + ", command=" + commandName);
                        SocketExtensions.WriteLine(clientSocket, "OK");
                    }
                    else LogWithTimestamp("Response does not match any client/command combination");
                    clientSocket.Close();

                    // Run the selected command if any
                    if (chosenCommand != null)
                    {
                        try
                        {
                            LogWithTimestamp("Running: " + Path.GetFileName(chosenCommand.FileName) + " " + chosenCommand.Arguments);
                            Process.Start(chosenCommand);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                        }
                    }
                }
                catch (SocketException) { /* Connection lost */ }
                catch (IOException) { /* Connection lost */ }
            }
        }

        /// <summary>
        /// Output log to console with timestamp
        /// </summary>
        /// <param name="message">Message to print to console</param>
        private static void LogWithTimestamp(object message)
        {
            Console.WriteLine(String.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message));
        }
    }
}
