#ifndef NEON_TYRANT_PHYSICS_INTERNAL_H
#define NEON_TYRANT_PHYSICS_INTERNAL_H

#include "../include/physics_api.h"

extern NtWorldConfig g_cfg;
extern bool g_ready;

bool nt_intersects(const NtAabb* a, const NtAabb* b);

#endif
