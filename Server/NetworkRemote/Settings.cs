using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Net;
using SharpTools;

namespace NetworkRemote
{
    /// <summary>
    /// Holds Settings for NetworkRemote program
    /// </summary>
    class Settings
    {
        /// <summary>
        /// Security constant: Minimum lengts of secrets
        /// </summary>
        public const int MinimumSecretLength = 64;

        /// <summary>
        /// IP address to bind to
        /// </summary>
        public readonly IPAddress BindAddress = IPAddress.Any;

        /// <summary>
        /// TCP port to bind to
        /// </summary>
        public readonly ushort BindPort = 10545;

        /// <summary>
        /// Hello String. Static value sent by client to server to start challenge-response.
        /// </summary>
        public readonly string HelloString = null;

        /// <summary>
        /// Set of allowed API Keys. They are used as a secret value in the challenge-response. Dictionary value is client name.
        /// </summary>
        public readonly Dictionary<string, string> AllowedKeys = new Dictionary<string, string>();

        /// <summary>
        /// List of allowed commands. Key is command name, value is exe + args (ProcessStartInfo)
        /// </summary>
        public readonly Dictionary<string, ProcessStartInfo> Commands = new Dictionary<string, ProcessStartInfo>();

        /// <summary>
        /// Initialize settings from default INI file (CurrentAssembly.ini inside same folder of running Executable)
        /// </summary>
        /// <exception cref="IOException">If failed to read the file</exception>
        /// <exception cref="InvalidDataException">If the file contains invalid settings</exception>
        public static Settings FromDefaultFile()
        {
            return new Settings(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(Settings).Namespace + ".ini"));
        }

        /// <summary>
        /// Initialize settings from INI file
        /// </summary>
        /// <param name="filePath">INI file</param>
        /// <exception cref="IOException">If failed to read the file</exception>
        /// <exception cref="InvalidDataException">If the file contains invalid settings</exception>
        public Settings(string filePath)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section in INIFile.ParseFile(filePath))
            {
                foreach (KeyValuePair<string, string> setting in section.Value)
                {
                    switch (section.Key)
                    {
                        case "server":
                            switch (setting.Key)
                            {
                                case "bindaddress":
                                    IPAddress bindAddress;
                                    if (IPAddress.TryParse(setting.Value, out bindAddress))
                                        BindAddress = bindAddress;
                                    else
                                        throw new InvalidDataException("Invalid Bind Address: " + setting.Value);
                                    break;
                                case "bindport":
                                    ushort bindPort;
                                    if (ushort.TryParse(setting.Value, out bindPort) && bindPort > 0)
                                        BindPort = bindPort;
                                    else
                                        throw new InvalidDataException("Invalid Bind Port: " + setting.Value);
                                    break;
                                case "hellostring":
                                    if (setting.Value.Length >= MinimumSecretLength)
                                        HelloString = setting.Value;
                                    else
                                        throw new InvalidDataException("Hello String is too short (must be >= " + MinimumSecretLength + " chars): " + setting.Value);
                                    break;
                                default:
                                    throw new InvalidDataException("Unknown Server setting: " + setting.Key);
                            }
                            break;
                        case "clientkeys":
                            if (setting.Value.Length >= MinimumSecretLength)
                                AllowedKeys[setting.Value] = setting.Key;
                            else
                                throw new InvalidDataException("Client Key is too short (must be >= " + MinimumSecretLength + " chars): " + setting.Value);
                            break;
                        case "commands":
                            string[] commandArgs = setting.Value.Split('|');
                            if (commandArgs.Length > 0 && commandArgs.Length <= 2)
                            {
                                string exePath = commandArgs[0];
                                if (File.Exists(exePath))
                                {
                                    ProcessStartInfo pStartInfo = new ProcessStartInfo(exePath);
                                    if (commandArgs.Length > 1)
                                        pStartInfo.Arguments = commandArgs[1];
                                    pStartInfo.CreateNoWindow = true;
                                    Commands[setting.Key] = pStartInfo;
                                }
                                else throw new InvalidDataException("Executable not found: " + exePath);
                            }
                            else throw new InvalidDataException("Invalid command, expected format: X:\\path\\to\\command.exe|argument1 argument2: " + setting.Value);
                            break;
                        default:
                            throw new InvalidDataException("Unknown Settings section: " + section.Key);
                    }
                }
            }
        }
    }
}
