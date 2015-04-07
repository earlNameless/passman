passman
=======

Password manager with minimal code.  Why? Because others have too much code for me to
trust them.

Goals
-----
 1. Provide a fairly easy to use safe password manager.
    * Storage has to be encrypted with algorithm that takes forever to decrypt brute force.
    * Storage should be easily relocatable.
    * Should be easy to add/delete/get authorization information.
    * Authorization information will go on clip board.
    * Check for known clip board managers and give warnings.
 2. Minimize time between getting source and being comfortable that this is only a password manager.
    * All code is in one file.  File structure should be easy to follow.
    * Comments are minimized, code must be clear enough as is without comments.
    * Minimum set of dependencies (including .NET assemblies).
    * Simple build scripts.
    * Little error checking, if things fail, rely on exceptions.

Things that will not be done
----------------------------
 * Delivery of binaries.
 * Installation.

Build requirements
------------------
Pick *one* of below:
 * Microsoft .NET Framework 4.0
    1. Install Microsoft .NET Framework 4.0 Full Profile
    2. Run `build_microsoft.bat`
 * Mono 2.8 on Windows
    1. Install Mono 2.8 or higher.
    2. Run `build_mono_windows.bat`
 * Linux
    1. Install Mono 2.8 or higher.
    2. Run `./build_mono_linux.sh`

In each case the binary created is cross-platform compatible.

Runtime requirements
--------------------
 * Windows, one of:
   1. Microsoft .NET Framework 4.0
   2. Mono 2.8 or higher
 * Linux, all:
   1. Mono 2.8 or higher
   2. [xclip](http://sourceforge.net/projects/xclip/)

Configure
---------
 1. Configure your own custom salt in passman.exe.config
    * This will be unique for you, does not have to be secret, just unique.  At least 8 characters in length.
    * If you lose it, you will *not* be able to decrypt the data.
 2. If you use a clipboard manager, add it to CheckApplications() method.

Other
-----
 * License: http://www.apache.org/licenses/LICENSE-2.0
 * Project Home: https://github.com/earlNameless/passman

