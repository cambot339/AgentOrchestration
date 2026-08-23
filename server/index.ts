import 'dotenv/config';

import express, { type Request, type Response } from 'express';
import rateLimit from 'express-rate-limit';
import fsSync from 'node:fs';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  buildPrompt,
  createResponseRecord,
  normalizePlan,
  parseResponse,
  type ArtifactRecord,
  type SessionRecord
} from './contracts.js';
import { HttpError } from './errors.js';
import { resolveWorkspacePath } from './pathSafety.js';
import { SessionStore } from './storage.js';

const currentFile = fileURLToPath(import.meta.url);
const currentDirectory = path.dirname(currentFile);
const repositoryRoot = findRepositoryRoot(currentDirectory);
const dataDirectory = path.resolve(repositoryRoot, process.env.DATA_DIR || 'data');
const workspaceRoot = path.resolve(repositoryRoot, process.env.WORKSPACE_ROOT || '.');
const webDirectory = path.join(repositoryRoot, 'web');
const port = Number.parseInt(process.env.PORT || '3000', 10);

const app = express();
const store = new SessionStore(dataDirectory);
const artifactWriteRateLimiter = rateLimit({
  windowMs: 60_000,
  limit: 30,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many artifact write requests. Please retry shortly.' }
});
const pageRateLimiter = rateLimit({
  windowMs: 60_000,
  limit: 240,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many page requests. Please retry shortly.' }
});

app.use(express.json({ limit: '2mb' }));

app.get('/api/health', (_request, response) => {
  response.json({
    ok: true,
    dataDirectory,
    workspaceRoot
  });
});

app.get('/api/sessions', asyncRoute(async (_request, response) => {
  const sessions = await store.listSessions();
  response.json(
    sessions.map((session) => ({
      id: session.id,
      name: session.name,
      createdAt: session.createdAt,
      updatedAt: session.updatedAt,
      stepCount: session.plan.steps.length,
      artifactCount: flattenArtifacts(session).length
    }))
  );
}));

app.post('/api/sessions', asyncRoute(async (request, response) => {
  const name = typeof request.body?.name === 'string' ? request.body.name : undefined;
  const session = await store.createSession(name);
  response.status(201).json(session);
}));

app.get('/api/sessions/:id', asyncRoute(async (request, response) => {
  const session = await store.getSession(getRouteId(request));
  response.json(session);
}));

app.put('/api/sessions/:id/plan', asyncRoute(async (request, response) => {
  const session = await store.updatePlan(getRouteId(request), normalizePlan(request.body));
  response.json(session);
}));

app.post('/api/sessions/:id/prompts', asyncRoute(async (request, response) => {
  const stepIndex = parseStepIndex(request.body?.stepIndex);
  if (!Number.isInteger(stepIndex)) {
    response.status(400).json({ error: 'A numeric stepIndex is required.' });
    return;
  }

  const session = await store.getSession(getRouteId(request));
  const prompt = buildPrompt(session.plan, stepIndex);
  await store.addPrompt(session.id, prompt);
  response.status(201).json(prompt);
}));

app.post('/api/sessions/:id/responses', asyncRoute(async (request, response) => {
  const rawText = typeof request.body?.rawText === 'string' ? request.body.rawText : '';
  const promptId = typeof request.body?.promptId === 'string' ? request.body.promptId : undefined;
  const parsed = parseResponse(rawText);
  const responseRecord = createResponseRecord(rawText, parsed, promptId);
  await store.addResponse(getRouteId(request), responseRecord);
  response.status(201).json(responseRecord);
}));

app.get('/api/sessions/:id/artifacts', asyncRoute(async (request, response) => {
  const session = await store.getSession(getRouteId(request));
  response.json(flattenArtifacts(session));
}));

app.post('/api/sessions/:id/artifacts/write', artifactWriteRateLimiter, asyncRoute(async (request, response) => {
  const writes = Array.isArray(request.body?.writes) ? request.body.writes : [];
  if (writes.length === 0) {
    response.status(400).json({ error: 'At least one artifact write request is required.' });
    return;
  }

  const session = await store.getSession(getRouteId(request));
  const artifacts = flattenArtifacts(session);
  const artifactsById = new Map(artifacts.map((artifact) => [artifact.id, artifact]));

  const results: Array<{ artifactId: string; relativePath: string; absolutePath: string }> = [];
  for (const writeRequest of writes) {
    const artifactId = typeof writeRequest?.artifactId === 'string' ? writeRequest.artifactId : '';
    const targetPath = typeof writeRequest?.targetPath === 'string' ? writeRequest.targetPath : undefined;

    const artifact = artifactsById.get(artifactId);
    if (!artifact) {
      response.status(404).json({ error: `Artifact '${artifactId}' was not found in this session.` });
      return;
    }

    const resolvedPath = resolveWorkspacePath(workspaceRoot, targetPath || artifact.path);
    await fs.mkdir(path.dirname(resolvedPath.absolutePath), { recursive: true });
    await fs.writeFile(resolvedPath.absolutePath, artifact.content, 'utf8');

    results.push({
      artifactId,
      relativePath: resolvedPath.relativePath,
      absolutePath: resolvedPath.absolutePath
    });
  }

  response.status(201).json({
    written: results
  });
}));

app.use(pageRateLimiter, express.static(webDirectory));
app.get(/.*/, pageRateLimiter, (request, response) => {
  if (request.path.startsWith('/api/')) {
    response.status(404).json({ error: 'API route not found.' });
    return;
  }

  response.sendFile(path.join(webDirectory, 'index.html'));
});

app.use((error: unknown, _request: Request, response: Response, _next: unknown) => {
  if (error instanceof HttpError) {
    response.status(error.statusCode).json({ error: error.message });
    return;
  }

  const message = error instanceof Error ? error.message : 'Unexpected server error.';
  response.status(500).json({ error: message });
});

await store.initialize();

app.listen(port, () => {
  console.log(`Agent orchestration app running on http://localhost:${port}`);
  console.log(`Data directory: ${dataDirectory}`);
  console.log(`Workspace root: ${workspaceRoot}`);
});

function findRepositoryRoot(startDirectory: string): string {
  let currentDirectory = startDirectory;

  while (true) {
    const packageJsonPath = path.join(currentDirectory, 'package.json');
    if (fsSync.existsSync(packageJsonPath)) {
      return currentDirectory;
    }

    const parentDirectory = path.dirname(currentDirectory);
    if (parentDirectory === currentDirectory) {
      throw new Error('Could not locate repository root.');
    }

    currentDirectory = parentDirectory;
  }
}

function flattenArtifacts(session: SessionRecord): ArtifactRecord[] {
  return session.responses.flatMap((responseRecord) => responseRecord.artifacts);
}

function asyncRoute(
  handler: (request: Request, response: Response) => Promise<void>
): (request: Request, response: Response, next: (error?: unknown) => void) => void {
  return (request, response, next) => {
    handler(request, response).catch(next);
  };
}

function getRouteId(request: Request): string {
  return Array.isArray(request.params.id) ? request.params.id[0] ?? '' : request.params.id;
}

function parseStepIndex(value: unknown): number {
  if (typeof value === 'number' && Number.isInteger(value)) {
    return value;
  }

  if (typeof value === 'string' && /^\d+$/.test(value)) {
    return Number.parseInt(value, 10);
  }

  return Number.NaN;
}
