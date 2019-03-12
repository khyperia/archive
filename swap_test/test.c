#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

int main() {
  const size_t gib = 1024 * 1024 * 1024;
  const size_t size = gib / 4;
  size_t total = 0;
  while (1) {
    total += size;
    printf("Up to %f GiB\n", (double)total / gib);
    char *tmp = malloc(size);
    memset(tmp, 0, size);
    sleep(1);
  }
  return 0;
}
