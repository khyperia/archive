use cgmath;
use cgmath::{Matrix4, Point3, vec3};
use chunk::gen_mbox;
use glium::draw_parameters::{BackfaceCullingMode, PolygonMode};
use glium::glutin::{Api, ContextBuilder, ControlFlow, ElementState, Event, EventsLoop, GlProfile,
                    GlRequest, KeyboardInput, VirtualKeyCode, WindowBuilder, WindowEvent};
use glium::index::PrimitiveType;
use glium::{Depth, DepthTest, Display, DrawParameters, IndexBuffer, Program, Surface, VertexBuffer};
use keyboard::{Input, Settings};
use std::mem;
use std::slice;
use std::time::Instant;

pub fn reinterpret_cast_slice<S, T>(input: &[S]) -> &[T] {
    let length_in_bytes = input.len() * mem::size_of::<S>();
    let desired_length = length_in_bytes / mem::size_of::<T>();
    unsafe { slice::from_raw_parts(input.as_ptr() as *const T, desired_length) }
}

#[derive(Copy, Clone, Debug)]
#[repr(C)]
struct Vertex {
    position: [f32; 3],
    normal: [f32; 3],
}

implement_vertex!(Vertex, position, normal);

struct GlChunk {
    vertices: VertexBuffer<Vertex>,
    indices: IndexBuffer<u32>,
}

struct FlingWindow {
    display: Display,
    program: Program,
    input: Input,
    settings: Settings,
    wireframe: bool,
    mesh: GlChunk,
}

impl FlingWindow {
    fn new() -> (FlingWindow, EventsLoop) {
        let generated = gen_mbox();

        let events_loop = EventsLoop::new();
        let window = WindowBuilder::new()
            .with_title("mandelbox")
            .with_dimensions((1024, 768).into());
        let context = ContextBuilder::new()
            .with_vsync(true)
            .with_gl_profile(GlProfile::Core)
            .with_gl(GlRequest::Specific(Api::OpenGl, (3, 3)))
            .with_depth_buffer(24);
        let display =
            Display::new(window, context, &events_loop).expect("failed to create display");

        let mesh = Self::upload_chunk(&display, generated);

        let input = Input::new();
        let settings = Settings::new();

        let wireframe = false;

        let program = program!(&display,
            330 => {
                vertex: "#version 330
                    uniform mat4 model_view_projection;

                    layout(location=0) in vec3 position;
                    layout(location=1) in vec3 normal;

                    out vec3 vNormal;

                    void main() {
                        gl_Position = model_view_projection * vec4(position, 1.0);
                        vNormal = normal;
                    }
                ",
                fragment: "#version 330
                    in vec3 vNormal;

                    layout(location=0) out vec4 color;

                    vec3 hemisphere(vec3 normal) {
                        const vec3 light = vec3(0.1, -1.0, 0.0);
                        float NdotL = dot(normal, light)*0.5 + 0.5;
                        return mix(vec3(0.886, 0.757, 0.337), vec3(0.518, 0.169, 0.0), NdotL);
                    }

                    void main() {
                        color = vec4(hemisphere(normalize(vNormal)), 1.0);
                    }
                "
            },
        ).expect("failed to compile shaders");

        (
            FlingWindow {
                display,
                program,
                input,
                settings,
                wireframe,
                mesh,
            },
            events_loop,
        )
    }

    fn run(&mut self, mut events_loop: EventsLoop) {
        loop {
            let mut stop = false;
            events_loop.poll_events(|event| {
                let result = self.handle_event(&event);
                if result == ControlFlow::Break {
                    stop = true;
                }
            });
            if stop {
                break;
            }
            self.draw();
        }
    }

    fn handle_event(&mut self, event: &Event) -> ControlFlow {
        let event = match event {
            Event::WindowEvent { event, .. } => event,
            _ => return ControlFlow::Continue,
        };
        match event {
            WindowEvent::CloseRequested => return ControlFlow::Break,
            WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state,
                        virtual_keycode: Some(virtual_keycode),
                        ..
                    },
                ..
            } => match state {
                ElementState::Pressed => {
                    if *virtual_keycode == VirtualKeyCode::T {
                        self.wireframe = !self.wireframe;
                    }
                    self.input.key_down(*virtual_keycode, Instant::now());
                    ControlFlow::Continue
                }
                ElementState::Released => {
                    self.input
                        .key_up(*virtual_keycode, Instant::now(), &mut self.settings);
                    ControlFlow::Continue
                }
            },
            WindowEvent::Refresh => ControlFlow::Continue,
            _ => ControlFlow::Continue,
        }

        //// Get the current sensor state
        //let poses = vr_context.compositor().unwrap().wait_get_poses();

        //// Submit eye textures
        //vr_context.compositor().unwrap().submit(..);
    }

    fn draw(&mut self) {
        self.input.integrate(&mut self.settings);
        let mut surface = self.display.draw();
        surface.clear_color_and_depth((0.024, 0.184, 0.337, 0.0), 1.0);

        let pos = self.settings.get_vec("pos_x", "pos_y", "pos_z").unwrap();
        let look = self.settings.get_vec("look_x", "look_y", "look_z").unwrap();
        let lookat = pos + look;
        let up = self.settings.get_vec("up_x", "up_y", "up_z").unwrap();
        let fov = self.settings.get_f32("fov").unwrap();
        //let focal_distance = self.settings.get_f32("focal_distance").unwrap();

        let (view_w, view_h) = self.display.get_framebuffer_dimensions();
        let aspect = view_w as f32 / view_h as f32;
        let projection = cgmath::perspective(cgmath::Deg(fov * 90.0), aspect, 0.01, 1000.0);

        let view = Matrix4::look_at(
            Point3::new(pos.x, pos.y, pos.z),
            Point3::new(lookat.x, lookat.y, lookat.z),
            vec3(up.x, up.y, up.z),
        );

        let polygon_mode = if self.wireframe {
            PolygonMode::Line
        } else {
            PolygonMode::Fill
        };

        let draw_parameters = DrawParameters {
            depth: Depth {
                test: DepthTest::IfLess,
                write: true,
                ..Default::default()
            },
            point_size: Some(8.0),
            polygon_mode,
            backface_culling: BackfaceCullingMode::CullCounterClockwise,
            ..Default::default()
        };

        //        pub fn projection_matrix(
        //    &self,
        //    eye: Eye,
        //    near_z: f32,
        //    far_z: f32
        //) -> [[f32; 4]; 4]
        for projection in [projection].iter() {
            let uniforms = uniform! {
                model_view_projection: Into::<[[f32; 4]; 4]>::into(projection * view),
            };

            surface
                .draw(
                    &self.mesh.vertices,
                    &self.mesh.indices,
                    &self.program,
                    &uniforms,
                    &draw_parameters,
                )
                .expect("failed to draw to surface");
        }

        surface.finish().expect("failed to finish rendering frame");
    }

    fn upload_chunk(display: &Display, gen_result: (Vec<f32>, Vec<u32>)) -> GlChunk {
        let vertex_buffer: VertexBuffer<Vertex> =
            VertexBuffer::new(display, reinterpret_cast_slice(&gen_result.0))
                .expect("failed to create vertex buffer");

        let index_buffer: IndexBuffer<u32> =
            IndexBuffer::new(display, PrimitiveType::TrianglesList, &gen_result.1)
                .expect("failed to create index buffer");

        GlChunk {
            //coords: chunk.coords.clone(),
            vertices: vertex_buffer,
            indices: index_buffer,
        }
    }
}

pub fn render_thing() {
    let (mut window, events) = FlingWindow::new();
    window.run(events)
}
