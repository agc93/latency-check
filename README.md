# LatencyCheck

> This should not be considered ready for use and is barely at the prototype stage.

LatencyCheck is a modular library/app combo for measuring the network latency of processes. LatencyCheck uses Windows' built-in connection tracking, reading basic connection latency stats and making them available through a vaguely sane API.

### Design

There's two projects at work here: `LatencyCheck` and `LatencyCheck.Service`.

`LatencyCheck` is the core library responsible for loading and parsing the connection statistics (through the Win32 `GetExtendedTcpTable` API) for a given process. It can be used as a self-contained timer (untested) or with an external wrapper/worker for convenience.

`LatencyCheck.Service` is an ASP.NET Core app designed for hosting as a Windows Service. While it exposes an **extremely** rudimentary HTTP API, the real magic happens in a worker service that runs on a (configurable) interval and gets the connection stats for a set of processes using `LatencyCheck`. These statistics are then published/made available through "update handlers" (name likely to be changed): simple types that get are provided the connection statistics for further processing.

Out of the box, the two update handlers available will publish connection stats as HWiNFO custom sensors and as Windows performance counters. The point of these handlers is that a lightweight handler can adapt connection statistics into any target app/service without needing to fetch the statistics themselves.

### Planned

This project is a barely functional prototype intended to solve a specific problem of mine (always-on latency monitor), but comically over-engineered because I have absolutely no self-control.

Ideally, I'd like to refactor things a bit to move more things out of the service app into the core lib, and then refactor update handlers into plugins that are reasonably easy to build/load.


### Notes

Your local AV will almost **certainly** flag this as suspicious because quite frankly it is suspicious as hell. This app is configured to run as a background service with admin rights, has an unauthenticated (local-only) HTTP API and P/Invokes out to arbitrary Win32 calls. It's pretty suspicious from an AV's point of view.