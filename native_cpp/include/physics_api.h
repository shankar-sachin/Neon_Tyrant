#ifndef NEON_TYRANT_PHYSICS_API_H
#define NEON_TYRANT_PHYSICS_API_H

#ifdef _WIN32
#define NT_EXPORT __declspec(dllexport)
#else
#define NT_EXPORT
#endif

extern "C" {

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

}

#endif
