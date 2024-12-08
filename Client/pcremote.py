#!/usr/bin/env python3

import sys
import socket
import hashlib

from configparser import ConfigParser

_hello_string = None
_devices = {}
_commands = {}

config = ConfigParser()
config.read('pcremote.ini')
_hello_string = config.get('Protocol', 'HelloString')

if _hello_string is None or len(_hello_string) == 0:
    print('WARNING: No HelloString defined in config, please configure')
    _hello_string = None
else:
    for (device, details) in config.items('Devices'):
        device = device.lower().strip()
        if not device in _devices:
            details = details.split('|')
            if (len(details) == 2):
                api_key = details[1]
                ip_port = details[0].split(':')
                if (len(ip_port) == 2):
                    ip = ip_port[0]
                    port = int(ip_port[1])
                    _devices[device] = {
                        'ip': ip,
                        'port': port,
                        'apikey': api_key
                    }
                else:
                    print('WARNING: Skipping invalid IP:Port definition for "{}"'.format(device))
            else:
                print('WARNING: Skipping invalid Host|API-Key definition for "{}"'.format(device))
        else:
            print('WARNING: Duplicate device "{}", keeping the first one.'.format(device))
    #print('Loaded {} devices: {}'.format(len(_devices), ', '.join(_devices.keys())))
    for (alias, command) in config.items('Commands'):
        alias = alias.lower().strip()
        command = command.lower().strip()
        if not alias in _commands:
            _commands[alias] = command
        else:
            print('WARNING: Duplicate command "{}", keeping the first one.'.format(alias))
    #print('Loaded {} command aliases: {}'.format(len(_commands), ', '.join(_commands.keys())))

def send(device: str, command: str, timeout_seconds: int = 10) -> bool:
    '''
    Send a command to the specified device
    device: Name of the device to operate
    command: Name of command to send
    timeout_seconds: Network timeout in seconds (default: 10)
    returns TRUE if send succeeded
    '''
    if _hello_string is None:
        print('ERROR: send() called but no HelloString defined in config.')
        return False
    device = device.lower()
    command = command.lower()
    if device in _devices:
        ip = _devices[device]['ip']
        port = _devices[device]['port']
        api_key = _devices[device]['apikey']
        if command in _commands:
            command_id = _commands[command]
            print('Sending command {} to {}'.format(command, device))
            try:
                server = socket.socket()
                server.connect((ip, port))
                if timeout_seconds > 0:
                    server.settimeout(timeout_seconds)
                server.send((_hello_string + "\n").encode())
                challenge = server.recv(1024).decode().split("\n")[0]
                response = hashlib.sha256((challenge + api_key + command_id).encode()).hexdigest()
                server.send((response + "\n").encode())
                status = server.recv(128).decode()
                return status == 'OK'
            except Exception as ex:
                print('WARNING: Failed to send command: ' + type(ex).__name__)
        else:
            print('WARNING: Unknown command: ' + command)
    else:
        print('WARNING: Unknown device: ' + device)
    return False

if __name__ == '__main__':
    if len (sys.argv) == 3:
        send(sys.argv[1], sys.argv[2])
    else:
        print('{} <pc-name> <command-name>'.format(sys.argv[0]))
        sys.exit(2)
