# Beginner projects

These small projects are meant to teach the workflow in layers instead of jumping straight into a full agent platform.

## Learning order

1. Run `AgentOrchestration.Beginner.Planning` to practice asking an AI agent for a clear implementation plan.
2. Run `AgentOrchestration.Beginner.Coding` to practice turning that plan into a single focused coding task.
3. Run `AgentOrchestration.LocalModelLab` to learn the basics of calling a local model that can later be orchestrated by the main runtime.

## Beginner planning

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Beginner.Planning
```

Use this project to learn:

- how a task request is shaped,
- how preferred tags influence routing,
- how to keep planning prompts small and explicit.

## Beginner coding

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.Beginner.Coding
```

Use this project to learn:

- how to request one coding task at a time,
- how to ask for checks and a definition of done,
- how to review generated work before continuing.

## Local model lab

```bash
dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.LocalModelLab -- --model llama3.1:8b --prompt "Explain why local models are useful for learning orchestration."
```

Use this project to learn:

- how a local model can sit behind a dispatchable node,
- how Ollama's HTTP API fits into the orchestration flow,
- how to evolve a single-machine experiment into a shared adapter later.

If Ollama is not running yet, start it first and pull a model such as `llama3.1:8b`.
