const elements = {
  sessionSelect: document.getElementById('sessionSelect'),
  refreshSessionsButton: document.getElementById('refreshSessionsButton'),
  sessionNameInput: document.getElementById('sessionNameInput'),
  createSessionButton: document.getElementById('createSessionButton'),
  sessionMeta: document.getElementById('sessionMeta'),
  goalInput: document.getElementById('goalInput'),
  constraintsInput: document.getElementById('constraintsInput'),
  stepsInput: document.getElementById('stepsInput'),
  savePlanButton: document.getElementById('savePlanButton'),
  stepSelect: document.getElementById('stepSelect'),
  generatePromptButton: document.getElementById('generatePromptButton'),
  copyPromptButton: document.getElementById('copyPromptButton'),
  promptOutput: document.getElementById('promptOutput'),
  currentPromptMeta: document.getElementById('currentPromptMeta'),
  responseInput: document.getElementById('responseInput'),
  saveResponseButton: document.getElementById('saveResponseButton'),
  responseStatus: document.getElementById('responseStatus'),
  responseDetails: document.getElementById('responseDetails'),
  artifactsContainer: document.getElementById('artifactsContainer')
};

const state = {
  sessions: [],
  currentSession: null,
  currentPrompt: null
};

async function request(url, options = {}) {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers || {})
    },
    ...options
  });

  const contentType = response.headers.get('content-type') || '';
  const payload = contentType.includes('application/json') ? await response.json() : await response.text();

  if (!response.ok) {
    throw new Error(payload.error || 'Request failed.');
  }

  return payload;
}

async function loadSessions(preferredSessionId) {
  state.sessions = await request('/api/sessions');
  elements.sessionSelect.innerHTML = '';

  if (state.sessions.length === 0) {
    const option = document.createElement('option');
    option.textContent = 'No sessions yet';
    option.value = '';
    elements.sessionSelect.append(option);
    state.currentSession = null;
    renderSession();
    return;
  }

  for (const session of state.sessions) {
    const option = document.createElement('option');
    option.value = session.id;
    option.textContent = `${session.name} (${new Date(session.updatedAt).toLocaleString()})`;
    elements.sessionSelect.append(option);
  }

  const nextSessionId = preferredSessionId || state.currentSession?.id || state.sessions[0].id;
  elements.sessionSelect.value = nextSessionId;
  await loadSession(nextSessionId);
}

async function loadSession(sessionId) {
  if (!sessionId) {
    state.currentSession = null;
    renderSession();
    return;
  }

  state.currentSession = await request(`/api/sessions/${sessionId}`);
  renderSession();
}

function renderSession() {
  const session = state.currentSession;
  state.currentPrompt = session?.prompts?.[0] || null;
  elements.promptOutput.textContent = state.currentPrompt?.prompt || 'No prompt generated yet.';
  elements.currentPromptMeta.textContent = state.currentPrompt
    ? `Responses will be linked to prompt step ${state.currentPrompt.stepIndex + 1}: ${state.currentPrompt.stepText}`
    : 'Responses will be linked to the most recent generated prompt.';
  elements.responseStatus.textContent = '';

  if (!session) {
    elements.sessionMeta.textContent = 'Create a session to begin.';
    elements.goalInput.value = '';
    elements.constraintsInput.value = '';
    elements.stepsInput.value = '';
    elements.stepSelect.innerHTML = '';
    elements.responseDetails.innerHTML = '<p class="muted">No responses captured yet.</p>';
    elements.artifactsContainer.innerHTML = '<p class="muted">No artifacts yet.</p>';
    return;
  }

  elements.sessionMeta.textContent = `Session ${session.id} • created ${new Date(session.createdAt).toLocaleString()} • updated ${new Date(session.updatedAt).toLocaleString()}`;
  elements.goalInput.value = session.plan.goal || '';
  elements.constraintsInput.value = (session.plan.constraints || []).join('\n');
  elements.stepsInput.value = (session.plan.steps || []).join('\n');
  renderStepOptions(session.plan.steps || []);
  renderLatestResponse(session.responses?.[0] || null);
  renderArtifacts(session.responses.flatMap((response) => response.artifacts || []));
}

function renderLatestResponse(response) {
  if (!response) {
    elements.responseDetails.innerHTML = '<p class="muted">No responses captured yet.</p>';
    return;
  }

  const nextQuestions = (response.parsed?.next_questions || [])
    .map((question) => `<li>${escapeHtml(question)}</li>`)
    .join('');

  elements.responseDetails.innerHTML = `
    <article class="artifact">
      <p><strong>Branch:</strong> ${escapeHtml(response.parsed?.branch || '(not provided)')}</p>
      <p><strong>Summary:</strong> ${escapeHtml(response.parsed?.summary || '')}</p>
      ${nextQuestions ? `<div><strong>Next questions</strong><ul>${nextQuestions}</ul></div>` : '<p class="muted">No follow-up questions.</p>'}
    </article>
  `;
}

