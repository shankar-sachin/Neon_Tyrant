/*
 * screen_fx.cpp  --  Screen effects system for Neon Tyrant
 *
 * Provides screen-shake (trauma model) and screen-flash effects that
 * the C# game layer queries each frame through the native DLL boundary.
 *
 * All mutable state is held in file-scope statics; init/shutdown reset it.
 */

#include "physics_internal.h"

#include <algorithm>
#include <cmath>

/* ------------------------------------------------------------------ */
/*  Internal state                                                     */
/* ------------------------------------------------------------------ */

static bool   s_initialized   = false;

/* Shake state */
static float  s_trauma        = 0.0f;   /* current trauma level [0, 1]   */
static float  s_shakeTime     = 0.0f;   /* monotonic clock for noise     */
static float  s_offsetX       = 0.0f;   /* last computed X offset        */
static float  s_offsetY       = 0.0f;   /* last computed Y offset        */

/* Flash state */
static float  s_flashR        = 0.0f;
static float  s_flashG        = 0.0f;
static float  s_flashB        = 0.0f;
static float  s_flashDuration = 0.0f;   /* total duration of the flash   */
static float  s_flashRemaining= 0.0f;   /* time left on the flash        */

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

static constexpr float kMaxShakeOffset  = 4.0f;
static constexpr float kTraumaDecayBase = 0.85f;
static constexpr float kTraumaDecayRate = 10.0f;
static constexpr float kFlashStartAlpha = 0.6f;

/* ------------------------------------------------------------------ */
/*  Deterministic pseudo-noise  [-1, 1]                                */
/* ------------------------------------------------------------------ */

static float noise_hash(int seed) {
    /*
     * A fast, cheap integer hash loosely based on the "squirrel-noise"
     * family.  We only need something that *looks* random enough for a
     * screen-shake; cryptographic quality is irrelevant.
     */
    unsigned int n = static_cast<unsigned int>(seed);
    n ^= (n << 13);
    n *= 0xBD4BCB5u;
    n ^= (n >> 17);
    n *= 0x1B56C4E9u;
    n ^= (n << 5);

    /* Map the unsigned result into [-1, 1] */
    return static_cast<float>(n & 0x7FFFFFFF) / static_cast<float>(0x3FFFFFFF) - 1.0f;
}

/* ------------------------------------------------------------------ */
/*  Helper: reset every piece of state to its default                  */
/* ------------------------------------------------------------------ */

static void reset_state() {
    s_trauma         = 0.0f;
    s_shakeTime      = 0.0f;
    s_offsetX        = 0.0f;
    s_offsetY        = 0.0f;

    s_flashR         = 0.0f;
    s_flashG         = 0.0f;
    s_flashB         = 0.0f;
    s_flashDuration  = 0.0f;
    s_flashRemaining = 0.0f;
}

/* ================================================================== */
/*  Public API                                                         */
/* ================================================================== */

extern "C" {

NT_EXPORT int nt_fx_init(void) {
    reset_state();
    s_initialized = true;
    return 0;
}

NT_EXPORT int nt_fx_trigger_shake(float trauma) {
    if (!s_initialized) {
        return -1;
    }

    /* Clamp the incoming value, then add it to current trauma (capped at 1). */
    const float clamped = std::min(1.0f, std::max(0.0f, trauma));
    s_trauma = std::min(1.0f, s_trauma + clamped);
    return 0;
}

NT_EXPORT int nt_fx_trigger_flash(float r, float g, float b, float duration) {
    if (!s_initialized) {
        return -1;
    }
    if (duration <= 0.0f) {
        return -1;
    }

    /* Last-wins semantics: just overwrite any active flash. */
    s_flashR         = std::min(1.0f, std::max(0.0f, r));
    s_flashG         = std::min(1.0f, std::max(0.0f, g));
    s_flashB         = std::min(1.0f, std::max(0.0f, b));
    s_flashDuration  = duration;
    s_flashRemaining = duration;
    return 0;
}

NT_EXPORT int nt_fx_update(float dt) {
    if (!s_initialized) {
        return -1;
    }
    if (dt <= 0.0f) {
        return -1;
    }

    /* ---- Shake update ---- */

    /* Advance the internal timer (used as the noise seed). */
    s_shakeTime += dt;

    /* Decay trauma exponentially: trauma *= 0.85 ^ (dt * 10) */
    if (s_trauma > 0.0f) {
        const float decay = std::pow(kTraumaDecayBase, dt * kTraumaDecayRate);
        s_trauma *= decay;

        /* Snap to zero when negligible to avoid endless tiny jitter. */
        if (s_trauma < 0.001f) {
            s_trauma = 0.0f;
        }
    }

    /*
     * Compute shake intensity as trauma^2 (the classic GDC "juice"
     * approach).  Then sample two noise channels offset by large primes
     * so X and Y are uncorrelated.
     */
    const float intensity = s_trauma * s_trauma;
    const int   timeSeed  = static_cast<int>(s_shakeTime * 1000.0f);

    s_offsetX = kMaxShakeOffset * intensity * noise_hash(timeSeed);
    s_offsetY = kMaxShakeOffset * intensity * noise_hash(timeSeed + 99991);

    /* ---- Flash update ---- */

    if (s_flashRemaining > 0.0f) {
        s_flashRemaining -= dt;
        if (s_flashRemaining < 0.0f) {
            s_flashRemaining = 0.0f;
        }
    }

    return 0;
}

NT_EXPORT int nt_fx_get_shake_offset(NtShakeOffset* out) {
    if (!s_initialized || out == nullptr) {
        return -1;
    }

    out->offsetX = s_offsetX;
    out->offsetY = s_offsetY;
    return 0;
}

NT_EXPORT int nt_fx_get_flash_color(NtFlashColor* out) {
    if (!s_initialized || out == nullptr) {
        return -1;
    }

    if (s_flashRemaining <= 0.0f || s_flashDuration <= 0.0f) {
        /* No active flash -- return fully transparent black. */
        out->r = 0.0f;
        out->g = 0.0f;
        out->b = 0.0f;
        out->a = 0.0f;
        return 0;
    }

    /* Alpha fades linearly from kFlashStartAlpha down to 0 over the duration. */
    const float t = s_flashRemaining / s_flashDuration;   /* 1 -> 0 */
    const float alpha = kFlashStartAlpha * t;

    out->r = s_flashR;
    out->g = s_flashG;
    out->b = s_flashB;
    out->a = alpha;
    return 0;
}

NT_EXPORT void nt_fx_shutdown(void) {
    reset_state();
    s_initialized = false;
}

} /* extern "C" */
