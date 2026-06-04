using Projectile.Data;

namespace Projectile
{
    public class ProjectileDataFactory
    {
        public ProjectileData Create(ProjectileCreationData creationData)
        {
            return new ProjectileData(
                creationData.Id,
                creationData.Position,
                creationData.Direction,
                creationData.Speed,
                creationData.LifeTime);
        }
    }
}