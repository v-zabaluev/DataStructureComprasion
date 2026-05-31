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
        public int SectorId;

        public ProjectileData(int id, Vector2 position, Vector2 direction, float speed, float lifeTime, int sectorId)
        {
            Id = id;
            Position = position;
            Direction = direction;
            Speed = speed;
            LifeTime = lifeTime;
            SectorId = sectorId;
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

        public void RecalculateSector(float cellSize, int cellsPerRow)
        {
            SectorId = CalculateSector(Position, cellSize, cellsPerRow);
        }

        public void RecalculateSector(Vector2 gridMin, float cellSize, int cellsPerRow)
        {
            SectorId = CalculateSector(Position, gridMin, cellSize, cellsPerRow);
        }

        public static int CalculateSector(Vector2 position, float cellSize, int cellsPerRow)
        {
            return CalculateSector(position, Vector2.zero, cellSize, cellsPerRow);
        }

        public static int CalculateSector(Vector2 position, Vector2 gridMin, float cellSize, int cellsPerRow)
        {
            if (cellsPerRow < 1)
            {
                cellsPerRow = 1;
            }

            if (cellSize <= 0f)
            {
                cellSize = 0.01f;
            }

            int y = Mathf.FloorToInt((position.y - gridMin.y) / cellSize);
            int x = Mathf.FloorToInt((position.x - gridMin.x) / cellSize);

            if (x < 0)
            {
                x = 0;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (x >= cellsPerRow)
            {
                x = cellsPerRow - 1;
            }

            if (y >= cellsPerRow)
            {
                y = cellsPerRow - 1;
            }

            return y * cellsPerRow + x;
        }

        public static bool IsOutsideGrid(Vector2 position, Vector2 gridMin, float cellSize, int cellsPerRow)
        {
            if (cellsPerRow < 1)
            {
                cellsPerRow = 1;
            }

            if (cellSize <= 0f)
            {
                cellSize = 0.01f;
            }

            float gridMaxX = gridMin.x + cellSize * cellsPerRow;
            float gridMaxY = gridMin.y + cellSize * cellsPerRow;

            return
                position.x < gridMin.x ||
                position.y < gridMin.y ||
                position.x >= gridMaxX ||
                position.y >= gridMaxY;
        }
    }
}
