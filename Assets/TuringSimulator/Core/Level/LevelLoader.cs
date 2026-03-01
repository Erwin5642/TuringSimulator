using UnityEngine;

namespace TuringSimulator.Core.Level
{
    public class LevelLoader
    {
        private int _index;
        private readonly LevelDatabase _database;
        private readonly LevelContext _context;

        public LevelLoader(LevelDatabase database, LevelContext context)
        {
            _database = database;
            _context = context;
        }

        public LevelDefinition LoadCurrent()
        {
            var level = _database.Get(_index);
            _context.Set(level);
            return level;
        }

        public LevelDefinition LoadNext()
        {
            _index = Mathf.Min(_index + 1, _database.Count - 1);
            return LoadCurrent();
        }
    }
}