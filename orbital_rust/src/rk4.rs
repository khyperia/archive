use world;

pub fn rk4_world(uni: &mut world::Universe, time: world::F) {
    let mut pos = Vec::with_capacity(uni.stars.len());
    let mut vel = Vec::with_capacity(uni.stars.len());
    for star in &uni.stars {
        pos.push(world::VecMass::new(star.pos, star.mass));
        vel.push(star.vel);
    }

    let add_vec_1 = |l: Vec<_>, r: Vec<_>| {
        l.into_iter()
            .zip(r.into_iter())
            .map(|(l, r)| l + r)
            .collect::<Vec<_>>()
    };
    let add_vec_2 = |l: Vec<_>, r: Vec<_>| {
        l.into_iter()
            .zip(r.into_iter())
            .map(|(l, r)| l + r)
            .collect::<Vec<_>>()
    };
    let mul_massvec = |l: Vec<_>, r| {
        l.into_iter()
            .map(|l| world::VecMass::new(l * r, 0.0))
            .collect::<Vec<_>>()
    };
    let mul_vec =
        |l: Vec<_>, r: world::F| -> Vec<_> { l.into_iter().map(|l| l * r).collect::<Vec<_>>() };
    let (pos, vel) = rk4_2nd(
        world::Universe::calc_accel,
        pos,
        vel,
        time,
        &add_vec_1,
        &add_vec_2,
        &add_vec_2,
        &mul_massvec,
        &mul_vec,
        |l, r| l * r,
    );

    for (star, (pos, vel)) in uni.stars
        .iter_mut()
        .zip(pos.into_iter().zip(vel.into_iter()))
    {
        star.pos = pos.vec;
        star.vel = vel;
    }
}

fn rk4_2nd<
    Pos: Clone,
    Vel: Clone,
    Accel: Clone,
    Time: Clone,
    AddPos: Fn(Pos, Pos) -> Pos,
    AddVel: Fn(Vel, Vel) -> Vel,
    AddAcc: Fn(Accel, Accel) -> Accel,
    MulVel: Fn(Vel, Time) -> Pos,
    MulAcc: Fn(Accel, Time) -> Vel,
    MulTime: Fn(Time, f32) -> Time,
    F: Fn(Pos) -> Accel,
>(
    func: F,
    pos: Pos,
    vel: Vel,
    step: Time,
    add_pos: AddPos,
    add_vel: AddVel,
    add_acc: AddAcc,
    mul_vel: MulVel,
    mul_acc: MulAcc,
    mul_time: MulTime,
) -> (Pos, Vel) {
    rk4(
        |(pos, vel)| (vel, func(pos)),
        (pos, vel),
        step,
        |(pos1, vel1), (pos2, vel2)| (add_pos(pos1, pos2), add_vel(vel1, vel2)),
        |(vel1, acc1), (vel2, acc2)| (add_vel(vel1, vel2), add_acc(acc1, acc2)),
        |(vel1, acc1), time| (mul_vel(vel1, time.clone()), mul_acc(acc1, time)),
        |time, scale| mul_time(time, scale),
    )
}

fn rk4<
    T: Clone,
    DT: Clone,
    Time: Clone,
    AddT: Fn(T, T) -> T,
    AddDT: Fn(DT, DT) -> DT,
    MulDT: Fn(DT, Time) -> T,
    MulTime: Fn(Time, f32) -> Time,
    F: Fn(T) -> DT,
>(
    func: F,
    pos: T,
    step: Time,
    add_t: AddT,
    add_dt: AddDT,
    mul_dt: MulDT,
    mul_time: MulTime,
) -> T {
    let half_step = mul_time(step.clone(), 1.0 / 2.0);
    let full_step = step;
    let k1 = func(pos.clone());
    let k2 = func(add_t(pos.clone(), mul_dt(k1.clone(), half_step.clone())));
    let k3 = func(add_t(pos.clone(), mul_dt(k2.clone(), half_step.clone())));
    let k4 = func(add_t(pos.clone(), mul_dt(k3.clone(), full_step.clone())));
    add_t(
        pos,
        mul_dt(
            add_dt(
                add_dt(add_dt(k1, k2.clone()), add_dt(k2, k3.clone())),
                add_dt(k3, k4),
            ),
            mul_time(full_step, 1.0 / 6.0),
        ),
    )
}
