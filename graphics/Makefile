run: main
	./main

perf: main
	perf record ./main

main: main.c
	clang -g -O3 -Weverything main.c -lSDL2 -lm -o main
