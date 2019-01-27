#include <iostream>
#include <fstream>
#include <ctime>
#include <vector>
#include <stdexcept>
#include <limits>
#include <thread>
#include <lucamapi.h>
#include <fitsio.h>
#include <SDL2/SDL.h>
#include <SDL2/SDL_ttf.h>

std::runtime_error error(int status)
{
    return std::runtime_error(std::string("FITS file library error: code ") + std::to_string(status));
}

void WriteFits(std::string filename, std::vector<uint16_t>& pixels, int width)
{
    fitsfile* file = nullptr;
    int status = 0;
    remove(filename.c_str());
    if (fits_create_file(&file, filename.c_str(), &status))
        throw error(status);
    long axes[] = { width, (long)pixels.size() / width };
    if (fits_create_img(file, USHORT_IMG, 2, axes, &status))
        throw error(status);
    std::vector<long> firstpix(2);
    for (unsigned long i = 0; i < firstpix.size(); i++)
        firstpix[i] = 1;
    if (fits_write_pix(file, TUSHORT, firstpix.data(), pixels.size(), pixels.data(), &status))
        throw error(status);
    if (fits_close_file(file, &status))
        throw error(status);
    std::cout << "Saved " << filename << std::endl;
}

inline bool fileExists(std::string const& name) {
    std::ifstream f(name.c_str());
    if (f.good()) {
        f.close();
        return true;
    } else {
        f.close();
        return false;
    }   
}

void WriteFits(std::vector<uint16_t> pixels, int width)
{
    time_t rawtime;
    time(&rawtime);
    auto timeinfo = localtime(&rawtime);
    char buffer[100];
    std::strftime(buffer, 100, "%Y-%m-%d_%I-%M-%S", timeinfo);
    auto prefix = "image-" + std::string(buffer);

    std::string file;
    int i = 0;
    do
    {
        if (i == 0)
            file = prefix + ".fits";
        else
            file = prefix + "." + std::to_string(i++) + ".fits";
    } while (fileExists(file));
    WriteFits(file, pixels, width);
}

static volatile bool closeLucamCamera = false;
static volatile bool beeping = false;

class LucamCamera
{
    _lucam* camera;
    int width;
    int height;
    public:
    volatile int numImagesTake = 0;
    volatile double exposureLive = 1;
    volatile double exposureImage = 10;
    volatile bool refreshExposure = false;

    LucamCamera()
    {
        _lucam_version_info versionInfo;
        int versionsCount = 1;
        if (lucam_enum_cameras(&versionInfo, &versionsCount))
            throw std::runtime_error("Lucam error on lucam_enum_cameras");
        if (versionsCount == 0)
            throw std::runtime_error("No Lumenera cameras found");
        camera = lucam_camera_open(versionInfo.minor);
        if (camera == nullptr)
            throw std::runtime_error("Lucam error on lucam_camera_open");
        if (lucam_camera_reset(camera))
            throw std::runtime_error("Lucam error on lucam_camera_reset");
        if (lucam_set_mode(camera, LUCAM_MODE_STILL_SW_TRIGGER))
            throw std::runtime_error("Lucam error on lucam_set_mode");
        _lucam_frame_format format;
        if (lucam_get_still_format(camera, &format))
            throw std::runtime_error("Lucam error on lucam_get_still_format");
        format.binningX = 1;
        format.binningY = 1;
        format.pixelformat = LUCAM_PIXEL_FORMAT_16BITS;
        width = format.width;
        height = format.height;
        if (lucam_set_still_format(camera, &format))
            throw std::runtime_error("Lucam error on lucam_set_still_format");
        
        SetProperty(LUCAM_PROP_TIMEOUT, -1);
        //SetProperty(LUCAM_PROP_GAMMA, 100);
        //SetProperty(LUCAM_PROP_CONTRAST, 100);
        //SetProperty(LUCAM_PROP_BRIGHTNESS, 100);
        SetProperty(LUCAM_PROP_STILL_GAIN, GAIN_MULT_FACTOR);
        SetProperty(LUCAM_PROP_STILL_EXPOSURE, 1000000);
    }

