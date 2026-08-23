import test from 'node:test';
import assert from 'node:assert/strict';
import { buildPrompt, parseResponse, validateParsedResponse } from './contracts.js';

test('buildPrompt includes the selected plan step and strict JSON instructions', () => {
  const prompt = buildPrompt(
    {
      goal: 'Create a feature',
      constraints: ['Keep dependencies minimal'],
      steps: ['Design the API', 'Produce files']
    },
    1
  );

  assert.equal(prompt.stepText, 'Produce files');
  assert.match(prompt.prompt, /Return strict JSON only/);
  assert.match(prompt.prompt, /local git repository/);
  assert.match(prompt.prompt, /branch/);
  assert.match(prompt.prompt, /Current step \(2\/2\):/);
});

test('parseResponse accepts fenced JSON and validates artifacts', () => {
  const parsed = parseResponse(`\`\`\`json
{
  "summary": "Done",
  "branch": "copilot/test-branch",
  "artifacts": [
    {
      "path": "server/output.ts",
      "content": "export const value = 1;",
      "rationale": "Generated sample"
    }
  ],
  "next_questions": ["Need another file?"]
}
\`\`\``);

  assert.equal(parsed.summary, 'Done');
  assert.equal(parsed.branch, 'copilot/test-branch');
  assert.equal(parsed.artifacts[0]?.path, 'server/output.ts');
});

test('validateParsedResponse rejects malformed payloads', () => {
  assert.throws(
    () => validateParsedResponse({ summary: '', branch: 'copilot/test', artifacts: [], next_questions: [] }),
    /summary/
  );

  assert.throws(
    () => validateParsedResponse({ summary: 'ok', artifacts: [], next_questions: [] }),
    /branch/
  );

  assert.throws(
    () => validateParsedResponse({
      summary: 'ok',
      branch: 'copilot/test',
      artifacts: [{ content: 'x', rationale: 'why' }],
      next_questions: []
    }),
    /path/
  );
});
