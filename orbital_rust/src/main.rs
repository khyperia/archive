extern crate rand;
extern crate sdl2;

mod display;
mod rk4;
mod world;

fn main() {
    let mut universe = world::Universe::new();
    match display::display(move |width, height, px| {
        universe.step();
        universe.draw(width, height, px)
    }) {
        Ok(()) => (),
        Err(err) => eprintln!("{}", err),
    }
}
