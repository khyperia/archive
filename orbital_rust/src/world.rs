use rand::{thread_rng, Rng, prelude::ThreadRng};
use rk4;
use std::ops::Add;
use std::ops::Mul;
use std::ops::Sub;

pub type F = f32;

const COLOR_VARIANCE: F = 1.4;
const SATURATION: F = 0.5;
const G: F = 1.0;
const SUN_MASS: F = 100.0;
const NUM_STARS: usize = 6;
const POS_GEN: F = 40.0;
const RETAIN_LIMIT: F = 50.0;
const ACCELERATE_LIMIT: F = 2.0;
const SCREEN_SIZE: F = 50.0;

#[derive(Clone, Copy)]
pub struct Vector {
    x: F,
    y: F,
}

impl Vector {
    fn new(x: F, y: F) -> Vector {
        Vector { x, y }
    }

    fn len2(self) -> F {
        self.x * self.x + self.y * self.y
    }

    fn len(self) -> F {
        self.len2().sqrt()
    }

    fn normalized(self) -> Self {
        self * (1.0 / self.len())
    }
}

impl Add for Vector {
    type Output = Self;
    fn add(self, other: Vector) -> Vector {
        Vector::new(self.x + other.x, self.y + other.y)
    }
}

impl Sub for Vector {
    type Output = Self;
    fn sub(self, other: Vector) -> Vector {
        Vector::new(self.x - other.x, self.y - other.y)
    }
}

impl Mul<F> for Vector {
    type Output = Self;
    fn mul(self, other: F) -> Vector {
        Vector::new(self.x * other, self.y * other)
    }
}

#[derive(Clone, Copy)]
pub struct VecMass {
    pub vec: Vector,
    pub mass: F,
}

impl VecMass {
    pub fn new(vec: Vector, mass: F) -> Self {
        VecMass { vec, mass }
    }
}

impl Add for VecMass {
    type Output = Self;
    fn add(self, other: VecMass) -> Self {
        VecMass::new(self.vec + other.vec, self.mass + other.mass)
    }
}

// impl Sub for VecMass {
//     type Output = Self;
//     fn sub(self, other: VecMass) -> Self {
//         VecMass::new(self.vec - other.vec, self.mass - other.mass)
//     }
// }

impl Mul<F> for VecMass {
    type Output = Self;
    fn mul(self, other: F) -> Self {
        VecMass::new(self.vec * other, self.mass * other)
    }
}

pub struct Star {
    pub pos: Vector,
    pub vel: Vector,
    pub mass: F,
}

impl Star {
    fn new(pos: Vector, vel: Vector, mass: F) -> Star {
        Star { pos, vel, mass }
    }
}

pub struct Universe {
    rng: ThreadRng,
    pub stars: Vec<Star>,
}

impl Universe {
    pub fn new() -> Universe {
        return Universe {
            rng: thread_rng(),
            stars: vec![Star::new(
                Vector::new(0.0, 0.0),
                Vector::new(0.0, 0.0),
                SUN_MASS,
            )],
        };
    }

    pub fn step(&mut self) {
        let time = 100;
        for _ in 0..time {
            rk4::rk4_world(self, 0.1 / time as F);
        }

        let com = self.com();
        for star in &mut self.stars {
            star.pos = star.pos - com;
        }

        let cov = self.cov();
        for star in &mut self.stars {
            star.vel = star.vel - cov;
        }

        let positions = self.stars
            .iter()
            .map(|x| (x.pos, x.mass))
            .collect::<Vec<_>>();
        self.stars.retain(|star| {
            star.pos.len2() < RETAIN_LIMIT * RETAIN_LIMIT && {
                let dist = positions
                    .iter()
                    .filter(|x| x.1 > star.mass)
                    .map(|other| (star.pos - other.0).len2())
                    .min_by(|x, y| x.partial_cmp(y).unwrap());
                let min_dist = 1.0;
                if let Some(dist) = dist {
                    dist > min_dist * min_dist
                } else {
                    true
                }
            }
        });
        if self.stars.len() < NUM_STARS {
            let new_star = self.gen_star();
            self.stars.push(new_star);
        }
    }

    fn com(&self) -> Vector {
        let mut com = Vector::new(0.0, 0.0);
        let mut total_mass = 0.0;
        for star in &self.stars {
            com = com + star.pos * star.mass;
            total_mass += star.mass;
        }
        com * (1.0 / total_mass)
    }

    fn cov(&self) -> Vector {
        let mut cov = Vector::new(0.0, 0.0);
        for star in &self.stars {
            cov = cov + star.vel;
        }
        cov * (1.0 / self.stars.len() as F)
    }

    pub fn gen_star(&mut self) -> Star {
        let pos = Vector::new(self.rng.gen::<F>() - 0.5, self.rng.gen::<F>() - 0.5) * 2.0 * POS_GEN;
        let accel = Self::calc_accel_one(
            self.stars.iter().map(|x| VecMass::new(x.pos, x.mass)),
            pos,
            usize::max_value(),
        );
        // a = v * v / r
        // v = sqrt(r * a)
        let speed = ((self.com() - pos).len() * accel.len()).sqrt();
        let vel = Vector::new(accel.y, -accel.x).normalized() * speed;
        Star::new(pos, vel, 1.0)
    }

