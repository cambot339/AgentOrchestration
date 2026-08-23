import fs from 'node:fs/promises';
import path from 'node:path';
import { createSession, type Plan, type PromptRecord, type ResponseRecord, type SessionRecord } from './contracts.js';

export class SessionStore {
  private readonly sessionsDir: string;

  public constructor(dataDir: string) {
    this.sessionsDir = path.join(dataDir, 'sessions');
  }

  public async initialize(): Promise<void> {
    await fs.mkdir(this.sessionsDir, { recursive: true });
  }

  public async listSessions(): Promise<SessionRecord[]> {
    await this.initialize();
    const entries = await fs.readdir(this.sessionsDir, { withFileTypes: true });
    const sessions = await Promise.all(
      entries
        .filter((entry) => entry.isFile() && entry.name.endsWith('.json'))
        .map((entry) => this.readSessionFile(path.join(this.sessionsDir, entry.name)))
    );

    return sessions.sort((left, right) => right.updatedAt.localeCompare(left.updatedAt));
  }

  public async getSession(id: string): Promise<SessionRecord> {
    await this.initialize();
    return this.readSessionFile(this.getSessionPath(id));
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
    return path.join(this.sessionsDir, `${id}.json`);
  }

  private async readSessionFile(filePath: string): Promise<SessionRecord> {
    try {
      const content = await fs.readFile(filePath, 'utf8');
      return JSON.parse(content) as SessionRecord;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
        throw new Error('Session not found.');
      }

      throw error;
    }
  }

  private async writeSession(session: SessionRecord): Promise<void> {
    await this.initialize();
    await fs.writeFile(this.getSessionPath(session.id), JSON.stringify(session, null, 2));
  }
}
