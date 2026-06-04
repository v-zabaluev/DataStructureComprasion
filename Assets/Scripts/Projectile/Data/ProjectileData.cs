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

        public bool IsExpired()
        {
            return LifeTime <= 0f;
        }

        public bool IsInsideCircle(Vector2 center, float radius)
        {
            float sqrRadius = radius * radius;
            float sqrDistance = (Position - center).sqrMagnitude;

            return sqrDistance <= sqrRadius;
        }
    }
}