import fs from 'node:fs/promises';
import path from 'node:path';
import { createSession, type Plan, type PromptRecord, type ResponseRecord, type SessionRecord } from './contracts.js';
import { HttpError } from './errors.js';

const SESSION_ID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export class SessionStore {
  private readonly sessionsDir: string;
  private initializePromise?: Promise<void>;

  public constructor(dataDir: string) {
    this.sessionsDir = path.join(dataDir, 'sessions');
  }

  public async initialize(): Promise<void> {
    this.initializePromise ??= fs.mkdir(this.sessionsDir, { recursive: true })
      .then(() => undefined)
      .catch((error) => {
        this.initializePromise = undefined;
        throw error;
      });
    await this.initializePromise;
  }

  public async listSessions(): Promise<SessionRecord[]> {
    await this.initialize();
    const entries = await fs.readdir(this.sessionsDir, { withFileTypes: true });
    const sessions = await Promise.all(
      entries
        .filter((entry) => entry.isFile() && entry.name.endsWith('.json'))
        .map((entry) => path.basename(entry.name, '.json'))
        .filter((id) => SESSION_ID_PATTERN.test(id))
        .map((id) => this.readSession(id))
    );

    return sessions.sort((left, right) => right.updatedAt.localeCompare(left.updatedAt));
  }

  public async getSession(id: string): Promise<SessionRecord> {
    await this.initialize();
    return this.readSession(id);
  }

  public async createSession(name?: string): Promise<SessionRecord> {
    const session = createSession(name);
    await this.writeSession(session);
    return session;
  }

  public async updatePlan(id: string, plan: Plan): Promise<SessionRecord> {
    const session = await this.getSession(id);
    session.plan = plan;
    session.updatedAt = new Date().toISOString();
    await this.writeSession(session);
    return session;
  }

  public async addPrompt(id: string, prompt: PromptRecord): Promise<SessionRecord> {
    const session = await this.getSession(id);
    session.prompts.unshift(prompt);
    session.updatedAt = new Date().toISOString();
    await this.writeSession(session);
    return session;
  }

  public async addResponse(id: string, response: ResponseRecord): Promise<SessionRecord> {
    const session = await this.getSession(id);
    session.responses.unshift(response);
    session.updatedAt = new Date().toISOString();
    await this.writeSession(session);
    return session;
  }

  private getSessionPath(id: string): string {
    if (!SESSION_ID_PATTERN.test(id)) {
      throw new HttpError(400, 'Session id format is invalid.');
    }

    return path.join(this.sessionsDir, `${id}.json`);
  }

  private async readSession(id: string): Promise<SessionRecord> {
    try {
      const sessionFilePath = await this.findSessionFilePath(id);
      const content = await fs.readFile(sessionFilePath, 'utf8');
      return JSON.parse(content) as SessionRecord;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
        throw new HttpError(404, 'Session not found.');
      }

      throw error;
    }
  }

  private async writeSession(session: SessionRecord): Promise<void> {
    await this.initialize();
    await fs.writeFile(this.getSessionPath(session.id), JSON.stringify(session, null, 2));
  }

  private async findSessionFilePath(id: string): Promise<string> {
    if (!SESSION_ID_PATTERN.test(id)) {
      throw new HttpError(400, 'Session id format is invalid.');
    }

    const fileName = `${id}.json`;
    const entries = await fs.readdir(this.sessionsDir, { withFileTypes: true });
    const matchedFile = entries.find((entry) => entry.isFile() && entry.name === fileName);

    if (!matchedFile) {
      throw new HttpError(404, 'Session not found.');
    }

    return path.join(this.sessionsDir, matchedFile.name);
  }
}
