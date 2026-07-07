'use strict';

const express    = require('express');
const rateLimit  = require('express-rate-limit');

const app = express();
app.use(express.json());

// ---------------------------------------------------------------------------
//  In-memory key store
//  Structure:  Map<code: string, { apiKey: string, expiresAt: number }>
//  Keys are NEVER written to disk.  Each entry is deleted on first pickup
//  or after TTL_MS milliseconds — whichever comes first.
// ---------------------------------------------------------------------------

const keyStore = new Map();
const TTL_MS   = 5 * 60 * 1000; // 5 minutes

// Evict expired entries every 60 seconds
setInterval(() => {
    const now = Date.now();
    for (const [code, entry] of keyStore) {
        if (now > entry.expiresAt) {
            keyStore.delete(code);
        }
    }
}, 60_000);

// ---------------------------------------------------------------------------
//  CORS — required so the landing page (any origin) can reach this server
// ---------------------------------------------------------------------------

app.use((req, res, next) => {
    res.setHeader('Access-Control-Allow-Origin',  '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    if (req.method === 'OPTIONS') {
        return res.sendStatus(204);
    }
    next();
});

// ---------------------------------------------------------------------------
//  Rate limiting — applied to all API routes
//  10 requests per minute per IP prevents enumeration of codes
// ---------------------------------------------------------------------------

const limiter = rateLimit({
    windowMs:        60 * 1000,
    max:             10,
    standardHeaders: true,
    legacyHeaders:   false,
    message:         { error: 'Too many requests — please wait a moment.' },
});

app.use('/api/', limiter);

// ---------------------------------------------------------------------------
//  Validation helpers
// ---------------------------------------------------------------------------

function isValidCode(code) {
    return typeof code === 'string' && /^\d{6}$/.test(code);
}

function isValidApiKey(key) {
    // Must be a non-empty string of reasonable length.
    // Intentionally broad so Google / ElevenLabs keys are accepted if needed later.
    return typeof key === 'string' && key.length >= 10 && key.length <= 512;
}

// ---------------------------------------------------------------------------
//  POST /api/submit-key
//  Called by the landing page when the user submits a code + API key.
//
//  Body:  { "code": "482719", "apiKey": "sk-..." }
//  Returns 200 on success, 400 on bad input, 429 on rate limit.
// ---------------------------------------------------------------------------

app.post('/api/submit-key', (req, res) => {
    const { code, apiKey } = req.body ?? {};

    if (!isValidCode(code)) {
        return res.status(400).json({ error: 'code must be exactly 6 digits.' });
    }
    if (!isValidApiKey(apiKey)) {
        return res.status(400).json({ error: 'apiKey appears to be invalid.' });
    }

    // Overwrite any existing entry for this code (user resubmitting)
    keyStore.set(code, { apiKey, expiresAt: Date.now() + TTL_MS });

    // Do NOT log the key value itself
    console.log(`[relay] Key stored for code ${code}. Store size: ${keyStore.size}`);

    return res.status(200).json({
        message: 'Key submitted. Your VR experience should start within a few seconds.',
    });
});

// ---------------------------------------------------------------------------
//  GET /api/get-key?code=XXXXXX
//  Polled by the Unity app every few seconds.
//  Returns the key and immediately deletes the entry (one-time pickup).
//
//  Returns 200 { "apiKey": "..." } on success.
//  Returns 404 { "error": "..." } when the code is not yet submitted / expired.
// ---------------------------------------------------------------------------

app.get('/api/get-key', (req, res) => {
    const { code } = req.query;

    if (!isValidCode(code)) {
        return res.status(400).json({ error: 'code must be exactly 6 digits.' });
    }

    const entry = keyStore.get(code);

    if (!entry) {
        return res.status(404).json({ error: 'Code not found or already used.' });
    }

    if (Date.now() > entry.expiresAt) {
        keyStore.delete(code);
        return res.status(404).json({ error: 'Code has expired.' });
    }

    // One-time pickup — delete immediately so the key cannot be retrieved again
    keyStore.delete(code);

    console.log(`[relay] Key picked up for code ${code}. Store size: ${keyStore.size}`);

    return res.status(200).json({ apiKey: entry.apiKey });
});

// ---------------------------------------------------------------------------
//  Health check — useful for hosting platform uptime monitors
// ---------------------------------------------------------------------------

app.get('/health', (_req, res) => res.sendStatus(200));

// ---------------------------------------------------------------------------
//  Start
// ---------------------------------------------------------------------------

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`[relay] Key relay server listening on port ${PORT}`);
    console.log('[relay] Keys are stored in memory only — no database, no persistence.');
});
