use std::cmp::PartialOrd;
use std::ops::{Add, Div, Mul, Sub};

#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub struct Vector3<T> {
    pub x: T,
    pub y: T,
    pub z: T,
}

impl<T> Vector3<T> {
    pub fn new(x: T, y: T, z: T) -> Self {
        Self { x, y, z }
    }
}

impl Vector3<f32> {
    pub fn normalized(self) -> Self {
        self * (1.0 / self.len2().sqrt())
    }

    pub fn cross(self, rhs: Self) -> Self {
        Vector3::new(
            self.y * rhs.z - self.z * rhs.y,
            self.z * rhs.x - self.x * rhs.z,
            self.x * rhs.y - self.y * rhs.x,
        )
    }

    pub fn rotate(&self, direction: Self, amount: f32) -> Self {
        (*self + direction * amount).normalized()
    }
}

impl<T: PartialOrd + Copy> Vector3<T> {
    pub fn clamp(self, min: T, max: T) -> Self {
        Self::new(
            clamp(self.x, min, max),
            clamp(self.y, min, max),
            clamp(self.z, min, max),
        )
    }
}

impl<T: Copy> Vector3<T>
where
    T: Mul<T>,
    <T as Mul>::Output: Add<Output = <T as Mul>::Output>,
{
    pub fn len2(self) -> <T as Mul>::Output {
        self.x * self.x + self.y * self.y + self.z * self.z
    }
}

impl<T: Add> Add for Vector3<T> {
    type Output = Vector3<T::Output>;
    fn add(self, rhs: Vector3<T>) -> Self::Output {
        Vector3::new(self.x + rhs.x, self.y + rhs.y, self.z + rhs.z)
    }
}

impl<T: Sub> Sub for Vector3<T> {
    type Output = Vector3<T::Output>;
    fn sub(self, rhs: Vector3<T>) -> Self::Output {
        Self::Output::new(self.x - rhs.x, self.y - rhs.y, self.z - rhs.z)
    }
}

impl<T: Mul + Copy> Mul<T> for Vector3<T> {
    type Output = Vector3<T::Output>;
    fn mul(self, rhs: T) -> Self::Output {
        Self::Output::new(self.x * rhs, self.y * rhs, self.z * rhs)
    }
}

impl<T: Div + Copy> Div<T> for Vector3<T> {
    type Output = Vector3<T::Output>;
    fn div(self, rhs: T) -> Self::Output {
        Self::Output::new(self.x / rhs, self.y / rhs, self.z / rhs)
    }
}

pub fn clamp<T: PartialOrd>(value: T, min: T, max: T) -> T {
    if value < min {
        min
    } else if value > max {
        max
    } else {
        value
    }
}

#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub struct Dual<T> {
    real: T,
    dual: T,
}

impl<T> Dual<T>
where
    T: Add<T, Output = T>,
    T: Sub<T, Output = T>,
    T: Mul<Output = T>,
    T: Div<Output = T>,
{
    pub fn new(real: T, dual: T) -> Self {
        Self { real, dual }
    }
}
