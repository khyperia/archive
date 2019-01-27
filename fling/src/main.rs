extern crate cgmath;
extern crate isosurface;
extern crate png;
//extern crate openvr;
#[macro_use]
extern crate glium;

mod chunk;
mod keyboard;
mod mandelbox;
mod number;
mod render;

fn main() {

    render::render_thing();
}
