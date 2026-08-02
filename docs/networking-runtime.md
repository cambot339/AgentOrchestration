# Networking runtime notes

`AgentOrchestration.Networking` is intentionally separated from the main playground so you can evolve transport, discovery, and diagnostics without tangling that work into agent logic.

## What the bootstrap runtime does

- listens for UDP discovery requests on the local network,
- responds with basic node metadata,
- accepts TCP command envelopes,
- writes JSONL logs for dispatched commands, and
- gives you a small debug surface for mixed Windows/Linux testing.

## Commands

### Listen

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- listen [node-name] [log-path]
```

### Browse

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- browse
```

### Dispatch

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Networking -- dispatch <host> <command> [task-name] [log-path]
```

## Why it is separate

This split makes it easier to:

- debug networking issues without running the full agent runtime,
- experiment with alternative protocols,
- test Windows and Linux hosts independently,
- add richer logging or packet capture later.

## Recommended next improvements

1. Move from open LAN traffic to authenticated transport.
2. Add node health checks and heartbeats.
3. Add durable queues so tasks can survive machine restarts.
4. Add structured task results instead of simple acknowledgements.
5. Add a small UI or TUI for browsing and dispatch inspection.
