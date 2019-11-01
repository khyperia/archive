use sdl2::event::Event;
use sdl2::event::WindowEvent;
use sdl2::init;
use sdl2::pixels::PixelFormatEnum;
use std::error::Error;

pub fn display<F: FnMut(usize, usize, &mut [u8])>(mut render: F) -> Result<(), Box<Error>> {
    let scale = 1;
    let mut width = 1 << 10;
    let mut height = width;
    let sdl = init()?;
    let video = sdl.video()?;
    //let window = video.window("Scopie", width, height).resizable().build()?;
    let window = video
        .window("Scopie", width * scale, height * scale)
        .build()?;
    let mut canvas = window.into_canvas().present_vsync().build()?;
    let creator = canvas.texture_creator();
    let mut texture = creator.create_texture_streaming(PixelFormatEnum::RGBX8888, width, height)?;
    let mut event_pump = sdl.event_pump()?;
    let mut data = vec![0; width as usize * height as usize * 4];
    loop {
        while let Some(event) = event_pump.poll_event() {
            match event {
                Event::Quit { .. } => return Ok(()),
                Event::Window {
                    win_event: WindowEvent::Resized(new_width, new_height),
                    ..
                } if new_width > 0 && new_height > 0 =>
                {
                    width = new_width as u32 / scale;
                    height = new_height as u32 / scale;
                }
                _ => (),
            }
        }
        {
            let new_size = width as usize * height as usize * 4;
            if data.len() != new_size {
                data.resize(new_size, 0);
                texture = creator.create_texture_streaming(None, width, height)?;
            }
            render(width as usize, height as usize, &mut data);
            texture.update(None, &data, width as usize * 4)?;
        }
        canvas.copy(&texture, None, None)?;
        canvas.present();
    }
}
