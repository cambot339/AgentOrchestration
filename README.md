# AgentOrchestration

AgentOrchestration now includes a basic Node.js app for **human-in-the-loop orchestration** between GitHub Copilot and external agents such as Claude. The app assumes you are coordinating work inside a local git repository: create a plan, generate strict JSON prompts for sub-agents, paste their responses back in manually, review the reported branch and produced artifacts, and safely write selected files into your project workspace.

The original .NET playground remains in this repository under `src/` for experimentation with local/cloud dispatch and networking.

## What the Node app does

The MVP web app supports this workflow:

1. Define a plan with a goal, constraints, and steps
2. Generate a structured sub-agent prompt for a specific step
3. Copy that prompt into Claude, Copilot, or another external agent
4. Paste the sub-agent response back into the app
5. Review the reported branch and returned artifacts
6. Write reviewed files into a safe workspace path

Sessions persist to local JSON files in `data/sessions/`, so you can restart the app and continue where you left off.

## Quick start

Requirements:

- Windows 11 with PowerShell (default documented setup)
- Node.js 20+
- npm

Linux/macOS should also work, but the examples below assume a local Windows-first workflow.

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
| `WORKSPACE_ROOT` | `.` | Root directory artifacts may be written into; point this at the root of the local git repository you want to update |

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
  "branch": "copilot/implement-selected-step",
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

Artifact paths can use either `\` or `/`. The server normalizes relative paths before validating and writing them, so Windows-style relative paths are supported.

The `branch` value should name the local git branch that contains the requested code changes so you can review or continue the work in your repository.

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

## Requested feature checklist

- [x] Create, list, and reopen orchestration sessions
- [x] Edit a plan with goal, constraints, and ordered steps
- [x] Generate a strict sub-agent prompt for a selected plan step
- [x] Accept pasted JSON responses and persist them on disk
- [x] Capture the local git branch reported by the agent for requested code changes
- [x] Review returned artifacts before writing them into the local repository workspace
- [x] Block absolute-path and traversal escapes during writes
- [x] Provide a minimal browser UI for the manual orchestration loop
- [ ] Add multiple prompt templates for research, code review, and patch generation
- [ ] Add stronger schema validation and response-repair guidance
- [ ] Add artifact diffing before writes
- [ ] Add authentication before exposing the app beyond a trusted local machine
- [ ] Add richer session history, notes, and prompt/result comparison tools

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
