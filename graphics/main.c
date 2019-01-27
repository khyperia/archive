#include <SDL2/SDL.h>

#define HandleSdl(x) HandleSdlImpl(x, #x, __FILE__, __LINE__)

static void HandleSdlImpl(int result,
                          const char *expr,
                          const char *file,
                          const unsigned int line)
{
    if (result)
    {
        printf("%s(%u): SDL failure (%d) \"%s\" - %s\n",
               file,
               line,
               result,
               expr,
               SDL_GetError());
        exit(1);
    }
}

#define FailIf(x, m) FailIfImpl(x, m, __FILE__, __LINE__)

static void
FailIfImpl(int fail, const char *msg, const char *file, const unsigned int line)
{
    if (fail)
    {
        printf("%s(%u): Failure (%d) - %s\n", file, line, fail, msg);
        exit(1);
    }
}

static SDL_Renderer *renderer;

static float randf(void)
{
    return (float)drand48();
}

static void rand_gauss(float *v1, float *v2)
{
    double r = sqrt(-2 * log(drand48()));
    double t = (M_PI * 2) * drand48();
    *v1 = (float)(r * cos(t));
    *v2 = (float)(r * sin(t));
}

static void hv_to_rgb(float hue, float value, float *r, float *g, float *b)
{
    hue = fmodf(hue, 1);
    hue *= 3;
    float frac = fmodf(hue, 1);
    switch ((int)hue)
    {
        case 0:
            *r = 1 - frac;
            *g = frac;
            *b = 0;
            break;
        case 1:
            *r = 0;
            *g = 1 - frac;
            *b = frac;
            break;
        case 2:
            *r = frac;
            *g = 0;
            *b = 1 - frac;
            break;
        default:
            *r = frac;
            *g = frac;
            *b = frac;
            break;
    }
    // *r *= value;
    // *g *= value;
    // *b *= value;
    *r = sqrtf(*r) * value;
    *g = sqrtf(*g) * value;
    *b = sqrtf(*b) * value;
}

static void draw_line(Uint8 r, Uint8 g, Uint8 b, int x1, int y1, int x2, int y2)
{
    HandleSdl(SDL_SetRenderDrawColor(renderer, r, g, b, 255));
    HandleSdl(SDL_RenderDrawLine(renderer, x1, y1, x2, y2));
}

static void draw_point(Uint8 r, Uint8 g, Uint8 b, int x, int y)
{
    HandleSdl(SDL_SetRenderDrawColor(renderer, r, g, b, 255));
    HandleSdl(SDL_RenderDrawPoint(renderer, x, y));
}

static void draw_circ(Uint8 r, Uint8 g, Uint8 b, float x, float y, float rad)
{
    HandleSdl(SDL_SetRenderDrawColor(renderer, r, g, b, 255));
    SDL_Point points[501];
    for (int i = 0; i < 500; i++)
    {
        points[i].x = (int)(cosf(i * 0.01256637061f) * rad + x);
        points[i].y = (int)(sinf(i * 0.01256637061f) * rad + y);
    }
    points[500] = points[0];
    HandleSdl(SDL_RenderDrawLines(renderer, points, 501));
}

static Uint8 color_ftou8(float v)
{
    if (v < 0)
        v = 0;
    if (v > 1)
        v = 1;
    return (Uint8)(v * 255);
}

static int scale_point(float value, int size)
{
    return (int)((value * 0.5f + 0.5f) * size);
}

static float unscale_point(int value, int size)
{
    return (float)value / size * 2 - 1;
}

static void draw_point_hv(float hue, float value, int x, int y)
{
    float r, g, b;
    hv_to_rgb(hue, value, &r, &g, &b);
    draw_point(color_ftou8(r), color_ftou8(g), color_ftou8(b), x, y);
}

static void
draw_line_hv(float hue, float value, float x1, float y1, float x2, float y2)
{
    float r, g, b;
    int width, height;
    HandleSdl(SDL_GetRendererOutputSize(renderer, &width, &height));
    hv_to_rgb(hue, value, &r, &g, &b);
    draw_line(color_ftou8(r),
              color_ftou8(g),
              color_ftou8(b),
              scale_point(x1, width),
              scale_point(y1, height),
              scale_point(x2, width),
              scale_point(y2, height));
}

static void draw_circ_hv(float hue, float value, float x, float y, float rad)
{
    float r, g, b;
    int width, height;
    HandleSdl(SDL_GetRendererOutputSize(renderer, &width, &height));
    hv_to_rgb(hue, value, &r, &g, &b);
    draw_circ(color_ftou8(r),
              color_ftou8(g),
              color_ftou8(b),
              scale_point(x, width),
              scale_point(y, height),
              (int)(rad * width / 2));
}

