#include "physics_internal.h"

#include <algorithm>
#include <cmath>

extern "C" {

int nt_update_boss_motion(float dt, float x, float baseY, float dir, float leftBound, float rightBound, float speed, int health, int maxHealth, int elapsedMs, NtBossStepResult* out) {
    if (!g_ready || out == nullptr || dt <= 0.0f || maxHealth <= 0) {
        return -1;
    }

    const float healthRatio = std::clamp(static_cast<float>(health) / static_cast<float>(maxHealth), 0.0f, 1.0f);
    const float aggressionBoost = 1.0f + (1.0f - healthRatio) * 0.35f;

    float nextDir = (dir < 0.0f) ? -1.0f : 1.0f;
    float nextX = x + speed * aggressionBoost * nextDir * dt;
    if (nextX <= leftBound || nextX >= rightBound) {
        nextDir *= -1.0f;
        nextX = std::clamp(nextX, leftBound, rightBound);
    }

    const float t = static_cast<float>(elapsedMs) / 1000.0f;
    const float bobAmplitude = 0.22f;
    const float bobFrequency = 3.8f;
    const float nextY = baseY + std::sin(t * bobFrequency) * bobAmplitude;

    out->x = nextX;
    out->y = nextY;
    out->dir = nextDir;
    return 0;
}

}
