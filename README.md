passman
=======

Password manager with minimal code.  Goals:
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
