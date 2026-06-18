using UnityEngine;

namespace Projectile.Data
{
    public struct ProjectileData
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Direction;
        public float Speed;
        public float LifeTime;

        public ProjectileData(int id, Vector2 position, Vector2 direction, float speed, float lifeTime)
        {
            Id = id;
            Position = position;
            Direction = direction;
            Speed = speed;
            LifeTime = lifeTime;
        }

        public void Update(float deltaTime)
        {
            Position += Direction * Speed * deltaTime;
            LifeTime -= deltaTime;
        }
    }
}   