#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int load_scores(const char* path) {
    FILE* fp = fopen(path, "r");
    if (fp == NULL) {
        return 0;
    }

    char line[512];
    while (fgets(line, sizeof(line), fp) != NULL) {
        fputs(line, stdout);
    }
    fclose(fp);
    return 0;
}

static int load_stats(const char* path) {
    FILE* fp = fopen(path, "r");
    if (fp == NULL) {
        printf("0,0,0\n");
        return 0;
    }

    char line[512];
    int count = 0;
    int max_score = 0;
    int total_score = 0;

    while (fgets(line, sizeof(line), fp) != NULL) {
        strtok(line, ",");
        char* score = strtok(NULL, ",");
        if (score == NULL) {
            continue;
        }

        int parsed = atoi(score);
        if (parsed > max_score) {
            max_score = parsed;
        }
        total_score += parsed;
        count++;
    }
    fclose(fp);

    if (count == 0) {
        printf("0,0,0\n");
        return 0;
    }

    printf("%d,%d,%d\n", count, max_score, total_score / count);
    return 0;
}

static int save_score(const char* path, const char* name, const char* score, const char* level, const char* time_ms) {
    FILE* fp = fopen(path, "a");
    if (fp == NULL) {
        return 2;
    }

    fprintf(fp, "%s,%s,%s,%s\n", name, score, level, time_ms);
    fclose(fp);
    return 0;
}

int main(int argc, char** argv) {
    if (argc < 3) {
        fprintf(stderr, "Usage: score_store.exe load <path> | stats <path> | save <path> <name> <score> <level> <time_ms>\n");
        return 1;
    }

    if (strcmp(argv[1], "load") == 0) {
        return load_scores(argv[2]);
    }

    if (strcmp(argv[1], "stats") == 0) {
        return load_stats(argv[2]);
    }

    if (strcmp(argv[1], "save") == 0) {
        if (argc < 7) {
            fprintf(stderr, "Missing save arguments.\n");
            return 1;
        }
        return save_score(argv[2], argv[3], argv[4], argv[5], argv[6]);
    }

    fprintf(stderr, "Unknown command: %s\n", argv[1]);
    return 1;
}
