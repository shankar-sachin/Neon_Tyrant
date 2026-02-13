#include "physics_internal.h"

#include <algorithm>
#include <cmath>

NtWorldConfig g_cfg = {28.0f, -10.5f, 16.0f};
bool g_ready = false;

bool nt_intersects(const NtAabb* a, const NtAabb* b) {
    return !(a->x + a->w <= b->x || a->x >= b->x + b->w || a->y + a->h <= b->y || a->y >= b->y + b->h);
}

extern "C" {

int nt_init_world(const NtWorldConfig* cfg) {
    if (cfg != nullptr) {
        g_cfg = *cfg;
    }
    g_ready = true;
    return 0;
}

int nt_step_world(float dt, NtInput in, NtWorldSnapshot* out) {
    if (!g_ready || out == nullptr || dt <= 0.0f) {
        return -1;
    }

    float vy = in.currentVelocityY;
    if (in.isGrounded == 1 && in.jumpPressed == 1) {
        vy = g_cfg.jumpVelocity;
    } else {
        vy = std::min(g_cfg.maxFallSpeed, vy + g_cfg.gravity * dt);
    }

    out->velocityY = vy;
    out->moveY = vy * dt;
    out->grounded = in.isGrounded;
    return 0;
}

int nt_resolve_player_move(const NtAabb* player, float dx, float dy, const NtAabb* obstacle, NtCollisionResult* out) {
    if (!g_ready || player == nullptr || obstacle == nullptr || out == nullptr) {
        return -1;
    }

    NtAabb next = *player;
    next.x += dx;
    next.y += dy;
    out->resolvedDx = dx;
    out->resolvedDy = dy;
    out->collidedX = 0;
    out->collidedY = 0;

    if (!nt_intersects(&next, obstacle)) {
        return 0;
    }

    const float overlapLeft = (next.x + next.w) - obstacle->x;
    const float overlapRight = (obstacle->x + obstacle->w) - next.x;
    const float overlapTop = (next.y + next.h) - obstacle->y;
    const float overlapBottom = (obstacle->y + obstacle->h) - next.y;

    const float pushX = (overlapLeft < overlapRight) ? -overlapLeft : overlapRight;
    const float pushY = (overlapTop < overlapBottom) ? -overlapTop : overlapBottom;

    if (std::abs(pushX) < std::abs(pushY)) {
        out->resolvedDx += pushX;
        out->collidedX = 1;
    } else {
        out->resolvedDy += pushY;
        out->collidedY = 1;
    }

    return 0;
}

int nt_check_hazard_overlap(const NtAabb* player, const NtAabb* hazard, int* hit) {
    if (!g_ready || player == nullptr || hazard == nullptr || hit == nullptr) {
        return -1;
    }
    *hit = nt_intersects(player, hazard) ? 1 : 0;
    return 0;
}

int nt_boss_hit_test(const NtAabb* attack, const NtAabb* boss, int* bossHit) {
    if (!g_ready || attack == nullptr || boss == nullptr || bossHit == nullptr) {
        return -1;
    }
    *bossHit = nt_intersects(attack, boss) ? 1 : 0;
    return 0;
}

int nt_compute_dash(float dt, int dashPressed, float facingDir, int cooldownMs, NtDashResult* out) {
    if (!g_ready || out == nullptr || dt <= 0.0f) {
        return -1;
    }

    constexpr float kDashDistance = 2.6f;
    constexpr int kDashCooldownMs = 900;
    int nextCooldownMs = std::max(0, cooldownMs - static_cast<int>(dt * 1000.0f));
    const float normalizedFacing = (facingDir < 0.0f) ? -1.0f : 1.0f;

    out->moveX = 0.0f;
    out->didDash = 0;
    out->cooldownMs = nextCooldownMs;

    if (dashPressed == 1 && nextCooldownMs == 0) {
        out->moveX = normalizedFacing * kDashDistance;
        out->didDash = 1;
        out->cooldownMs = kDashCooldownMs;
    }
    return 0;
}

void nt_shutdown_world(void) {
    g_ready = false;
}

}
