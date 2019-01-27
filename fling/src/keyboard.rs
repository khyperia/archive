use glium::glutin::VirtualKeyCode as Key;
use number::Vector3;
use std::collections::HashMap;
use std::collections::hash_map::Entry;
use std::time::Instant;

pub struct Input {
    pressed_keys: HashMap<Key, Instant>,
    pub index: usize,
}

impl Input {
    pub fn new() -> Input {
        Input {
            pressed_keys: HashMap::new(),
            index: 0,
        }
    }

    pub fn key_down(&mut self, key: Key, time: Instant) {
        if let Entry::Vacant(entry) = self.pressed_keys.entry(key) {
            entry.insert(time);
        }
    }

    pub fn key_up(&mut self, key: Key, time: Instant, settings: &mut Settings) {
        if self.pressed_keys.contains_key(&key) {
            self.run(settings, time);
            self.pressed_keys.remove(&key);
        }
    }

    pub fn integrate(&mut self, settings: &mut Settings) {
        let now = Instant::now();
        self.run(settings, now);
    }

    fn run(&mut self, settings: &mut Settings, now: Instant) {
        self.camera_3d(settings, now);
        self.exp_setting(settings, now, "focal_distance".into(), Key::R, Key::F);
        self.exp_setting(settings, now, "fov".into(), Key::N, Key::M);
        self.manual_control(settings, now);
        for value in self.pressed_keys.values_mut() {
            *value = now;
        }
    }

    fn is_pressed(&self, now: Instant, key: Key) -> Option<f32> {
        if let Some(&old) = self.pressed_keys.get(&key) {
            let dt = now.duration_since(old);
            let flt = dt.as_secs() as f32 + dt.subsec_nanos() as f32 * 1e-9;
            Some(flt)
        } else {
            None
        }
    }

