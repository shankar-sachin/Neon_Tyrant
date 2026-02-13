/*
 * particles.c - Neon Tyrant particle effects system
 *
 * Compiled into physics.dll alongside the C++ modules.
 * Implements a fixed-pool particle system with gravity, damping,
 * radial burst spawning, and linear alpha fade.
 */

#include "../native_cpp/include/physics_api.h"
#include <math.h>
#include <string.h>

/* ---- Constants ---- */

#define NT_PARTICLES_DEFAULT_MAX  512
#define NT_PARTICLES_HARD_MAX     2048
#define NT_PI                     3.14159265358979323846f

/* ---- LCG random number generator ---- */

static unsigned int s_lcgState = 0xDEADBEEF;

static unsigned int lcg_next(void)
{
    s_lcgState = s_lcgState * 1664525u + 1013904223u;
    return s_lcgState;
}

/* Return a float in [0.0, 1.0) */
static float lcg_float(void)
{
    return (float)(lcg_next() & 0x7FFFFF) / (float)0x800000;
}

/* Return a float in [-1.0, 1.0) */
static float lcg_float_signed(void)
{
    return lcg_float() * 2.0f - 1.0f;
}

/* ---- Module state ---- */

static NtParticle  s_pool[NT_PARTICLES_HARD_MAX];
static int         s_activeCount = 0;
static int         s_maxParticles = 0;
static float       s_gravity = 0.0f;
static float       s_damping = 1.0f;
static int         s_initialized = 0;

/* ---- Implementation ---- */

NT_EXPORT int nt_particles_init(const NtParticleConfig* cfg)
{
    if (!cfg) {
        return -1;
    }

    int maxP = cfg->maxParticles;
    if (maxP <= 0) {
        maxP = NT_PARTICLES_DEFAULT_MAX;
    }
    if (maxP > NT_PARTICLES_HARD_MAX) {
        maxP = NT_PARTICLES_HARD_MAX;
    }

    s_maxParticles = maxP;
    s_gravity      = cfg->gravity;
    s_damping      = cfg->damping;
    s_activeCount  = 0;
    s_initialized  = 1;

    /* Seed the LCG with a value derived from the config so bursts vary */
    s_lcgState = (unsigned int)(cfg->gravity * 100000.0f) ^ 0xDEADBEEF;

    memset(s_pool, 0, sizeof(s_pool));

    return 0;
}

NT_EXPORT int nt_particles_spawn(float x, float y,
                                 float vx, float vy,
                                 float r, float g, float b, float a,
                                 float lifetime)
{
    if (!s_initialized) {
        return -1;
    }
    if (s_activeCount >= s_maxParticles) {
        return -1; /* pool full */
    }
    if (lifetime <= 0.0f) {
        return -1;
    }

    NtParticle* p = &s_pool[s_activeCount];
    p->x  = x;
    p->y  = y;
    p->vx = vx;
    p->vy = vy;
    p->r  = r;
    p->g  = g;
    p->b  = b;
    p->a  = a;
    p->lifetime    = lifetime;
    p->maxLifetime = lifetime;

    s_activeCount++;
    return 0;
}

NT_EXPORT int nt_particles_spawn_burst(float x, float y,
                                       float speed, int count,
                                       float r, float g, float b,
                                       float lifetime)
{
    if (!s_initialized) {
        return -1;
    }
    if (count <= 0 || lifetime <= 0.0f) {
        return -1;
    }

    float angleStep = (2.0f * NT_PI) / (float)count;

    for (int i = 0; i < count; i++) {
        if (s_activeCount >= s_maxParticles) {
            break; /* silently stop spawning when pool is full */
        }

        float angle = angleStep * (float)i;

        /* Add some randomness: +/- 15 degrees and 70%-130% speed variation */
        float angleJitter = lcg_float_signed() * (NT_PI / 12.0f);
        float speedJitter = 0.7f + lcg_float() * 0.6f;

        float finalAngle = angle + angleJitter;
        float finalSpeed = speed * speedJitter;

        float vx = cosf(finalAngle) * finalSpeed;
        float vy = sinf(finalAngle) * finalSpeed;

        /* Slight lifetime variation: 80%-120% of base lifetime */
        float lt = lifetime * (0.8f + lcg_float() * 0.4f);

        NtParticle* p = &s_pool[s_activeCount];
        p->x  = x;
        p->y  = y;
        p->vx = vx;
        p->vy = vy;
        p->r  = r;
        p->g  = g;
        p->b  = b;
        p->a  = 1.0f;  /* full alpha at spawn */
        p->lifetime    = lt;
        p->maxLifetime = lt;

        s_activeCount++;
    }

    return 0;
}

NT_EXPORT int nt_particles_update(float dt)
{
    if (!s_initialized) {
        return -1;
    }
    if (dt <= 0.0f) {
        return 0; /* nothing to do */
    }

    int i = 0;
    while (i < s_activeCount) {
        NtParticle* p = &s_pool[i];

        /* Apply gravity to vertical velocity */
        p->vy += s_gravity * dt;

        /* Apply velocity damping */
        p->vx *= (1.0f - s_damping * dt);
        p->vy *= (1.0f - s_damping * dt);

        /* Integrate position */
        p->x += p->vx * dt;
        p->y += p->vy * dt;

        /* Decrement lifetime */
        p->lifetime -= dt;

        /* Fade alpha linearly with remaining lifetime */
        if (p->maxLifetime > 0.0f) {
            float ratio = p->lifetime / p->maxLifetime;
            if (ratio < 0.0f) {
                ratio = 0.0f;
            }
            p->a = ratio;
        }

        /* Remove dead particles by swapping with the last active one */
        if (p->lifetime <= 0.0f) {
            s_pool[i] = s_pool[s_activeCount - 1];
            s_activeCount--;
            /* Don't increment i; re-check the swapped particle */
        } else {
            i++;
        }
    }

    return 0;
}

NT_EXPORT int nt_particles_get_count(int* count)
{
    if (!s_initialized || !count) {
        return -1;
    }
    *count = s_activeCount;
    return 0;
}

NT_EXPORT int nt_particles_get_data(NtParticle* outBuffer, int maxCount, int* actualCount)
{
    if (!s_initialized) {
        return -1;
    }
    if (!outBuffer || !actualCount) {
        return -1;
    }
    if (maxCount <= 0) {
        *actualCount = 0;
        return 0;
    }

    int toCopy = s_activeCount;
    if (toCopy > maxCount) {
        toCopy = maxCount;
    }

    memcpy(outBuffer, s_pool, (size_t)toCopy * sizeof(NtParticle));
    *actualCount = toCopy;

    return 0;
}

NT_EXPORT void nt_particles_shutdown(void)
{
    s_activeCount  = 0;
    s_maxParticles = 0;
    s_gravity      = 0.0f;
    s_damping      = 1.0f;
    s_initialized  = 0;

    memset(s_pool, 0, sizeof(s_pool));
}