    // pos -> accel
    pub fn calc_accel(pos: Vec<VecMass>) -> Vec<Vector> {
        pos.iter()
            .enumerate()
            .map(|(i, &me)| Self::calc_accel_one(pos.iter().cloned(), me.vec, i))
            .collect()
    }

    pub fn calc_accel_one<'a, Pos: IntoIterator<Item = VecMass>>(
        pos: Pos,
        single: Vector,
        index: usize,
    ) -> Vector {
        let mut res = Vector::new(0.0, 0.0);
        for (j, other) in pos.into_iter().enumerate() {
            if index != j {
                let direction = single - other.vec;
                //let accel = G * other.mass / direction.len2();
                let mut accel = G * other.mass / direction.len2();
                res = res - direction.normalized() * accel;
            }
        }
        let maxlen = 1000.0;
        if res.len2() > maxlen * maxlen {
            println!("Screee!!! {}", res.len());
            res = Vector::new(0.0, 0.0);
            //res = res.normalized() * maxlen;
        }
        res
    }

    fn hue_to_rgb(mut hue: f32, mut saturation: f32, value: f32) -> (f32, f32, f32) {
        hue *= 3.0;
        let frac = hue % 1.0;
        let mut color = match hue as u32 {
            0 => (1.0 - frac, frac, 0.0),
            1 => (0.0, 1.0 - frac, frac),
            2 => (frac, 0.0, 1.0 - frac),
            _ => (1.0, 1.0, 1.0),
        };
        saturation = value * (1.0 - saturation);
        color.0 = color.0 * (value - saturation) + saturation;
        color.1 = color.1 * (value - saturation) + saturation;
        color.2 = color.2 * (value - saturation) + saturation;
        color.0 = color.0.sqrt();
        color.1 = color.1.sqrt();
        color.2 = color.2.sqrt();
        color
    }

    pub fn draw(&self, width: usize, height: usize, pixels: &mut [u8]) {
        for x in pixels.iter_mut() {
            *x = x.saturating_sub(1);
            //if *x > 0 { *x -= 1; }
        }
        const RENDER_RADIUS: usize = 5;
        for (i, star) in self.stars.iter().enumerate() {
            let mut energy = 0.0;
            for (j, other) in self.stars.iter().enumerate() {
                if i != j {
                    let grav_energy = G * other.mass / (other.pos - star.pos).len();
                    energy += grav_energy;
                }
            }
            let kine_energy = star.mass * star.vel.len2() / 2.0;
            energy += kine_energy;
            let hue = energy.abs().log(COLOR_VARIANCE).sin() * 0.5 + 0.5;
            let screen_pos = (star.pos * (1.0 / (2.0 * SCREEN_SIZE)) + Vector::new(0.5, 0.5))
                * ((width + height) as F / 2.0);
            for y in (screen_pos.y as usize).saturating_sub(RENDER_RADIUS)
                ..(screen_pos.y as usize + RENDER_RADIUS).min(height - 1)
            {
                for x in (screen_pos.x as usize).saturating_sub(RENDER_RADIUS)
                    ..(screen_pos.x as usize + RENDER_RADIUS).min(width - 1)
                {
                    let pos = (Vector::new(x as F, y as F) * (2.0 / (width + height) as F)
                        - Vector::new(0.5, 0.5)) * 2.0 * SCREEN_SIZE;
                    let dist = star.pos - pos;
                    let bright = star.vel.len().sqrt();
                    let add = (-dist.len2() * 10.0).exp();
                    let val = add * bright * 2000.0;
                    let rgb = Self::hue_to_rgb(hue, SATURATION, val);
                    let pix = &mut pixels[(y * width + x) * 4..(y * width + x) * 4 + 4];
                    pix[1] = pix[1].saturating_add(rgb.0 as u8);
                    pix[2] = pix[2].saturating_add(rgb.1 as u8);
                    pix[3] = pix[3].saturating_add(rgb.2 as u8);
                }
            }
        }
        //for y in 0..height {
        //    for x in 0..width {
        //        let pos = (Vector::new(x as F, y as F) * (2.0 / (width + height) as F)
        //            - Vector::new(0.5, 0.5)) * 2.0 * SCREEN_SIZE;
        //        let mut val = 0.0;
        //        for star in &self.stars {
        //            let dist = star.pos - pos;
        //            let bright = star.vel.len().sqrt();
        //            let add = (-dist.len2() * 10.0).exp();
        //            val += add * bright;
        //        }
        //        let val = (val * 50.0) as u8;
        //        let pix = &mut pixels[(y * width + x) * 4..(y * width + x) * 4 + 4];
        //        pix[1] = pix[1].saturating_add(val);
        //        pix[2] = pix[2].saturating_add(val);
        //        pix[3] = pix[3].saturating_add(val);
        //    }
        //}
        //for star in &self.stars {
        //    let x = ((star.pos.x / 100.0 + 0.5) * width as F) as usize;
        //    let y = ((star.pos.y / 100.0 + 0.5) * height as F) as usize;
        //    if x >= width || y >= height {
        //        continue
        //    }
        //    pixels[(y * width + x) * 4 + 1] = 255;
        //}
    }
}
