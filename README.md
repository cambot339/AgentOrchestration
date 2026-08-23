# AgentOrchestration

AgentOrchestration now includes a basic Node.js app for **human-in-the-loop orchestration** between GitHub Copilot and external agents such as Claude. The app helps you create a plan, generate strict JSON prompts for sub-agents, paste their responses back in manually, review produced artifacts, and safely write those artifacts into your project workspace.

The original .NET playground remains in this repository under `src/` for experimentation with local/cloud dispatch and networking.

## What the Node app does

The MVP web app supports this workflow:

1. Define a plan with a goal, constraints, and steps
2. Generate a structured sub-agent prompt for a specific step
3. Copy that prompt into Claude, Copilot, or another external agent
4. Paste the sub-agent response back into the app
5. Validate and review returned artifacts
6. Write reviewed files into a safe workspace path

Sessions persist to local JSON files in `data/sessions/`, so you can restart the app and continue where you left off.

## Quick start

Requirements:

- Node.js 20+
- npm
- Ubuntu/Linux-friendly shell environment

Install dependencies:

```bash
cd <repo-root>
npm install
```

Run in development mode:

```bash
npm run dev
```

Run the compiled server:

```bash
npm run build
npm start
```

Open the app in your browser:

```text
http://localhost:3000
```

## Optional environment variables

Copy `.env.example` to `.env` if you want to customize runtime paths or the port.

| Variable | Default | Purpose |
| --- | --- | --- |
| `PORT` | `3000` | HTTP port for the web app |
| `DATA_DIR` | `./data` | Directory used for persisted session JSON |
| `WORKSPACE_ROOT` | `.` | Root directory artifacts may be written into |

## API overview

The backend exposes these main endpoints:

- `POST /api/sessions` - create a session
- `GET /api/sessions` - list sessions
- `GET /api/sessions/:id` - fetch a session
- `PUT /api/sessions/:id/plan` - update plan data
- `POST /api/sessions/:id/prompts` - generate a prompt for a plan step
- `POST /api/sessions/:id/responses` - ingest a pasted response
- `GET /api/sessions/:id/artifacts` - list parsed artifacts
- `POST /api/sessions/:id/artifacts/write` - write selected artifacts to disk

## Prompt contract

The built-in prompt template asks sub-agents to return strict JSON with:

```json
{
  "summary": "Short summary of the completed step.",
  "artifacts": [
    {
      "path": "relative/path/to/file.ext",
      "content": "full file contents",
      "rationale": "why this artifact was produced"
    }
  ],
  "next_questions": [
    "optional follow-up question"
  ]
}
```

You can paste either raw JSON or a fenced ```json``` block back into the app.

## Safety notes

- Artifact paths are validated before writes and cannot escape the configured workspace root.
- The app never executes generated code.
- Review all generated output before writing it to disk.

## Known limitations

- There is only one built-in prompt template.
- Response validation is intentionally basic and schema-focused.
- There is no authentication yet, so treat this as a local/trusted-environment tool for now.
- The UI is intentionally minimal and optimized for manual copy/paste workflows.

## Suggested next improvements

1. Add multiple prompt templates for research, code review, and patch generation.
2. Add stronger schema validation and better response repair guidance.
3. Add artifact diffing before writes.
4. Add authentication before exposing the app on a broader network.
5. Add richer session history, notes, and prompt/result comparisons.

## Existing .NET playground

The repository still includes the original .NET learning projects:

- `/src/AgentOrchestration.Core` - shared orchestration models and a minimal failover dispatcher
- `/src/AgentOrchestration.App` - the main learning-oriented runtime bootstrap
- `/src/AgentOrchestration.Networking` - a separate networking runtime and debug tool for discovery and dispatch logging
- `/docs/local-model-setup.md` - local model runtime setup notes
- `/docs/networking-runtime.md` - networking project notes

Build the .NET solution:

```bash
dotnet build AgentOrchestration.slnx
```
