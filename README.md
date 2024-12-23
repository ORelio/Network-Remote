![Network Remote](Images/network-remote-logo.png)

_"Like a TV remote, but for a PC, from the network"_

A simple client/server program to remotely execute pre-configured commands on a Windows-based PC.

The project is made of two parts:
* A server program written in C# to run on PC
* A client script written in Python that works on multiple platforms

It serves the following goals:
* Easy to set-up: configure INI files and launch server, done
* Pre-configure a set of commands to avoid giving access to remote shell
* Use a simple handshake protocol that avoids bruteforce, relay and replay attacks

## Usage example

Commands set in server:

```ini
Play=C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine\wallpaper64.exe|-control play
Stop=C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine\wallpaper64.exe|-control stop
```

Aliases set in client:

```ini
WallpaperPlay=Play
WallpaperStop=Stop
```

Running command from client script:

```ini
./pcremote.py mycomputer wallpaperplay
./pcremote.py mycomputer wallpaperstop
```

See sample configuration files in Server and Client folders for more info.

## License

Network Remote is provided under
[CDDL-1.0](http://opensource.org/licenses/CDDL-1.0)
([Why?](http://qstuff.blogspot.fr/2007/04/why-cddl.html)).

Basically, you can use it or its source for any project, free or commercial, but if you improve it or fix issues,
the license requires you to contribute back by submitting a pull request with your improved version of the code.
Also, credit must be given to the original project, and license notices may not be removed from the code.
