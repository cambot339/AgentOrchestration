import crypto from 'node:crypto';
import { HttpError } from './errors.js';

export interface Plan {
  goal: string;
  constraints: string[];
  steps: string[];
}

export interface PromptRecord {
  id: string;
  templateId: string;
  stepIndex: number;
  stepText: string;
  prompt: string;
  createdAt: string;
}

export interface ArtifactRecord {
  id: string;
  responseId: string;
  path: string;
  content: string;
  rationale: string;
}

export interface ParsedResponsePayload {
  summary: string;
  branch: string;
  artifacts: Array<{
    path: string;
    content: string;
    rationale: string;
  }>;
  next_questions: string[];
}

export interface ResponseRecord {
  id: string;
  promptId?: string;
  rawText: string;
  receivedAt: string;
  parsed: ParsedResponsePayload;
  artifacts: ArtifactRecord[];
}

export interface SessionRecord {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  plan: Plan;
  prompts: PromptRecord[];
  responses: ResponseRecord[];
}

export const BUILT_IN_TEMPLATE_ID = 'sub-agent-file-generation';

export function createEmptyPlan(): Plan {
  return {
    goal: '',
    constraints: [],
    steps: []
  };
}

export function createSession(name?: string): SessionRecord {
  const timestamp = new Date().toISOString();
  const id = crypto.randomUUID();

  return {
    id,
    name: name?.trim() || `Session ${timestamp}`,
    createdAt: timestamp,
    updatedAt: timestamp,
    plan: createEmptyPlan(),
    prompts: [],
    responses: []
  };
}

export function normalizePlan(input: Partial<Plan> | undefined): Plan {
  return {
    goal: typeof input?.goal === 'string' ? input.goal.trim() : '',
    constraints: normalizeStringArray(input?.constraints),
    steps: normalizeStringArray(input?.steps)
  };
}

export function normalizeStringArray(input: unknown): string[] {
  if (!Array.isArray(input)) {
    return [];
  }

  return input
    .filter((item): item is string => typeof item === 'string')
    .map((item) => item.trim())
    .filter(Boolean);
}

export function buildPrompt(plan: Plan, stepIndex: number): PromptRecord {
  const stepText = plan.steps[stepIndex];
  if (!stepText) {
    throw new HttpError(400, 'The requested plan step does not exist.');
  }

  const prompt = [
    'You are a sub-agent helping with a human-in-the-loop orchestration session.',
    'Assume the workspace is a local git repository.',
    'Return strict JSON only. Do not wrap the JSON in markdown fences. Do not add commentary before or after the JSON.',
    '',
    'Goal:',
    plan.goal || '(not provided)',
    '',
    'Constraints:',
    ...(plan.constraints.length > 0 ? plan.constraints.map((constraint) => `- ${constraint}`) : ['- None provided']),
    '',
    `Current step (${stepIndex + 1}/${plan.steps.length}):`,
    stepText,
    '',
    'Required JSON schema:',
    JSON.stringify(
      {
        summary: 'Short summary of the completed step.',
        branch: 'copilot/implement-selected-step',
        artifacts: [
          {
            path: 'relative/path/to/file.ext',
            content: 'full file contents',
            rationale: 'why this artifact was produced'
          }
        ],
        next_questions: ['optional follow-up question']
      },
      null,
      2
    ),
    '',
    'Rules:',
    '- Include the git branch name that contains the requested code changes in the branch field.',
    '- Keep artifact paths relative. Windows-style backslashes are allowed and will be normalized.',
    '- Put complete file contents in each artifacts[].content field.',
    '- Use an empty array when there are no artifacts or no next questions.',
    '- Never omit required fields.'
  ].join('\n');

  return {
    id: crypto.randomUUID(),
    templateId: BUILT_IN_TEMPLATE_ID,
    stepIndex,
    stepText,
    prompt,
    createdAt: new Date().toISOString()
  };
}

export function parseResponse(rawText: string): ParsedResponsePayload {
  const candidate = extractJsonCandidate(rawText);
  let parsed: unknown;

  try {
    parsed = JSON.parse(candidate);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unknown JSON parse error.';
    throw new HttpError(400, `Response must contain valid JSON. ${message}`);
  }

  return validateParsedResponse(parsed);
}

export function validateParsedResponse(input: unknown): ParsedResponsePayload {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    throw new HttpError(400, 'Response JSON must be an object.');
  }

  const record = input as Record<string, unknown>;

  if (typeof record.summary !== 'string' || !record.summary.trim()) {
    throw new HttpError(400, 'Response JSON must include a non-empty string field named "summary".');
  }

  if (typeof record.branch !== 'string' || !record.branch.trim()) {
    throw new HttpError(400, 'Response JSON must include a non-empty string field named "branch".');
  }

  if (!Array.isArray(record.next_questions) || record.next_questions.some((item) => typeof item !== 'string')) {
    throw new HttpError(400, 'Response JSON must include a string array field named "next_questions".');
  }

  if (!Array.isArray(record.artifacts)) {
    throw new HttpError(400, 'Response JSON must include an array field named "artifacts".');
  }

  const artifacts = record.artifacts.map((artifact, index) => {
    if (!artifact || typeof artifact !== 'object' || Array.isArray(artifact)) {
      throw new HttpError(400, `Artifact at index ${index} must be an object.`);
    }

    const artifactRecord = artifact as Record<string, unknown>;
    if (typeof artifactRecord.path !== 'string' || !artifactRecord.path.trim()) {
      throw new HttpError(400, `Artifact at index ${index} must include a non-empty string field named "path".`);
    }

    if (typeof artifactRecord.content !== 'string') {
      throw new HttpError(400, `Artifact at index ${index} must include a string field named "content".`);
    }

    if (typeof artifactRecord.rationale !== 'string' || !artifactRecord.rationale.trim()) {
      throw new HttpError(400, `Artifact at index ${index} must include a non-empty string field named "rationale".`);
    }

    return {
      path: artifactRecord.path.trim(),
      content: artifactRecord.content,
      rationale: artifactRecord.rationale.trim()
    };
  });

  return {
    summary: record.summary.trim(),
    branch: record.branch.trim(),
    next_questions: record.next_questions.map((question) => question.trim()),
    artifacts
  };
}

export function createResponseRecord(rawText: string, parsed: ParsedResponsePayload, promptId?: string): ResponseRecord {
  const responseId = crypto.randomUUID();

  return {
    id: responseId,
    promptId,
    rawText,
    receivedAt: new Date().toISOString(),
    parsed,
    artifacts: parsed.artifacts.map((artifact) => ({
      ...artifact,
      id: crypto.randomUUID(),
      responseId
    }))
  };
}

function extractJsonCandidate(rawText: string): string {
  const trimmed = rawText.trim();
  if (!trimmed) {
    throw new HttpError(400, 'Response text is required.');
  }

  if (trimmed.startsWith('```')) {
    const firstLineBreak = trimmed.indexOf('\n');
    const closingFence = trimmed.lastIndexOf('```');
    if (firstLineBreak !== -1 && closingFence > firstLineBreak) {
      return trimmed.slice(firstLineBreak + 1, closingFence).trim();
    }
  }

  return trimmed;
}
