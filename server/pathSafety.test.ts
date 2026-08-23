import test from 'node:test';
import assert from 'node:assert/strict';
import { resolveWorkspacePath } from './pathSafety.js';

test('resolveWorkspacePath keeps writes inside the workspace root', () => {
  const resolved = resolveWorkspacePath('/workspace/root', 'nested/file.txt');
  assert.equal(resolved.relativePath, 'nested/file.txt');
  assert.equal(resolved.absolutePath, '/workspace/root/nested/file.txt');
});

test('resolveWorkspacePath normalizes Windows-style relative paths', () => {
  const resolved = resolveWorkspacePath('/workspace/root', 'nested\\folder\\file.txt');
  assert.equal(resolved.relativePath, 'nested/folder/file.txt');
  assert.equal(resolved.absolutePath, '/workspace/root/nested/folder/file.txt');
});

test('resolveWorkspacePath rejects parent-directory traversal', () => {
  assert.throws(() => resolveWorkspacePath('/workspace/root', '../secrets.txt'), /cannot escape/);
});

test('resolveWorkspacePath rejects absolute paths', () => {
  assert.throws(() => resolveWorkspacePath('/workspace/root', '/etc/passwd'), /must be relative/);
  assert.throws(() => resolveWorkspacePath('/workspace/root', 'C:\\Windows\\System32\\drivers\\etc\\hosts'), /must be relative/);
});
