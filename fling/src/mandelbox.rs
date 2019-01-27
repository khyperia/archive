use number::Vector3;
use number::clamp;

pub struct Cfg {
    folding_limit: f32,
    min_radius_2: f32,
    fixed_radius_2: f32,
    bailout: f32,
    scale: f32,
    max_iters: u32,
}

fn boxfold(cfg: &Cfg, z: Vector3<f32>) -> (Vector3<f32>) {
    z.clamp(-cfg.folding_limit, cfg.folding_limit) * 2.0 - z
}

fn spherefold(cfg: &Cfg, z: Vector3<f32>, dz: f32) -> (Vector3<f32>, f32) {
    let factor = cfg.fixed_radius_2 / clamp(z.len2(), cfg.min_radius_2, cfg.fixed_radius_2);
    (z * factor, dz * factor)
}

fn scale(cfg: &Cfg, z: Vector3<f32>, dz: f32) -> (Vector3<f32>, f32) {
    let scale = cfg.scale;
    (z * scale, dz * scale.abs())
}

fn offset(z: Vector3<f32>, dz: f32, offset: Vector3<f32>) -> (Vector3<f32>, f32) {
    (z + offset, dz + 1.0)
}

fn mandelbox_one(cfg: &Cfg, z: Vector3<f32>, dz: f32, offset_value: Vector3<f32>) -> (Vector3<f32>, f32) {
    let z = boxfold(cfg, z);
    let (z, dz) = spherefold(cfg, z, dz);
    let (z, dz) = scale(cfg, z, dz);
    let (z, dz) = offset(z, dz, offset_value);
    (z, dz)
}

pub fn mandelbox(cfg: &Cfg, offset: Vector3<f32>) -> f32 {
    let mut z = offset;
    let mut dz = 1.0;
    let mut n = cfg.max_iters.max(1);
    while z.len2() < cfg.bailout && n > 0 {
        let (new_z, new_dz) = mandelbox_one(cfg, z, dz, offset);
        z = new_z;
        dz = new_dz;
        n -= 1;
    }
    return z.len2().sqrt() / dz;
}

pub fn de(x: f32, y: f32, z: f32) -> f32 {
    mandelbox(
        &Cfg {
            folding_limit: 1.0,
            min_radius_2: 0.125,
            fixed_radius_2: 1.0,
            bailout: (1 << 10) as f32,
            scale: -2.0,
            max_iters: 1 << 8,
        },
        Vector3::new(x, y, z),
    )
}
