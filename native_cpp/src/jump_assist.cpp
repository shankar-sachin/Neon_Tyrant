#include "physics_internal.h"

#include <algorithm>

extern "C" {

int nt_update_jump_assist(float dt, int jumpPressed, int isGrounded, int coyoteMs, int jumpBufferMs, NtJumpAssistResult* out) {
    if (!g_ready || out == nullptr || dt <= 0.0f) {
        return -1;
    }

    constexpr int kCoyoteWindowMs = 120;
    constexpr int kJumpBufferMs = 140;
    const int elapsedMs = std::max(1, static_cast<int>(dt * 1000.0f));

    int nextCoyoteMs = std::max(0, coyoteMs - elapsedMs);
    int nextJumpBufferMs = std::max(0, jumpBufferMs - elapsedMs);

    if (isGrounded == 1) {
        nextCoyoteMs = kCoyoteWindowMs;
    }
    if (jumpPressed == 1) {
        nextJumpBufferMs = kJumpBufferMs;
    }

    out->consumeJump = 0;
    if (nextCoyoteMs > 0 && nextJumpBufferMs > 0) {
        out->consumeJump = 1;
        nextCoyoteMs = 0;
        nextJumpBufferMs = 0;
    }

    out->coyoteMs = nextCoyoteMs;
    out->jumpBufferMs = nextJumpBufferMs;
    return 0;
}

}