    fn camera_3d(&self, settings: &mut Settings, now: Instant) {
        let move_speed = settings.get_f32("focal_distance").unwrap() * 0.5;
        let turn_speed = settings.get_f32("fov").unwrap();
        let roll_speed = 1.0;
        let mut pos = settings.get_vec("pos_x", "pos_y", "pos_z").unwrap();
        let mut look = settings.get_vec("look_x", "look_y", "look_z").unwrap();
        let mut up = settings.get_vec("up_x", "up_y", "up_z").unwrap();
        let old = (pos, look, up);
        let right = Vector3::cross(look, up);
        if let Some(dt) = self.is_pressed(now, Key::W) {
            pos = pos + look * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::S) {
            pos = pos - look * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::D) {
            pos = pos + right * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::A) {
            pos = pos - right * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::Space) {
            pos = pos + up * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::Z) {
            pos = pos - up * (move_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::I) {
            look = look.rotate(up, turn_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::K) {
            look = look.rotate(up, -turn_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::L) {
            look = look.rotate(right, turn_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::J) {
            look = look.rotate(right, -turn_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::O) {
            up = up.rotate(right, roll_speed * dt);
        }
        if let Some(dt) = self.is_pressed(now, Key::U) {
            up = up.rotate(right, -roll_speed * dt);
        }
        if old != (pos, look, up) {
            look = look.normalized();
            up = Vector3::cross(Vector3::cross(look, up), look).normalized();
            settings.set_vec("pos_x", "pos_y", "pos_z", pos);
            settings.set_vec("look_x", "look_y", "look_z", look);
            settings.set_vec("up_x", "up_y", "up_z", up);
        }
    }

    fn exp_setting(
        &self,
        settings: &mut Settings,
        now: Instant,
        key: String,
        increase: Key,
        decrease: Key,
    ) {
        let (mut value, change) = match *settings.get(&key).unwrap() {
            SettingValue::F32(value, change) => (value, -change + 1.0),
        };
        if let Some(dt) = self.is_pressed(now, increase) {
            value *= change.powf(dt);
        }
        if let Some(dt) = self.is_pressed(now, decrease) {
            value *= change.powf(-dt);
        }
        settings.insert(key, SettingValue::F32(value, -change + 1.0));
    }

    fn sorted_keys(&self, settings: &Settings) -> Vec<String> {
        let mut keys = settings.value_map().keys().cloned().collect::<Vec<_>>();
        keys.sort();
        keys
    }

    fn manual_control(&mut self, settings: &mut Settings, now: Instant) {
        if let Some(dt) = self.is_pressed(now, Key::Left) {
            let key = &self.sorted_keys(settings)[self.index];
            let &SettingValue::F32(value, change) = settings.get(key).unwrap();
            if change < 0.0 {
                settings.insert(
                    key.clone(),
                    SettingValue::F32(value * (-change + 1.0).powf(-dt), change),
                );
            } else {
                settings.insert(key.clone(), SettingValue::F32(value - dt * change, change));
            }
        }
        if let Some(dt) = self.is_pressed(now, Key::Right) {
            let key = &self.sorted_keys(settings)[self.index];
            let &SettingValue::F32(value, change) = settings.get(key).unwrap();
            if change < 0.0 {
                settings.insert(
                    key.clone(),
                    SettingValue::F32(value * (-change + 1.0).powf(dt), change),
                );
            } else {
                settings.insert(key.clone(), SettingValue::F32(value + dt * change, change));
            }
        }
    }
}

#[derive(Copy, Clone)]
pub enum SettingValue {
    F32(f32, f32),
}

#[derive(Clone)]
pub struct Settings {
    value_map: HashMap<String, SettingValue>,
}

impl Settings {
    pub fn new() -> Self {
        let mut settings = Settings {
            value_map: HashMap::new(),
        };
        settings.insert("pos_x".into(), SettingValue::F32(5.0, 1.0));
        settings.insert("pos_y".into(), SettingValue::F32(0.9, 1.0));
        settings.insert("pos_z".into(), SettingValue::F32(0.9, 1.0));
        settings.insert("look_x".into(), SettingValue::F32(-1.0, 1.0));
        settings.insert("look_y".into(), SettingValue::F32(0.0, 1.0));
        settings.insert("look_z".into(), SettingValue::F32(0.0, 1.0));
        settings.insert("up_x".into(), SettingValue::F32(0.0, 1.0));
        settings.insert("up_y".into(), SettingValue::F32(1.0, 1.0));
        settings.insert("up_z".into(), SettingValue::F32(0.0, 1.0));
        settings.insert("fov".into(), SettingValue::F32(1.0, -1.0));
        settings.insert("focal_distance".into(), SettingValue::F32(3.0, -1.0));
        settings
    }

    pub fn value_map(&self) -> &HashMap<String, SettingValue> {
        &self.value_map
    }

    pub fn insert(&mut self, key: String, value: SettingValue) -> Option<SettingValue> {
        self.value_map.insert(key, value)
    }

    pub fn get(&self, key: &str) -> Option<&SettingValue> {
        self.value_map.get(key)
    }

    pub fn get_f32(&self, key: &str) -> Option<f32> {
        match self.get(key) {
            Some(&SettingValue::F32(val, _)) => Some(val),
            _ => None,
        }
    }

    pub fn get_vec(&self, x: &str, y: &str, z: &str) -> Option<Vector3<f32>> {
        let x = self.get_f32(x);
        let x = match x {
            Some(a) => a,
            None => return None,
        };
        let y = self.get_f32(y);
        let y = match y {
            Some(a) => a,
            None => return None,
        };
        let z = self.get_f32(z);
        let z = match z {
            Some(a) => a,
            None => return None,
        };
        Some(Vector3::new(x, y, z))
    }

    pub fn set_vec(&mut self, x: &str, y: &str, z: &str, value: Vector3<f32>) {
        match (
            self.get(&x).cloned(),
            self.get(&y).cloned(),
            self.get(&z).cloned(),
        ) {
            (
                Some(SettingValue::F32(_, dx)),
                Some(SettingValue::F32(_, dy)),
                Some(SettingValue::F32(_, dz)),
            ) => {
                self.insert(x.to_string(), SettingValue::F32(value.x, dx));
                self.insert(y.to_string(), SettingValue::F32(value.y, dy));
                self.insert(z.to_string(), SettingValue::F32(value.z, dz));
            }
            _ => (),
        }
    }
}