    template<typename Callback>
        void StreamLoop(Callback const& callback)
        {
            auto buffercount = lucam_get_buffer_count(camera);
            if (lucam_enable_streaming(camera))
                throw std::runtime_error("Lucam error on lucam_enable_streaming");
            if (lucam_start_capture_to_buffer(camera, 0, LUCAM_PIXEL_FORMAT_16BITS))
                throw std::runtime_error("Lucam error on lucam_start_capture_to_buffer");
            if (lucam_start_streaming(camera))
                throw std::runtime_error("Lucam error on lucam_start_streaming");

            struct Raii {
                _lucam* camera;
                Raii(_lucam* camera) : camera(camera) {}
                ~Raii()
                {
                    if (lucam_stop_streaming(camera))
                        throw std::runtime_error("Lucam error on lucam_stop_streaming");
                    if (lucam_disable_streaming(camera))
                        throw std::runtime_error("Lucam error on lucam_disable_streaming");
                }
            } raii(camera);
            auto size = lucam_get_still_frame_size(camera) / sizeof(uint16_t);

            std::cout << "Camera thread running with " << buffercount << " buffers" << std::endl;
            int oldImageCount = numImagesTake;
            for (int buffer = 0; !closeLucamCamera; buffer = (buffer + 1) % buffercount)
            {
                if (lucam_start_capture_to_buffer(camera,
                            (buffer + 1) % buffercount,
                            LUCAM_PIXEL_FORMAT_16BITS))
                    throw std::runtime_error("Lucam error on lucam_start_capture_to_buffer");
                if (lucam_software_trigger(camera) < 0)
                    throw std::runtime_error("Lucam error on lucam_software_trigger");
                if (lucam_wait_for_capture_to_buffer(camera, buffer))
                    throw std::runtime_error("Lucam error on lucam_wait_for_capture_to_buffer");
                auto address = reinterpret_cast<uint16_t*>(lucam_get_buffer_address(camera, buffer));
                callback(std::vector<uint16_t>(address, address + size), width);
                if (numImagesTake > 0 && oldImageCount != 0)
                {
                    WriteFits(std::vector<uint16_t>(address, address + size), width);
                    numImagesTake--;
                    if (numImagesTake == 0)
                        beeping = true;
                }
                if ((oldImageCount == 0) != (numImagesTake == 0))
                {
                    refreshExposure = true;
                }
                if (refreshExposure)
                {
                    refreshExposure = false;
                    if (numImagesTake == 0)
                        SetProperty(LUCAM_PROP_STILL_EXPOSURE, (LONG)(1000000 * exposureLive));
                    else
                        SetProperty(LUCAM_PROP_STILL_EXPOSURE, (LONG)(1000000 * exposureImage));
                }
                oldImageCount = numImagesTake;
            }
            std::cout << "Camera thread shut down" << std::endl;
        }

    void SetProperty(int property, LONG value)
    {
        LONG oldValue = 0;
        ULONG oldFlags = 0;
        if (lucam_property_get(camera, property, &oldValue, &oldFlags))
            throw std::runtime_error("Failed to get property " + std::to_string(property));
        LONG min, max, rvalue;
        ULONG caps;
        if (lucam_property_get_range(camera, property, &min, &max, &rvalue, &caps))
            throw std::runtime_error("Lucam error in lucam_property_get_range");
        std::cout << min << " < " << value << " < " << max << std::endl;
        // not sure why it's <0
        if (lucam_property_set(camera, property, value, oldFlags) < 0)
            throw std::runtime_error("Failed to set property " + std::to_string(property));
    }

    ~LucamCamera()
    {
        lucam_camera_close(camera);
    }
};

struct GuiSettings
{
    int currentSetting = 0;
    static const int numSettings = 5;
    int settings[numSettings] = {
        0,
        0,
        -1,
        0,
        10
    };

    enum
    {
        GAMMA = 0,
        DARKTHRESH = 1,
        ZOOM = 2,
        LIVEEXPOSURE = 3,
        IMAGEEXPOSURE = 4
    };

    double GetGamma() const
    {
        return exp(settings[GAMMA] / 32.0);
    }

    double GetDarkThresh() const
    {
        return settings[DARKTHRESH] * 32.0;
    }

    double GetZoom() const
    {
        return settings[ZOOM] < 0 ? settings[ZOOM] : settings[ZOOM] * 8;
    }

    double GetLiveExposure() const
    {
        return exp(settings[LIVEEXPOSURE] / 32.0);
    }

    double GetImageExposure() const
    {
        return std::max(settings[IMAGEEXPOSURE], 1);
    }

    void IncreaseSetting()
    {
        settings[currentSetting]++;
    }

    void DecreaseSetting()
    {
        settings[currentSetting]--;
    }

    void CycleForward()
    {
        currentSetting++;
        while (currentSetting >= numSettings)
            currentSetting -= numSettings;
    }

