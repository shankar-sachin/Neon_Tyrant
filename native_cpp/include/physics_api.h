#ifndef NEON_TYRANT_PHYSICS_API_H
#define NEON_TYRANT_PHYSICS_API_H

#ifdef _WIN32
#define NT_EXPORT __declspec(dllexport)
#else
#define NT_EXPORT
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* ---- Core physics types ---- */

typedef struct NtWorldConfig {
    float gravity;
    float jumpVelocity;
    float maxFallSpeed;
} NtWorldConfig;

typedef struct NtInput {
    float currentVelocityY;
    int jumpPressed;
    int isGrounded;
} NtInput;

typedef struct NtWorldSnapshot {
    float velocityY;
    float moveY;
    int grounded;
} NtWorldSnapshot;

typedef struct NtAabb {
    float x;
    float y;
    float w;
    float h;
} NtAabb;

typedef struct NtCollisionResult {
    float resolvedDx;
    float resolvedDy;
    int collidedX;
    int collidedY;
} NtCollisionResult;

typedef struct NtDashResult {
    float moveX;
    int cooldownMs;
    int didDash;
} NtDashResult;

typedef struct NtJumpAssistResult {
    int coyoteMs;
    int jumpBufferMs;
    int consumeJump;
} NtJumpAssistResult;

typedef struct NtEnemyState {
    float x;
    float left;
    float right;
    float speed;
    int direction;
    int turnDelayMs;
} NtEnemyState;

typedef struct NtBossStepResult {
    float x;
    float y;
    float dir;
} NtBossStepResult;

/* ---- Particle system types ---- */

typedef struct NtParticle {
    float x, y;
    float vx, vy;
    float r, g, b, a;
    float lifetime;
    float maxLifetime;
} NtParticle;

typedef struct NtParticleConfig {
    float gravity;
    float damping;
    int maxParticles;
} NtParticleConfig;

/* ---- Screen effects types ---- */

typedef struct NtShakeOffset {
    float offsetX;
    float offsetY;
} NtShakeOffset;

typedef struct NtFlashColor {
    float r, g, b, a;
} NtFlashColor;

/* ---- Spatial hash types ---- */

typedef struct NtSpatialQuery {
    int id;
    float x, y, w, h;
} NtSpatialQuery;

/* ---- Procedural generation types ---- */

typedef struct NtPlatformSegment {
    int startX, endX, y;
    int hasHazard;
} NtPlatformSegment;

typedef struct NtRoomTemplate {
    char tiles[20][64];
    int width, height;
    int spawnX, spawnY;
    int exitX, exitY;
} NtRoomTemplate;

/* ---- Core physics functions ---- */

NT_EXPORT int nt_init_world(const NtWorldConfig* cfg);
NT_EXPORT int nt_step_world(float dt, NtInput in, NtWorldSnapshot* out);
NT_EXPORT int nt_resolve_player_move(const NtAabb* player, float dx, float dy, const NtAabb* obstacle, NtCollisionResult* out);
NT_EXPORT int nt_check_hazard_overlap(const NtAabb* player, const NtAabb* hazard, int* hit);
NT_EXPORT int nt_boss_hit_test(const NtAabb* attack, const NtAabb* boss, int* bossHit);
NT_EXPORT int nt_compute_dash(float dt, int dashPressed, float facingDir, int cooldownMs, NtDashResult* out);
NT_EXPORT int nt_update_jump_assist(float dt, int jumpPressed, int isGrounded, int coyoteMs, int jumpBufferMs, NtJumpAssistResult* out);
NT_EXPORT int nt_update_enemy_patrol(float dt, const NtEnemyState* inState, NtEnemyState* outState);
NT_EXPORT int nt_update_boss_motion(float dt, float x, float baseY, float dir, float leftBound, float rightBound, float speed, int health, int maxHealth, int elapsedMs, NtBossStepResult* out);
NT_EXPORT void nt_shutdown_world(void);

/* ---- Particle system functions ---- */

NT_EXPORT int nt_particles_init(const NtParticleConfig* cfg);
NT_EXPORT int nt_particles_spawn(float x, float y, float vx, float vy, float r, float g, float b, float a, float lifetime);
NT_EXPORT int nt_particles_spawn_burst(float x, float y, float speed, int count, float r, float g, float b, float lifetime);
NT_EXPORT int nt_particles_update(float dt);
NT_EXPORT int nt_particles_get_count(int* count);
NT_EXPORT int nt_particles_get_data(NtParticle* outBuffer, int maxCount, int* actualCount);
NT_EXPORT void nt_particles_shutdown(void);

/* ---- Screen effects functions ---- */

NT_EXPORT int nt_fx_init(void);
NT_EXPORT int nt_fx_trigger_shake(float trauma);
NT_EXPORT int nt_fx_trigger_flash(float r, float g, float b, float duration);
NT_EXPORT int nt_fx_update(float dt);
NT_EXPORT int nt_fx_get_shake_offset(NtShakeOffset* out);
NT_EXPORT int nt_fx_get_flash_color(NtFlashColor* out);
NT_EXPORT void nt_fx_shutdown(void);

/* ---- Spatial hash functions ---- */

NT_EXPORT int nt_shash_init(float cellSize, int maxEntries);
NT_EXPORT int nt_shash_clear(void);
NT_EXPORT int nt_shash_insert(int id, float x, float y, float w, float h);
NT_EXPORT int nt_shash_query(float qx, float qy, float qw, float qh, int* outIds, int maxResults, int* actualCount);
NT_EXPORT void nt_shash_destroy(void);

/* ---- Procedural generation functions ---- */

NT_EXPORT int nt_procgen_seed(unsigned int seed);
NT_EXPORT int nt_procgen_platform_row(int width, int minGap, int maxGap, int minPlatLen, int maxPlatLen, NtPlatformSegment* out, int maxSegs, int* count);
NT_EXPORT int nt_procgen_obstacle_pattern(int width, int density, char* outRow);
NT_EXPORT int nt_procgen_room(int width, int height, NtRoomTemplate* out);

#ifdef __cplusplus
}
#endif

#endif
