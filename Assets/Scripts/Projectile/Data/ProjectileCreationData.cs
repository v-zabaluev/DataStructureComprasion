using UnityEngine;

namespace Projectile.Data
{
    public readonly struct ProjectileCreationData
    {
        public readonly int Id;
        public readonly Vector2 Position;
        public readonly Vector2 Direction;
        public readonly float Speed;
        public readonly float LifeTime;

        public ProjectileCreationData(
            int id,
            Vector2 position,
            Vector2 direction,
            float speed,
            float lifeTime)
        {
            Id = id;
            Position = position;
            Direction = direction;
            Speed = speed;
            LifeTime = lifeTime;
        }
    }
}