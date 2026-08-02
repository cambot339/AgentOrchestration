# AgentOrchestration

AgentOrchestration is a small C# playground for learning how to dispatch AI-agent work across local and cloud resources without locking yourself into a single runtime too early.

## Goals

- Start with a progressive learning environment instead of a giant framework.
- Dispatch work to local or cloud agents from C#.
- Keep the networking/runtime layer separate from the main agent playground.
- Support mixed operating systems such as Windows 11, Windows 10, and Linux.
- Prefer graceful failover so work can continue when one machine is unavailable.

## Repository layout

- `/src/AgentOrchestration.Core` - shared orchestration models and a minimal failover dispatcher.
- `/src/AgentOrchestration.App` - the main learning-oriented runtime bootstrap.
- `/src/AgentOrchestration.Networking` - a separate networking runtime and debug tool for discovery and dispatch logging.
- `/docs/local-model-setup.md` - dedicated guide for downloading and setting up local model runtimes.
- `/docs/networking-runtime.md` - networking project notes and suggested expansion points.

## Current bootstrap

This repository now includes a minimal .NET 8+ solution that demonstrates:

- a cross-platform agent runtime scaffold,
- a separate networking project,
- simple local-network discovery,
- dispatch log capture for remote commands, and
- graceful failover from one node to the next available resource.

The bootstrap is intentionally small so you can replace the demo execution pieces with real Copilot integrations, local-model adapters, or custom agents as your skills grow.

## Build

```bash
dotnet build /home/runner/work/AgentOrchestration/AgentOrchestration/AgentOrchestration.slnx
```

## Run the main playground

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.App
```

This runs a simple demonstration where the first preferred machine fails and the dispatcher automatically continues on the next viable node.

## Run the networking runtime

Start a listener on each machine that should participate:

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- listen
```

Browse available machines on the same network:

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- browse
```

Dispatch a command envelope to a known host and log the dispatch locally:

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- dispatch 192.168.1.44 "ollama run llama3.1:8b"
```

## Suggested next milestones

1. Add real agent adapters for GitHub Copilot APIs, Ollama, llama.cpp, or LM Studio.
2. Move machine definitions into JSON configuration files.
3. Add health checks and retry backoff before dispatching to cloud fallback.
4. Add authenticated command transport once you move beyond a trusted local network.
5. Introduce tests around node ranking, retries, and network discovery.