function renderStepOptions(steps) {
  elements.stepSelect.innerHTML = '';
  if (steps.length === 0) {
    const option = document.createElement('option');
    option.value = '';
    option.textContent = 'Save a plan with at least one step';
    elements.stepSelect.append(option);
    return;
  }

  steps.forEach((step, index) => {
    const option = document.createElement('option');
    option.value = `${index}`;
    option.textContent = `${index + 1}. ${step}`;
    elements.stepSelect.append(option);
  });
}

function renderArtifacts(artifacts) {
  if (!artifacts.length) {
    elements.artifactsContainer.innerHTML = '<p class="muted">No artifacts captured yet.</p>';
    return;
  }

  elements.artifactsContainer.innerHTML = '';
  for (const artifact of artifacts) {
    const article = document.createElement('article');
    article.className = 'artifact';
    article.innerHTML = `
      <h3>${escapeHtml(artifact.path)}</h3>
      <p>${escapeHtml(artifact.rationale)}</p>
      <label>Content</label>
      <textarea rows="10" readonly>${escapeHtml(artifact.content)}</textarea>
      <label>Write path (relative to workspace root; \ or / accepted)</label>
      <input type="text" value="${escapeAttribute(artifact.path)}" />
      <div class="row">
        <button type="button">Write file</button>
        <span class="muted"></span>
      </div>
    `;

    const targetInput = article.querySelector('input');
    const writeButton = article.querySelector('button');
    const status = article.querySelector('span');

    writeButton.addEventListener('click', async () => {
      status.textContent = 'Writing...';
      try {
        const result = await request(`/api/sessions/${state.currentSession.id}/artifacts/write`, {
          method: 'POST',
          body: JSON.stringify({
            writes: [
              {
                artifactId: artifact.id,
                targetPath: targetInput.value
              }
            ]
          })
        });
        status.textContent = `Wrote ${result.written[0].relativePath}`;
      } catch (error) {
        status.textContent = error.message;
      }
    });

    elements.artifactsContainer.append(article);
  }
}

function collectPlanPayload() {
  return {
    goal: elements.goalInput.value,
    constraints: elements.constraintsInput.value.split('\n').map((value) => value.trim()).filter(Boolean),
    steps: elements.stepsInput.value.split('\n').map((value) => value.trim()).filter(Boolean)
  };
}

async function createSession() {
  const session = await request('/api/sessions', {
    method: 'POST',
    body: JSON.stringify({ name: elements.sessionNameInput.value })
  });
  elements.sessionNameInput.value = '';
  await loadSessions(session.id);
}

async function savePlan() {
  if (!state.currentSession) {
    alert('Create a session first.');
    return;
  }

  const session = await request(`/api/sessions/${state.currentSession.id}/plan`, {
    method: 'PUT',
    body: JSON.stringify(collectPlanPayload())
  });

  state.currentSession = session;
  renderSession();
}

async function generatePrompt() {
  if (!state.currentSession) {
    alert('Create a session first.');
    return;
  }

  await request(`/api/sessions/${state.currentSession.id}/prompts`, {
    method: 'POST',
    body: JSON.stringify({ stepIndex: Number(elements.stepSelect.value) })
  });

  await loadSession(state.currentSession.id);
}

async function saveResponse() {
  if (!state.currentSession) {
    alert('Create a session first.');
    return;
  }

  elements.responseStatus.textContent = 'Saving response...';
  try {
    await request(`/api/sessions/${state.currentSession.id}/responses`, {
      method: 'POST',
      body: JSON.stringify({
        promptId: state.currentPrompt?.id,
        rawText: elements.responseInput.value
      })
    });
    elements.responseInput.value = '';
    elements.responseStatus.textContent = 'Response saved.';
    await loadSession(state.currentSession.id);
  } catch (error) {
    elements.responseStatus.textContent = error.message;
  }
}

async function copyPrompt() {
  const text = elements.promptOutput.textContent || '';
  if (!text || text === 'No prompt generated yet.') {
    return;
  }

  await navigator.clipboard.writeText(text);
}

function escapeHtml(value) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function escapeAttribute(value) {
  return escapeHtml(value).replaceAll('"', '&quot;');
}

elements.refreshSessionsButton.addEventListener('click', () => loadSessions().catch(showError));
elements.createSessionButton.addEventListener('click', () => createSession().catch(showError));
elements.sessionSelect.addEventListener('change', () => loadSession(elements.sessionSelect.value).catch(showError));
elements.savePlanButton.addEventListener('click', () => savePlan().catch(showError));
elements.generatePromptButton.addEventListener('click', () => generatePrompt().catch(showError));
elements.copyPromptButton.addEventListener('click', () => copyPrompt().catch(showError));
elements.saveResponseButton.addEventListener('click', () => saveResponse().catch(showError));

function showError(error) {
  alert(error.message || 'Unexpected error.');
}

loadSessions().catch(showError);
