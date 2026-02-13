/* ==========================================================================
 *  spatial_hash.c  --  Grid-based spatial hash for Neon Tyrant
 *  Compiled into physics.dll alongside the C++ physics modules.
 * ========================================================================== */

#include <stdlib.h>
#include <string.h>
#include <math.h>
#include "../native_cpp/include/physics_api.h"

/* ---------- compile-time constants ---------- */

#define SHASH_NUM_BUCKETS   1024
#define SHASH_PRIME_A       92821
#define SHASH_PRIME_B       689287

/* ---------- internal types ---------- */

typedef struct SHashEntry {
    int   id;
    float x, y, w, h;
    struct SHashEntry* next;   /* intrusive linked-list per bucket */
} SHashEntry;

typedef struct SHashState {
    SHashEntry** buckets;      /* array of SHASH_NUM_BUCKETS bucket heads   */
    SHashEntry*  pool;         /* flat pool of pre-allocated entries         */
    int          poolCapacity; /* total entries available in the pool        */
    int          poolUsed;     /* next free index in the pool               */
    float        cellSize;     /* width/height of each grid cell            */
    float        invCellSize;  /* 1.0f / cellSize, cached for fast division */
} SHashState;

/* ---------- module-level state ---------- */

static SHashState* g_shash = NULL;

/* ---------- helpers ---------- */

/* Hash two integer cell coordinates into a bucket index. */
static unsigned int shash_bucket_index(int cx, int cy)
{
    unsigned int h = (unsigned int)(cx * SHASH_PRIME_A) ^ (unsigned int)(cy * SHASH_PRIME_B);
    return h & (SHASH_NUM_BUCKETS - 1);   /* fast modulo for power-of-two */
}

/* Allocate the next entry from the flat pool.  Returns NULL when full. */
static SHashEntry* shash_pool_alloc(void)
{
    if (!g_shash || g_shash->poolUsed >= g_shash->poolCapacity)
        return NULL;
    return &g_shash->pool[g_shash->poolUsed++];
}

/* Insert a single entry into the bucket for cell (cx, cy). */
static int shash_bucket_insert(int cx, int cy, int id, float x, float y, float w, float h)
{
    unsigned int idx;
    SHashEntry*  entry;

    entry = shash_pool_alloc();
    if (!entry)
        return -1;  /* pool exhausted */

    entry->id = id;
    entry->x  = x;
    entry->y  = y;
    entry->w  = w;
    entry->h  = h;

    idx = shash_bucket_index(cx, cy);
    entry->next = g_shash->buckets[idx];
    g_shash->buckets[idx] = entry;

    return 0;
}

/* ---------- public API ---------- */

NT_EXPORT int nt_shash_init(float cellSize, int maxEntries)
{
    if (cellSize <= 0.0f || maxEntries <= 0)
        return -1;

    /* tear down any previous state first */
    nt_shash_destroy();

    g_shash = (SHashState*)malloc(sizeof(SHashState));
    if (!g_shash)
        return -1;

    g_shash->cellSize    = cellSize;
    g_shash->invCellSize = 1.0f / cellSize;
    g_shash->poolCapacity = maxEntries;
    g_shash->poolUsed     = 0;

    /* allocate the bucket array */
    g_shash->buckets = (SHashEntry**)malloc(sizeof(SHashEntry*) * SHASH_NUM_BUCKETS);
    if (!g_shash->buckets) {
        free(g_shash);
        g_shash = NULL;
        return -1;
    }
    memset(g_shash->buckets, 0, sizeof(SHashEntry*) * SHASH_NUM_BUCKETS);

    /* allocate the flat entry pool */
    g_shash->pool = (SHashEntry*)malloc(sizeof(SHashEntry) * (size_t)maxEntries);
    if (!g_shash->pool) {
        free(g_shash->buckets);
        free(g_shash);
        g_shash = NULL;
        return -1;
    }

    return 0;
}

