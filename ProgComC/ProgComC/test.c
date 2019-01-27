#include test.c

struct Foo
{
	int x;
}

int y = 5;

int Inc(int v)
{
	return v + 1;
}

int (*superderp)(int) Derp(int (*derp)(int)) {
	return derp;
}

int Derp(int (*blah)(int), int x)
{
	return blah(x);
}

public void main()
{
	int testnum = 93000;
	int merp = Derp(&Inc)(2) % 2;
	--merp;
	int* string = "STRINNNGS";
}
