using System;

namespace Projectile.Data
{
    public  class ProjectileDataset
    {
        private readonly ProjectileData[] _items;

        public ProjectileDataset(
            ProjectileData[] items,
            int seed,
            int iterationIndex)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            Seed = seed;
            IterationIndex = iterationIndex;
        }

        public int Count => _items.Length;

        public int Seed { get; }

        public int IterationIndex { get; }

        public ProjectileData Get(int index)
        {
            return _items[index];
        }

        public void CopyTo(ProjectileData[] target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Length < _items.Length)
            {
                throw new ArgumentException(
                    "Target array length is less than dataset length.",
                    nameof(target));
            }

            Array.Copy(_items, target, _items.Length);
        }

        public ProjectileData[] CreateCopy()
        {
            ProjectileData[] copy = new ProjectileData[_items.Length];
            Array.Copy(_items, copy, _items.Length);

            return copy;
        }
    }
}