static void draw_pix(float time)
{
    struct star
    {
        float phase_x;
        float phase_y;
        float a_x;
        float b_x;
        float a_y;
        float b_y;
    };
    static struct star stars[200];
    static int star_init = 1;
    if (star_init)
    {
        star_init = 0;
        const int count = sizeof(stars) / sizeof(*stars);
        const int circle_count = count;
        for (int i = 0; i < circle_count; i++)
        {
            const float box_size = 0.9f;
            const float circle_size = 0.9f;

            float theta = randf();
            float which_circle = randf();
            float box = randf();
            float phase_shift_x = (randf() * 2 - 1) * 1.0f;
            float phase_shift_y = (randf() * 2 - 1) * 1.0f;
            // float phase_shift_x = 0;
            // float phase_shift_y = 0;
            // float box_side = randf();

            box = (box - 0.5f) * (float)(8 * M_PI);
            float box_x = 16 * sinf(box) * sinf(box) * sinf(box);
            float box_y = 13 * cosf(box) - 5 * cosf(2 * box) - 2 * cosf(3 * box)
                - cosf(4 * box) + 2;
            box_x *= 1.0f / 16;
            box_y *= -1.0f / 16;
            box_x *= box_size;
            box_y *= box_size;
            // float box_pos = box * (2 * box_size) - box_size;
            // float box_x =
            //     box_side < 0.5f ? box_pos : (box_side < 0.75f ? -box_size
            //                                                   : box_size);
            // float box_y =
            //     box_side >= 0.5f ? box_pos : (box_side < 0.25f ? -box_size
            //                                                    : box_size);

            float circle_x = cosf(theta * (float)(M_PI * 2)) * circle_size;
            float circle_y = sinf(theta * (float)(M_PI * 2)) * circle_size;
            if (which_circle > 0.5f)
            {
                // lines inside
                which_circle = (which_circle - 0.5f) * 2.0f;
                if (which_circle > 0.5f)
                {
                    // vert bar
                    which_circle = (which_circle - 0.5f) * 2.0f;
                    which_circle = which_circle * 2 - 1;
                    circle_x = 0;
                    circle_y = which_circle * circle_size;
                }
                else
                {
                    // inverse v
                    which_circle = which_circle * 2.0f;
                    which_circle = which_circle * 2 - 1;
                    circle_x = which_circle * circle_size / sqrtf(2);
                    circle_y = which_circle * circle_size / sqrtf(2);
                    if (which_circle < 0.0)
                    {
                        circle_y *= -1;
                    }
                }
            }

            float box_t = 0.0f;
            float circle_t = 0.5f;

            // u = cos(pi + p) * a + b
            // v = cos(0.0 + p) * a + b
            // pos = cos(phase + time) * a + b
            // x = cos(phase + time)
            // y = pos
            // a = (y2 - y1) / (x2 - x1)
            // b = y1 - a * x1
            float a_x = (circle_x - box_x)
                / (cosf(phase_shift_x) - cosf(phase_shift_x + (float)M_PI));
            float b_x = circle_x - a_x * cosf(phase_shift_x);
            float a_y = (circle_y - box_y)
                / (cosf(phase_shift_y) - cosf(phase_shift_y + (float)M_PI));
            float b_y = circle_y - a_y * cosf(phase_shift_y);
            stars[i].phase_x = phase_shift_x;
            stars[i].phase_y = phase_shift_y;
            stars[i].a_x = a_x;
            stars[i].b_x = b_x;
            stars[i].a_y = a_y;
            stars[i].b_y = b_y;
        }
    }
    int width, height;
    HandleSdl(SDL_GetRendererOutputSize(renderer, &width, &height));
    float t = time + 0.25f;
    t = fmodf(t, 1.0);
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            float px = unscale_point(x, width);
            float py = unscale_point(y, height);
            const int count = sizeof(stars) / sizeof(*stars);
            float r = 0, g = 0, b = 0;
            for (int i = 0; i < count; i++)
            {
                //for (int dt = -1; dt <= 1; dt++)
                {
                    //float theta = (t + dt) * (float)(2 * M_PI);
                    float theta = t * (float)(2 * M_PI);
                    float sx = cosf(theta + stars[i].phase_x) * stars[i].a_x + stars[i].b_x;
                    float sy = cosf(theta + stars[i].phase_y) * stars[i].a_y + stars[i].b_y;
                    if (sx < -1 || sx > 1 || sy < -1 || sy > 1)
                    {
                        continue;
                    }
                    float dx = sx - px;
                    float dy = sy - py;
                    float len2 = (dx * dx + dy * dy);
                    float bright = 1 - len2 * 1000;
                    if (bright < 0)
                        bright = 0;
                    float this_r, this_g, this_b;
                    //hv_to_rgb(stars[i].hue, bright, &this_r, &this_g, &this_b);
                    this_r = bright;
                    this_g = bright;
                    this_b = bright;
                    r += this_r;
                    g += this_g * (192 / 256.0);
                    b += this_b * (203 / 256.0);
                }
            }
            //printf("%f %f %f\n", r, g, b);
            draw_point(color_ftou8(r), color_ftou8(g), color_ftou8(b), x, y);
        }
    }
}

