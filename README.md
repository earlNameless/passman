passman
=======

Password manager with minimal code.  Goals:
1) Provide a fairly easy to use safe password manager.
2) Minimize time between getting source and being comfortable that this is only a password manager.

To achieve #1:
A) Storage has to be encrypted with algorithm that takes forever to decrypt brute force.
B) Storage should be easily relocatable.
C) Should be easy to add/delete/get authorization information.
D) Authorization information will go on clip board.
E) Check for known clip board managers and give warnings.

To achieve #2, there are several things done:
A) All code is in one file.  File structure should be easy to follow.
B) Comments are minimized, code must be clear enough as is without comments.
C) Minimum set of dependencies (including .NET assemblies).
D) Simple build scripts.
E) Little error checking, if things fail, rely on exceptions.



