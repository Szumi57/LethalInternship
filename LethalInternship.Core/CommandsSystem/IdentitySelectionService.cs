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

        public readonly HashSet<IInternIdentity> SelectedInterns = new HashSet<IInternIdentity>();

        public void SelectSingle(IInternIdentity identity)
        {
            SelectedInterns.Clear();
            if (identity != null)
                SelectedInterns.Add(identity);
        }

        public void SelectMultiple(IEnumerable<IInternIdentity> identities)
        {
            SelectedInterns.Clear();
            foreach (var i in identities)
                SelectedInterns.Add(i);
        }

        public void RemoveIntern(IInternIdentity identity)
        {
            SelectedInterns.Remove(identity);
        }
    }
}