    void CycleBack()
    {
        currentSetting--;
        while (currentSetting < 0)
            currentSetting += numSettings;
    }
};

void DrawString(SDL_Renderer* renderer, TTF_Font* font, std::string str, int x, int y, bool outline)
{
    SDL_Color color;
    color.r = 255;
    color.g = 0;
    color.b = 0;
    color.a = 255;
    auto surf = TTF_RenderText_Blended(font, str.c_str(), color);
    if (!surf)
        throw std::runtime_error(SDL_GetError());
    auto texture = SDL_CreateTextureFromSurface(renderer, surf);
    if (!texture)
        throw std::runtime_error(SDL_GetError());
    SDL_Rect dest;
    dest.x = x;
    dest.y = y;
    dest.w = surf->w;
    dest.h = surf->h;
    if (SDL_RenderCopy(renderer, texture, nullptr, &dest))
        throw std::runtime_error(SDL_GetError());
    SDL_DestroyTexture(texture); // void
    SDL_FreeSurface(surf); // void
    if (outline)
    {
        SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, color.a);
        SDL_RenderDrawRect(renderer, &dest);
    }
}

void DrawSettings(SDL_Renderer* renderer, TTF_Font* font, GuiSettings const& settings, LucamCamera const& camera)
{
    int y = -10;
    const int yStep = 15;
    DrawString(renderer, font, "Gamma: " + std::to_string(settings.GetGamma()),
            10, y += yStep, settings.currentSetting == 0);
    DrawString(renderer, font, "Dark thresh: " + std::to_string(settings.GetDarkThresh()),
            10, y += yStep, settings.currentSetting == 1);
    DrawString(renderer, font, "Zoom: " + std::to_string(settings.GetZoom()),
            10, y += yStep, settings.currentSetting == 2);
    DrawString(renderer, font, "Live exposure: " + std::to_string(settings.GetLiveExposure()),
            10, y += yStep, settings.currentSetting == 3);
    DrawString(renderer, font, "Image exposure: " + std::to_string(settings.GetImageExposure()),
            10, y += yStep, settings.currentSetting == 4);
    DrawString(renderer, font, "Num images capturing: " + std::to_string(camera.numImagesTake),
            10, y += yStep, false);
}

void DispPixels(SDL_Renderer* renderer, SDL_Texture*& texture,
        std::vector<uint16_t> const& pixels, int width, GuiSettings const& settings,
        int winWidth, int winHeight)
{
    int texAccess, texWidth, texHeight;
    Uint32 texFormat;
    if (texture)
        if (SDL_QueryTexture(texture, &texFormat, &texAccess, &texWidth, &texHeight))
            throw std::runtime_error(SDL_GetError());
    if (texture == nullptr || texWidth != width || texHeight != (int)(pixels.size() / width))
    {
        if (texture)
            SDL_DestroyTexture(texture);
        texture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_RGB24,
                SDL_TEXTUREACCESS_STREAMING, width, pixels.size() / width);
        if (texture == nullptr)
            throw std::runtime_error(SDL_GetError());
    }
    uint8_t* rawpixels = nullptr;
    int pitch = 0;
    if (SDL_LockTexture(texture, nullptr, reinterpret_cast<void**>(&rawpixels), &pitch))
        throw std::runtime_error(SDL_GetError());
    int height = pixels.size() / width;

    double darkThresh = settings.GetDarkThresh();
    double gamma = settings.GetGamma();
    double zoom = settings.GetZoom();

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            uint16_t pixel = pixels[y * width + x];
            double value = ((double)pixel - darkThresh) / 65535;
            value = pow(value, gamma);
            uint8_t pixcol = (uint8_t)(value * 255);
            int index = y * pitch + x * 3;
            rawpixels[index + 0] = pixcol;
            rawpixels[index + 1] = pixcol;
            rawpixels[index + 2] = pixcol;
            if ((y == height / 2 || x == width / 2) && zoom >= 0)
            {
                rawpixels[index] = 255;
            }
        }
    }
    SDL_UnlockTexture(texture); // void

    SDL_Rect destRect;
    destRect.x = 0;
    destRect.y = 0;
    auto realWinWidth = std::min(winWidth, (int)((long)winHeight * width * width / pixels.size()));
    destRect.w = realWinWidth;
    destRect.h = (int)((long)realWinWidth * pixels.size() / (width * width));
    if (zoom < 0)
    {
        if (SDL_RenderCopy(renderer, texture, nullptr, &destRect))
            throw std::runtime_error(SDL_GetError());
    }
    else
    {
        SDL_Rect srcRect;
        srcRect.x = (int)zoom;
        srcRect.y = (int)zoom * height / width;
        srcRect.w = width - (int)zoom * 2;
        srcRect.h = height - (int)zoom * 2 * height / width;
        if (SDL_RenderCopy(renderer, texture, &srcRect, &destRect))
            throw std::runtime_error(SDL_GetError());
    }
}