NT_EXPORT int nt_shash_clear(void)
{
    if (!g_shash)
        return -1;

    /* O(1) reset: wipe bucket heads and rewind the pool allocator */
    memset(g_shash->buckets, 0, sizeof(SHashEntry*) * SHASH_NUM_BUCKETS);
    g_shash->poolUsed = 0;

    return 0;
}

NT_EXPORT int nt_shash_insert(int id, float x, float y, float w, float h)
{
    int minCX, minCY, maxCX, maxCY;
    int cx, cy;

    if (!g_shash)
        return -1;

    /* Determine the range of grid cells this AABB overlaps. */
    minCX = (int)floorf(x * g_shash->invCellSize);
    minCY = (int)floorf(y * g_shash->invCellSize);
    maxCX = (int)floorf((x + w) * g_shash->invCellSize);
    maxCY = (int)floorf((y + h) * g_shash->invCellSize);

    /* Insert into every overlapping cell. */
    for (cy = minCY; cy <= maxCY; cy++) {
        for (cx = minCX; cx <= maxCX; cx++) {
            if (shash_bucket_insert(cx, cy, id, x, y, w, h) != 0)
                return -1;  /* pool exhausted mid-insert */
        }
    }

    return 0;
}

NT_EXPORT int nt_shash_query(float qx, float qy, float qw, float qh,
                             int* outIds, int maxResults, int* actualCount)
{
    int minCX, minCY, maxCX, maxCY;
    int cx, cy;
    int count;
    /* Dynamically-allocated visited bitset for deduplication.  We allocate
       once per query and free at the end so there is no persistent overhead. */
    unsigned char* visited = NULL;
    int visitedBytes       = 0;

    if (!g_shash || !outIds || !actualCount || maxResults <= 0)
        return -1;

    /* Determine the size of the visited bitset.  It must cover the id range
       [0 .. poolCapacity).  Each entity id is expected to be a small non-
       negative integer assigned by the caller. */
    visitedBytes = (g_shash->poolCapacity + 7) / 8;
    visited = (unsigned char*)malloc((size_t)visitedBytes);
    if (!visited)
        return -1;
    memset(visited, 0, (size_t)visitedBytes);

    count = 0;

    /* Determine the grid cells that the query rectangle overlaps. */
    minCX = (int)floorf(qx * g_shash->invCellSize);
    minCY = (int)floorf(qy * g_shash->invCellSize);
    maxCX = (int)floorf((qx + qw) * g_shash->invCellSize);
    maxCY = (int)floorf((qy + qh) * g_shash->invCellSize);

    for (cy = minCY; cy <= maxCY && count < maxResults; cy++) {
        for (cx = minCX; cx <= maxCX && count < maxResults; cx++) {
            unsigned int idx = shash_bucket_index(cx, cy);
            SHashEntry*  e   = g_shash->buckets[idx];

            while (e && count < maxResults) {
                /* Broad-phase AABB overlap test between the query rect and
                   the stored entry.  This filters out hash collisions from
                   unrelated cells that map to the same bucket. */
                int overlaps =
                    (e->x < qx + qw) && (e->x + e->w > qx) &&
                    (e->y < qy + qh) && (e->y + e->h > qy);

                if (overlaps) {
                    /* Deduplicate: only report each id once. */
                    int eid = e->id;
                    if (eid >= 0 && eid < g_shash->poolCapacity) {
                        int byteIdx = eid / 8;
                        int bitIdx  = eid % 8;
                        if (!(visited[byteIdx] & (1 << bitIdx))) {
                            visited[byteIdx] |= (unsigned char)(1 << bitIdx);
                            outIds[count++] = eid;
                        }
                    }
                }

                e = e->next;
            }
        }
    }

    *actualCount = count;
    free(visited);
    return 0;
}

NT_EXPORT void nt_shash_destroy(void)
{
    if (!g_shash)
        return;

    if (g_shash->pool)
        free(g_shash->pool);

    if (g_shash->buckets)
        free(g_shash->buckets);

    free(g_shash);
    g_shash = NULL;
}
