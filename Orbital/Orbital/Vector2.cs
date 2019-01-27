using System;
using System.Linq;

namespace Orbital
{
    struct Vector2
    {
        public readonly double X, Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator *(Vector2 left, double right)
        {
            return new Vector2(left.X * right, left.Y * right);
        }

        public static Vector2 operator *(double left, Vector2 right)
        {
            return new Vector2(left * right.X, left * right.Y);
        }

        public static Vector2 operator /(Vector2 left, double right)
        {
            return new Vector2(left.X / right, left.Y / right);
        }

        public double LengthSquared
        {
            get { return X * X + Y * Y; }
        }

        public double Length
        {
            get { return Math.Sqrt(LengthSquared); }
        }

        public Vector2 Normalized
        {
            get { return this / Length; }
        }

        public double Theta
        {
            get { return Math.Atan2(Y, X); }
        }

        public static double Dot(Vector2 left, Vector2 right)
        {
            return left.X * right.X + left.Y * right.Y;
        }
    }

    struct Star
    {
        private const double G = 1;
        private readonly double _mass;
        private readonly Vector2 _position;
        private readonly Vector2 _velocity;
        private Vector2 _acceleration;

        public Star(double mass, Vector2 position, Vector2 velocity)
        {
            _mass = mass;
            _position = position;
            _velocity = velocity;
            _acceleration = new Vector2();
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public double Mass
        {
            get { return _mass; }
        }

        public Vector2 Velocity
        {
            get { return _velocity; }
        }

        public double Energy(Star other)
        {
            return (Velocity - other.Velocity).LengthSquared / 2 - G * (Mass + other.Mass) / (Position - other.Position).Length;
        }

        public static Vector2 AccelerationAt(Star[] stars, Vector2 position)
        {
            var total = new Vector2(0, 0);
            for (var i = 0; i < stars.Length; i++)
            {
                var direction = stars[i].Position - position;
                var attraction = G * stars[i]._mass / direction.LengthSquared;
                total += direction.Normalized * attraction;
            }
            return total;
        }

        private Vector2 ComputeAcceleration(Star[] stars, int ignoreIndex)
        {
            var total = new Vector2(0, 0);
            for (var i = 0; i < stars.Length; i++)
            {
                if (i == ignoreIndex)
                    continue;
                var direction = stars[i].Position - Position;
                var attraction = G * stars[i]._mass / direction.LengthSquared;
                total += direction.Normalized * attraction;
            }
            return total;
        }

        private static Star[] ComputeStep(Star[] original, Star[] stars, double timestep)
        {
            var result = new Star[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                var position = original[i].Position + timestep * stars[i].Velocity;
                var velocity = original[i].Velocity + timestep * stars[i]._acceleration;
                result[i] = new Star(original[i].Mass, position, velocity);
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i]._acceleration = result[i].ComputeAcceleration(result, i);
            }
            return result;
        }

        private static void Rk4Stars(Star[] stars, double timestep)
        {
            var rk1 = stars;
            var rk2 = ComputeStep(stars, rk1, timestep / 2);
            var rk3 = ComputeStep(stars, rk2, timestep / 2);
            var rk4 = ComputeStep(stars, rk3, timestep);

            for (int i = 0; i < stars.Length; i++)
            {
                var newPos = stars[i].Position + timestep / 6 * (rk1[i].Velocity + 2 * rk2[i].Velocity + 2 * rk3[i].Velocity + rk4[i].Velocity);
                var newVel = stars[i].Velocity + timestep / 6 * (rk1[i]._acceleration + 2 * rk2[i]._acceleration + 2 * rk3[i]._acceleration + rk4[i]._acceleration);
                stars[i] = new Star(stars[i].Mass, newPos, newVel);
            }
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i]._acceleration = stars[i].ComputeAcceleration(stars, i);
            }
        }

        public static Vector2 CenterOfMass(Star[] stars)
        {
            return stars.Length == 0 ? new Vector2(0, 0) : stars.Aggregate(new Vector2(0, 0), (total, part) => total + part._position * part._mass) /
                (stars.Length * stars.Aggregate(0.0, (total, part) => total + part._mass));
        }

        public static Vector2 AvgVelocity(Star[] stars)
        {
            var totalMomentum = stars.Aggregate(new Vector2(0, 0), (total, part) => total + part._velocity * part._mass);
            var totalMass = stars.Aggregate(0.0, (total, part) => total + part._mass);
            return stars.Length == 0 ? new Vector2(0, 0) : totalMomentum / (stars.Length * totalMass);
        }

        public static void Simulate(Star[] stars, double timestep)
        {
            Rk4Stars(stars, timestep);
            var center = CenterOfMass(stars);
            var avgVel = AvgVelocity(stars);
            for (var i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                stars[i] = new Star(star._mass, star._position - center, star._velocity - avgVel);
            }
        }
    }
}