bool DispLoop(std::vector<uint16_t> const& pixels, int width,
        GuiSettings& settings, LucamCamera& camera)
{
    static SDL_Window* window = nullptr;
    static SDL_Renderer* renderer = nullptr;
    static TTF_Font* font = nullptr;
    static SDL_Texture* texture = nullptr;
    if (!window)
    {
        if (SDL_Init(SDL_INIT_EVERYTHING))
            throw std::runtime_error(SDL_GetError());
        if (TTF_Init())
            throw std::runtime_error(TTF_GetError());
        window = SDL_CreateWindow("LuDisp", 100, 100, 800, 600, SDL_WINDOW_RESIZABLE);
        if (!window)
            throw std::runtime_error(SDL_GetError());
        renderer = SDL_CreateRenderer(window, -1, SDL_RENDERER_PRESENTVSYNC);
        if (!renderer)
            throw std::runtime_error(SDL_GetError());
        font = TTF_OpenFont("/usr/share/fonts/OTF/Inconsolata.otf", 14);
        if (!font)
            throw std::runtime_error(TTF_GetError());
    }

    if (SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255))
        throw std::runtime_error(SDL_GetError());
    if (SDL_RenderClear(renderer))
        throw std::runtime_error(SDL_GetError());

    int winWidth, winHeight;
    SDL_GetWindowSize(window, &winWidth, &winHeight);

    DispPixels(renderer, texture, pixels, width, settings, winWidth, winHeight);

    DrawSettings(renderer, font, settings, camera);

    SDL_RenderPresent(renderer);
    SDL_Event event;
    while (SDL_PollEvent(&event))
    {
        if (event.type == SDL_QUIT)
        {
            SDL_DestroyWindow(window);
            SDL_DestroyRenderer(renderer);
            TTF_CloseFont(font);
            if (texture)
                SDL_DestroyTexture(texture);
            window = nullptr;
            renderer = nullptr;
            font = nullptr;
            texture = nullptr;
            return true;
        }
        if (event.type == SDL_KEYDOWN)
        {
            switch (event.key.keysym.sym)
            {
                case SDLK_RIGHT:
                    settings.IncreaseSetting();
                    break;
                case SDLK_LEFT:
                    settings.DecreaseSetting();
                    break;
                case SDLK_DOWN:
                    settings.CycleForward();
                    break;
                case SDLK_UP:
                    settings.CycleBack();
                    break;
                case SDLK_s:
                    camera.numImagesTake++;
                    break;
                case SDLK_b:
                    beeping = !beeping;
                    break;
            }
            if (settings.currentSetting == GuiSettings::LIVEEXPOSURE ||
                    settings.currentSetting == GuiSettings::IMAGEEXPOSURE)
            {
                camera.exposureImage = settings.GetImageExposure();
                camera.exposureLive = settings.GetLiveExposure();
                camera.refreshExposure = true;
            }
        }
    }
    return false;
}

int main(int, char*[])
{
    try
    {
        std::cout.setf(std::ios_base::unitbuf); // for beeping
        std::vector<uint16_t> pixels(255 * 255);
        int pixelsWidth = 255;
        for (unsigned long i = 0; i < pixels.size(); i++)
        {
            pixels[i] = i;
        }
        GuiSettings settings;
        LucamCamera camera;
        std::thread cameraThread([&]()
                {
                camera.StreamLoop(
                        [&](std::vector<uint16_t> const& vec, int imgWidth)
                        {
                        pixels = vec;
                        pixelsWidth = imgWidth;
                        });
                });
        auto lastBeep = time(nullptr);
        while (true)
        {
            auto thisClock = time(nullptr);
            if (difftime(thisClock, lastBeep) > 4 && beeping)
            {
                lastBeep = thisClock;
                std::cout << '\a';
            }
            if (DispLoop(pixels, pixelsWidth, settings, camera))
                break;
        }
        closeLucamCamera = true;
        cameraThread.join();
    }
    catch (std::exception const& ex)
    {
        std::cout << "Exception!" << std::endl;
        std::cout << ex.what() << std::endl;
        getchar();
    }
}
