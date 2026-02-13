#include "physics_internal.h"

#include <algorithm>

extern "C" {

int nt_update_enemy_patrol(float dt, const NtEnemyState* inState, NtEnemyState* outState) {
    if (!g_ready || inState == nullptr || outState == nullptr || dt <= 0.0f) {
        return -1;
    }

    const int elapsedMs = std::max(1, static_cast<int>(dt * 1000.0f));
    const int nextTurnDelayMs = std::max(0, inState->turnDelayMs - elapsedMs);

    *outState = *inState;
    outState->turnDelayMs = nextTurnDelayMs;

    if (outState->turnDelayMs > 0) {
        return 0;
    }

    float x = inState->x + inState->speed * static_cast<float>(inState->direction) * dt;
    int direction = inState->direction;
    int turnDelayMs = 0;

    if (x <= inState->left || x >= inState->right) {
        x = std::clamp(x, inState->left, inState->right);
        direction = -direction;
        turnDelayMs = 80;
    }

    outState->x = x;
    outState->direction = direction;
    outState->turnDelayMs = turnDelayMs;
    return 0;
}

}
