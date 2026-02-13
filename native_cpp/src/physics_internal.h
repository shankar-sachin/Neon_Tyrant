#ifndef NEON_TYRANT_PHYSICS_INTERNAL_H
#define NEON_TYRANT_PHYSICS_INTERNAL_H

#include "../include/physics_api.h"
#include <algorithm>

extern NtWorldConfig g_cfg;
extern bool g_ready;

bool nt_intersects(const NtAabb* a, const NtAabb* b);

template <typename T>
inline T nt_clamp(T value, T minValue, T maxValue) {
    return std::max(minValue, std::min(value, maxValue));
}

#endif
