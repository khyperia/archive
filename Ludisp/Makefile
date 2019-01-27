CC = g++
CFLAGS = -std=c++11 -Wall -Wextra -O2
INCLUDES = -Ilucam/include
LFLAGS = -Llucam/lib/x86-64
LIBS = -l:lucamapi.a -lSDL2 -lSDL2_ttf -lcfitsio -lpthread
SRCS = main.cpp

OBJS = $(SRCS:.cpp=.o)
MAIN = ludisp

.PHONEY: clean

all: $(MAIN)

$(MAIN): $(OBJS)
	$(CC) $(CFLAGS) $(INCLUDES) -o $(MAIN) $(OBJS) $(LFLAGS) $(LIBS)

.cpp.o:
	$(CC) $(CFLAGS) $(INCLUDES) -c $< -o $@

clean:
	$(RM) *.o $(MAIN)
