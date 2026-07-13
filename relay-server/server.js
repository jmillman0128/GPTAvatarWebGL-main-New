'use strict';

const express    = require('express');
const rateLimit  = require('express-rate-limit');

const app = express();
app.use(express.json());
app.set('trust proxy', 1);
// ---------------------------------------------------------------------------
//  In-memory key store
//  Structure:  Map<code: string, { openaiKey, googleKey, elevenLabsKey, expiresAt: number }>
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
//  Called by the landing page when the user submits keys for a session code.
//
//  Body:  { "code": "482719", "openaiKey": "sk-...", "googleKey": "", "elevenLabsKey": "" }
//  openaiKey is required.  googleKey and elevenLabsKey are optional (empty string OK).
//  Returns 200 on success, 400 on bad input, 429 on rate limit.
// ---------------------------------------------------------------------------

app.post('/api/submit-key', (req, res) => {
    const { code, openaiKey, googleKey = '', elevenLabsKey = '' } = req.body ?? {};

    if (!isValidCode(code)) {
        return res.status(400).json({ error: 'code must be exactly 6 digits.' });
    }
    if (!isValidApiKey(openaiKey)) {
        return res.status(400).json({ error: 'openaiKey is required and appears to be invalid.' });
    }
    if (googleKey && !isValidApiKey(googleKey)) {
        return res.status(400).json({ error: 'googleKey appears to be invalid.' });
    }
    if (elevenLabsKey && !isValidApiKey(elevenLabsKey)) {
        return res.status(400).json({ error: 'elevenLabsKey appears to be invalid.' });
    }

    keyStore.set(code, { openaiKey, googleKey, elevenLabsKey, expiresAt: Date.now() + TTL_MS });

    console.log(`[relay] Keys stored for code ${code}. Store size: ${keyStore.size}`);

    return res.status(200).json({
        message: 'Keys submitted. Your VR experience should start within a few seconds.',
    });
});

// ---------------------------------------------------------------------------
//  GET /api/get-key?code=XXXXXX
//  Polled by the Unity app every few seconds.
//  Returns the key and immediately deletes the entry (one-time pickup).
//
//  Returns 200 { "openaiKey": "...", "googleKey": "...", "elevenLabsKey": "..." } on success.
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

    console.log(`[relay] Keys picked up for code ${code}. Store size: ${keyStore.size}`);

    return res.status(200).json({
        openaiKey:     entry.openaiKey,
        googleKey:     entry.googleKey,
        elevenLabsKey: entry.elevenLabsKey,
    });
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
