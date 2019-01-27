use isosurface::linear_hashed_marching_cubes::LinearHashedMarchingCubes;
use isosurface::source::CentralDifference;
use isosurface::source::Source;
use mandelbox;

struct Mandelbox {}

const LOD: usize = 6;
const DELTA: f32 = 0.005;
const SCALE: f32 = 2.1;

impl Source for Mandelbox {
    fn sample(&self, x: f32, y: f32, z: f32) -> f32 {
        let result = mandelbox::de(x * SCALE, y * SCALE, z * SCALE);
        if result.is_nan() {
            println!("NaN at: {} {} {}", x, y, z);
            0.0
        } else {
            result - DELTA
        }
    }
}

pub fn gen_mbox() -> (Vec<f32>, Vec<u32>) {
    let mut pos_normal = Vec::new();
    let mut index = Vec::new();

    let source = CentralDifference::new(Box::new(Mandelbox {}));
    let mut linear_hashed_marching_cubes = LinearHashedMarchingCubes::new(LOD);
    linear_hashed_marching_cubes.extract_with_normals(&source, &mut pos_normal, &mut index);

    return (pos_normal, index);
}
