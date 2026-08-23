import path from 'node:path';
import { HttpError } from './errors.js';

export interface ResolvedWorkspacePath {
  relativePath: string;
  absolutePath: string;
}

export function resolveWorkspacePath(workspaceRoot: string, requestedPath: string): ResolvedWorkspacePath {
  const normalizedInput = requestedPath.trim().replace(/\\/g, '/');
  if (!normalizedInput) {
    throw new HttpError(400, 'A target path is required.');
  }

  if (normalizedInput.startsWith('/') || normalizedInput.startsWith('~') || /^[A-Za-z]:\//.test(normalizedInput)) {
    throw new HttpError(400, 'Artifact paths must be relative to the configured workspace root.');
  }

  const relativePath = path.posix.normalize(normalizedInput);
  if (relativePath === '.' || relativePath === '..' || relativePath.startsWith('../')) {
    throw new HttpError(400, 'Artifact paths cannot escape the configured workspace root.');
  }

  const absoluteRoot = path.resolve(workspaceRoot);
  const absolutePath = path.resolve(absoluteRoot, relativePath);
  const relativeToRoot = path.relative(absoluteRoot, absolutePath);

  if (relativeToRoot === '..' || relativeToRoot.startsWith(`..${path.sep}`) || path.isAbsolute(relativeToRoot)) {
    throw new HttpError(400, 'Artifact paths cannot escape the configured workspace root.');
  }

  return {
    relativePath,
    absolutePath
  };
}
