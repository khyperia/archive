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
        float hue, x, y, dx, dy;
    };
    static struct star stars[100];
    static int star_init = 1;
    if (star_init)
    {
        star_init = 0;
        const int count = sizeof(stars) / sizeof(*stars);
        const int circle_count = count;
        for (int i = 0; i < circle_count; i++)
        {
            float sx, sy, dx, dy;
            while (1)
            {
                const float box_size = 0.9f;
                const float circle_size = 0.9f;

                float theta = randf();
                float box = randf();
                // float box_side = randf();
                float which_box = randf();

                box = (box - 0.5f) * (float)(8 * M_PI);
                float box_x = 16 * sinf(box) * sinf(box) * sinf(box);
                float box_y = 13 * cosf(box) - 5 * cosf(2 * box) - 2 * cosf(3 * box) - cosf(4 * box) + 2;
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

                float begin_t = -0.5f;
                float end_t = 1.5f;
                float box1_t = 0.0f;
                float circle_t = 0.5f;
                float box2_t = 1.0f;

                float box_t = which_box < 0.5f ? box1_t : box2_t;
                float off1_t = which_box < 0.5f ? begin_t : box1_t;
                float off2_t = which_box < 0.5f ? box2_t : end_t;

                dx = (circle_x - box_x) / (circle_t - box_t);
                dy = (circle_y - box_y) / (circle_t - box_t);
                sx = circle_x - dx * circle_t;
                sy = circle_y - dy * circle_t;

                float off1_x = sx + dx * off1_t;
                float off1_y = sy + dy * off1_t;
                float off2_x = sx + dx * off2_t;
                float off2_y = sy + dy * off2_t;
                if ((off1_x > -1 && off1_x < 1 && off1_y > -1 && off1_y < 1)
                    || (off2_x > -1 && off2_x < 1 && off2_y > -1 && off2_y < 1))
                {
                    continue;
                }
                break;
            }
            stars[i].hue = i / (float)circle_count;
            stars[i].x = sx;
            stars[i].y = sy;
            stars[i].dx = dx;
            stars[i].dy = dy;
        }
    }
    int width, height;
    HandleSdl(SDL_GetRendererOutputSize(renderer, &width, &height));
    float t = time;
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
                for (int dt = -1; dt <= 1; dt++)
                {
                    float sx = stars[i].x + stars[i].dx * (t + dt);
                    float sy = stars[i].y + stars[i].dy * (t + dt);
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
                    hv_to_rgb(stars[i].hue, bright, &this_r, &this_g, &this_b);
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
    const float duration = 8;
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
