/*
 * procgen.cpp -- Procedural generation module for Neon Tyrant
 *
 * Implements xorshift32-based PRNG-driven level generation including
 * platform rows, obstacle patterns, and complete room templates.
 * Compiled into physics.dll alongside the other native modules.
 */

#include "physics_internal.h"

#include <algorithm>
#include <cstring>

/* ---- xorshift32 PRNG state ---- */

static unsigned int s_prngState = 1u;

static unsigned int xorshift32() {
    unsigned int x = s_prngState;
    x ^= x << 13;
    x ^= x >> 17;
    x ^= x << 5;
    s_prngState = x;
    return x;
}

/* Returns a random integer in [lo, hi] (inclusive). */
static int randRange(int lo, int hi) {
    if (lo > hi) std::swap(lo, hi);
    unsigned int range = static_cast<unsigned int>(hi - lo + 1);
    return lo + static_cast<int>(xorshift32() % range);
}

/* Returns a random float in [0.0, 1.0). */
static float randFloat() {
    return static_cast<float>(xorshift32() & 0x00FFFFFFu) / 16777216.0f;
}

extern "C" {

/* ------------------------------------------------------------------ */
/*  nt_procgen_seed                                                   */
/* ------------------------------------------------------------------ */

NT_EXPORT int nt_procgen_seed(unsigned int seed) {
    /* Zero seed would stall xorshift -- force it to a non-zero value. */
    s_prngState = (seed != 0u) ? seed : 0xDEADBEEFu;
    return 0;
}

/* ------------------------------------------------------------------ */
/*  nt_procgen_platform_row                                           */
/* ------------------------------------------------------------------ */

NT_EXPORT int nt_procgen_platform_row(int width, int minGap, int maxGap,
                                      int minPlatLen, int maxPlatLen,
                                      NtPlatformSegment* out, int maxSegs,
                                      int* count) {
    if (out == nullptr || count == nullptr) return -1;
    if (width <= 0 || maxSegs <= 0) return -1;
    if (minGap < 0 || maxGap < minGap) return -1;
    if (minPlatLen <= 0 || maxPlatLen < minPlatLen) return -1;

    int cursor = 0;
    int segIdx = 0;

    /* Start with a small random gap so the row doesn't always begin at x=0. */
    cursor += randRange(0, std::min(maxGap, width / 4));

    while (cursor < width && segIdx < maxSegs) {
        /* Determine platform length, clamped so it doesn't exceed the row. */
        int platLen = randRange(minPlatLen, maxPlatLen);
        platLen = std::min(platLen, width - cursor);
        if (platLen <= 0) break;

        NtPlatformSegment seg;
        seg.startX = cursor;
        seg.endX   = cursor + platLen;
        seg.y      = 0; /* Caller sets the actual y coordinate. */
        seg.hasHazard = (randFloat() < 0.30f) ? 1 : 0;

        out[segIdx] = seg;
        segIdx++;

        cursor += platLen;

        /* Add a gap after the platform. */
        int gap = randRange(minGap, maxGap);
        cursor += gap;
    }

    *count = segIdx;
    return 0;
}

/* ------------------------------------------------------------------ */
/*  nt_procgen_obstacle_pattern                                       */
/* ------------------------------------------------------------------ */

NT_EXPORT int nt_procgen_obstacle_pattern(int width, int density,
                                          char* outRow) {
    if (outRow == nullptr) return -1;
    if (width <= 0) return -1;

    density = nt_clamp(density, 1, 10);

    /* Start with an empty row. */
    std::memset(outRow, ' ', static_cast<size_t>(width));

    /* Place hazard clusters. The number of clusters scales with density. */
    int numClusters = 1 + density / 3;   /* 1-4 clusters */
    for (int c = 0; c < numClusters; c++) {
        int clusterLen = randRange(2, 5);
        int startPos   = randRange(0, std::max(0, width - clusterLen));

        for (int i = 0; i < clusterLen && (startPos + i) < width; i++) {
            /* Higher density => higher chance each cell becomes a hazard. */
            if (randFloat() < (0.20f + density * 0.07f)) {
                outRow[startPos + i] = '^';
            }
        }
    }

    /* Sprinkle shards into empty cells. More shards at higher density. */
    int shardBudget = density;
    for (int attempt = 0; attempt < width * 2 && shardBudget > 0; attempt++) {
        int pos = randRange(0, width - 1);
        if (outRow[pos] == ' ') {
            if (randFloat() < (density * 0.06f)) {
                outRow[pos] = '*';
                shardBudget--;
            }
        }
    }

    return 0;
}

/* ------------------------------------------------------------------ */
/*  nt_procgen_room                                                   */
/* ------------------------------------------------------------------ */

NT_EXPORT int nt_procgen_room(int width, int height, NtRoomTemplate* out) {
    if (out == nullptr) return -1;

    /* Clamp dimensions to allowed tile array bounds.
     * NtRoomTemplate supports at most 20 rows (height) and 64 cols (width). */
    width  = nt_clamp(width,  20, 60);
    height = nt_clamp(height,  8, 20);

    out->width  = width;
    out->height = height;

    /* Clear the entire tile buffer. */
    std::memset(out->tiles, ' ', sizeof(out->tiles));

    /* ---- Draw border walls ---- */
    for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
            if (y == 0 || y == height - 1 || x == 0 || x == width - 1) {
                out->tiles[y][x] = '#';
            }
        }
    }

    /* ---- Generate platform rows ---- */
    int numPlatRows = randRange(3, 5);

    /* Distribute rows roughly evenly between floor and ceiling. */
    for (int r = 0; r < numPlatRows; r++) {
        /* Pick a y coordinate inside the room (avoid top/bottom walls). */
        int platY = 2 + randRange(0, height - 5);
        platY = nt_clamp(platY, 2, height - 3);

        NtPlatformSegment segs[32];
        int segCount = 0;
        int innerWidth = width - 2; /* exclude left/right walls */

        int ret = nt_procgen_platform_row(
            innerWidth,
            2, std::max(2, innerWidth / 6),   /* gap range  */
            3, std::max(3, innerWidth / 4),    /* plat range */
            segs, 32, &segCount
        );
        if (ret != 0) continue;

        for (int s = 0; s < segCount; s++) {
            int sx = segs[s].startX + 1; /* offset by 1 for left wall */
            int ex = segs[s].endX   + 1;
            ex = std::min(ex, width - 1);

            for (int x = sx; x < ex; x++) {
                out->tiles[platY][x] = '=';
            }

            /* Place hazards below some platforms (30% chance per segment). */
            if (segs[s].hasHazard && platY + 1 < height - 1) {
                int hazardStart = sx + randRange(0, std::max(0, (ex - sx) / 2));
                int hazardLen   = randRange(1, std::max(1, (ex - sx) / 2));
                for (int x = hazardStart; x < hazardStart + hazardLen && x < ex; x++) {
                    out->tiles[platY + 1][x] = '^';
                }
            }

            /* Sprinkle shards above platforms. */
            if (platY - 1 > 0) {
                int shardCount = randRange(0, 3);
                for (int sc = 0; sc < shardCount; sc++) {
                    int shardX = randRange(sx, std::max(sx, ex - 1));
                    if (out->tiles[platY - 1][shardX] == ' ') {
                        out->tiles[platY - 1][shardX] = '*';
                    }
                }
            }
        }
    }

    /* ---- Place spawn (bottom-left area) ---- */
    out->spawnX = 2;
    out->spawnY = height - 2;

    /* Clear a small landing zone around the spawn point. */
    for (int x = 1; x <= 3 && x < width - 1; x++) {
        if (out->tiles[out->spawnY][x] != '#') {
            out->tiles[out->spawnY][x] = ' ';
        }
    }

    /* ---- Place exit (top-right area) ---- */
    out->exitX = width - 3;
    out->exitY = 1;

    /* Make sure the exit cell is passable. */
    if (out->tiles[out->exitY][out->exitX] != '#') {
        out->tiles[out->exitY][out->exitX] = 'E';
    }

    /* Place a small platform under the exit so the player can reach it. */
    int exitPlatY = out->exitY + 1;
    if (exitPlatY < height - 1) {
        for (int x = width - 5; x < width - 1; x++) {
            if (x > 0 && out->tiles[exitPlatY][x] == ' ') {
                out->tiles[exitPlatY][x] = '=';
            }
        }
    }

    return 0;
}

} /* extern "C" */