static void draw(float frame)
{
    draw_pix(frame);
    /*
    HandleSdl(SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255));
    HandleSdl(SDL_RenderClear(renderer));
    const int numlines = 16;
    for (int i_int = 0; i_int < numlines; i_int++)
    {
        float i = (float)i_int / numlines;
        float rotval = frame * i * 6.28318530718f * (numlines / 2);
        float scaleval = sqrtf(2);
        float x1 = cosf(rotval) * scaleval;
        float y1 = sinf(rotval) * scaleval;
        float x2 = cosf(rotval + i) * -scaleval;
        float y2 = sinf(rotval + i) * -scaleval;
        draw_line_hv(i * 2, 1, x1, y1, x2, y2);
    }
    */
    /*
        const int numlines = 32;
        float xOld = 0, yOld = 0;
        for (int i_int = 0; i_int < numlines; i_int++)
        {
            float xNew, yNew;
            float i = (float)i_int / numlines;
            float rotval = frame * (i_int * 2 + 1) * 6.28318530718f;
            float scaleval = 0.5f / (i_int * 2 + 1);
            xNew = cosf(rotval) * scaleval + xOld;
            yNew = sinf(rotval) * scaleval + yOld;
            draw_circ_hv(i * 2, 1, xOld, yOld, scaleval);
            draw_line_hv(i * 2, 1, xOld, yOld, xNew, yNew);
            xOld = xNew;
            yOld = yNew;
        }
        draw_line_hv(0, 1, -1, yOld, 1, yOld);
    */
}

static void do_window(void)
{
    Uint32 flags = 0;
    //Uint32 flags = SDL_WINDOW_RESIZABLE;
    SDL_Window
        *window = SDL_CreateWindow("Graphics", 100, 100, 200, 200, flags);
    FailIf(!window, "Could not create SDL window");
    renderer = SDL_CreateRenderer(window, -1, SDL_RENDERER_PRESENTVSYNC);
    FailIf(!renderer, "Could not create SDL renderer");
    int frame = 0;
    const int timescale = 60 * 16;
    const float duration = 5;
    Uint32 start = SDL_GetTicks();
    float last_time = 0.0f;
    while (1)
    {
        SDL_Event evnt;
        if (SDL_PollEvent(&evnt))
        {
            if (evnt.type == SDL_QUIT)
            {
                break;
            }
        }
        else
        {
            frame++;
            if (frame == timescale)
            {
                puts("Loop");
                //frame = 0;
            }
            Uint32 now = SDL_GetTicks();
            float time = (now - start) / (1000.0f * duration);
            time = fmodf(time, 1.0f);
            if (time < last_time)
            {
                printf("Tick\n");
            }
            last_time = time;
            //draw(frame / (float)timescale);
            draw(time);
            SDL_RenderPresent(renderer);
        }
    }
    SDL_DestroyRenderer(renderer);
}

static void do_gif(void)
{
    const int FrameSize = 250;
    const int NumFrames = 200;
    SDL_Surface
        *surf = SDL_CreateRGBSurface(0, FrameSize, FrameSize, 32, 0, 0, 0, 0);
    FailIf(!surf, "Could not create SDL surface");
    renderer = SDL_CreateSoftwareRenderer(surf);
    FailIf(!renderer, "Could not create SDL software renderer");
    for (int i = 1; i <= NumFrames; i++)
    {
        draw((i - 1) / (float)NumFrames);
        char fname[100];
        snprintf(fname, sizeof(fname), "%d.bmp", i);
        SDL_SaveBMP(surf, fname);
    }
    SDL_FreeSurface(surf);
}

int main(int argc, const char *const *argv)
{
    HandleSdl(SDL_Init(SDL_INIT_VIDEO));
    int gif = 0;
    for (int i = 1; i < argc; i++)
    {
        if (!strcmp(argv[i], "--gif"))
        {
            gif = 1;
        }
    }
    if (gif)
    {
        do_gif();
    }
    else
    {
        do_window();
    }
    return 0;
}
