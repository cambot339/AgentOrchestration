# Local model setup guide

This document is the dedicated starting point for downloading and setting up local model runtimes for the mixed-hardware lab described in this repository.

## Hardware profile

| Machine | GPU | Recommended first runtime | Notes |
| --- | --- | --- | --- |
| Main workstation | NVIDIA RTX 4070 | Ollama or LM Studio | Strong default choice for daily experimentation on Windows 11. |
| Main worker | NVIDIA GTX 1080 Ti | Ollama, llama.cpp, or text-generation-webui | Very usable for 7B and some 8B quantized models. |
| Extra worker | NVIDIA GTX 1070 | llama.cpp or Ollama where supported | Best treated as a lighter-duty worker for smaller quantized models. |

## Cross-platform baseline

Target .NET 8 or newer so the orchestration code can run on:

- Windows 11
- Windows 10
- modern Linux distributions

That lets you run the same repository on your main machine and on networked workers with minimal differences.

## Recommended local runtimes

### Option 1: Ollama

Best first stop when you want a clean local API and easy model downloads.

- Windows: install from the official Ollama installer.
- Linux: install from the official shell installer or package flow recommended by Ollama.
- Useful because your C# agent runtime can call its HTTP API without learning a custom wire format first.

Good starter models to try:

- `llama3.1:8b`
- `qwen2.5:7b`
- `mistral:7b`
- a smaller coding model if you want faster iteration on the GTX 1070 class machine

### Option 2: llama.cpp

Best when you want direct control over quantized GGUF models and lower-level experimentation.

- Works well on Windows and Linux.
- Especially useful for older GPUs or CPU fallback testing.
- Good for learning what your hardware can actually sustain.

### Option 3: LM Studio

Good choice when you want a GUI-first experience on Windows before automating everything.

- Helpful for model comparison and prompt testing.
- Can be paired later with scripted orchestration if you expose a local API.

## Suggested machine roles

### Main workstation (RTX 4070)

Use this machine for:

- primary development,
- larger local coding models,
- interactive debugging,
- first-choice dispatch target for GPU-heavy tasks.

### Main worker (GTX 1080 Ti)

Use this machine for:

- background jobs,
- secondary local inference,
- failover when the main workstation is busy.

### Extra worker (GTX 1070 or similar)

Use this machine for:

- lighter quantized models,
- low-priority tasks,
- smoke tests for Linux support.

## Multi-computer rollout plan

1. Install .NET 8+ on every machine.
2. Install one local model runtime per machine before mixing multiple tools.
3. Run `/home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.LocalModelLab` on your main machine to verify your first Ollama model call works locally.
3. Run `AgentOrchestration.Networking` in `listen` mode on each worker.
4. Use `browse` from the main machine to confirm local discovery works.
5. Start by dispatching harmless command envelopes and reviewing the generated logs.
6. Only after that, add real model execution adapters to the main app.

## Safety and practicality notes

- Treat the current networking runtime as a trusted-LAN bootstrap, not a hardened remote execution service.
- Prefer quantized models that fit comfortably in GPU memory instead of pushing each card to the limit.
- Keep one cloud fallback path available for tasks that exceed local hardware capacity.
- Add authentication and explicit allowlists before executing arbitrary remote commands.

## Progressive learning path

### Phase 1

- Run the sample app locally.
- Run `AgentOrchestration.LocalModelLab` against one local Ollama model.
- Run the networking listener on one extra machine.
- Browse the network and inspect dispatch logs.

### Phase 2

- Integrate one local runtime, preferably Ollama.
- Replace the demo runner with a real local model call.
- Add JSON config for your machine inventory.

### Phase 3

- Add cloud agents.
- Add workload routing rules.
- Add retries, health checks, and persistence.

### Phase 4

- Build custom agents from scratch.
- Add richer command/result contracts.
- Add authentication, metrics, and durable queues.
