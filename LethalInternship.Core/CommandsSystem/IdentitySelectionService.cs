using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem
{
    public sealed class IdentitySelectionService
    {
        private static IdentitySelectionService _instance = null!;
        public static IdentitySelectionService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new IdentitySelectionService();

                return _instance;
            }
        }

        private IdentitySelectionService() { }

        private readonly List<IInternIdentity> _currentList = new List<IInternIdentity>();
        private readonly HashSet<IInternIdentity> _selected = new HashSet<IInternIdentity>();

        private int _currentIndex = -1;

        public void Refresh(IEnumerable<IInternIdentity> candidates)
        {
            IInternIdentity? previous = GetCurrent();

            _currentList.Clear();
            _currentList.AddRange(candidates);

            _selected.RemoveWhere(x => !_currentList.Contains(x));

            // restaurer index si possible
            if (previous != null)
            {
                _currentIndex = _currentList.IndexOf(previous);
            }

            if (_currentIndex < 0 && _currentList.Count > 0)
                _currentIndex = 0;

            if (_currentList.Count == 0)
                Reset();
        }

        public void SelectSingle(IInternIdentity identity)
        {
            _selected.Clear();
            if (identity == null) return;

            _selected.Add(identity);
            _currentIndex = _currentList.IndexOf(identity);
        }

        public void SelectAll()
        {
            _selected.Clear();
            foreach (var identity in _currentList)
                _selected.Add(identity);

            _currentIndex = _currentList.Count > 0 ? 0 : -1;
        }

        public IInternIdentity? Next()
        {
            if (_currentList.Count == 0) return null;

            _currentIndex = (_currentIndex + 1) % _currentList.Count;
            return _currentList[_currentIndex];
        }

        public IInternIdentity? Previous()
        {
            if (_currentList.Count == 0) return null;

            _currentIndex--;
            if (_currentIndex < 0)
                _currentIndex = _currentList.Count - 1;

            return _currentList[_currentIndex];
        }

        public IInternIdentity? GetCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _currentList.Count)
                return null;

            return _currentList[_currentIndex];
        }

        public IReadOnlyCollection<IInternIdentity> GetSelected()
        {
            return _selected;
        }

        public void Reset()
        {
            _currentList.Clear();
            _selected.Clear();
            _currentIndex = -1;
        }
    }
}
