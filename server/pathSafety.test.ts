import test from 'node:test';
import assert from 'node:assert/strict';
import { resolveWorkspacePath } from './pathSafety.js';

test('resolveWorkspacePath keeps writes inside the workspace root', () => {
  const resolved = resolveWorkspacePath('/workspace/root', 'nested/file.txt');
  assert.equal(resolved.relativePath, 'nested/file.txt');
  assert.equal(resolved.absolutePath, '/workspace/root/nested/file.txt');
});

test('resolveWorkspacePath rejects parent-directory traversal', () => {
  assert.throws(() => resolveWorkspacePath('/workspace/root', '../secrets.txt'), /cannot escape/);
});

test('resolveWorkspacePath rejects absolute paths', () => {
  assert.throws(() => resolveWorkspacePath('/workspace/root', '/etc/passwd'), /must be relative/);
});
