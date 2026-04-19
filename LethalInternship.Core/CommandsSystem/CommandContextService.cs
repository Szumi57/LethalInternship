using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using System.Linq;

namespace LethalInternship.Core.CommandsSystem
{
    public class CommandContextService
    {
        private static CommandContextService _instance = null!;
        public static CommandContextService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CommandContextService();

                return _instance;
            }
        }

        private CommandContextService() { }

        public void EnterCommandMode()
        {
            var internsOwned = IdentityManager.Instance.GetIdentitiesSpawned()
                                .Where(x => IdentityManager.Instance.IsIdentityValidToCommand(x))
                                .Select(x => x.InternAI!);
            foreach (IInternAI intern in internsOwned)
            {
                intern.SetCommandToWaitForCommand(wait: true);
            }
        }

        public void ExitCommandMode()
        {
            var internsOwned = IdentityManager.Instance.GetIdentitiesSpawned()
                                .Where(x => IdentityManager.Instance.IsIdentityValidToCommand(x))
                                .Select(x => x.InternAI!);
            foreach (IInternAI intern in internsOwned)
            {
                intern.SetCommandToWaitForCommand(wait: false);
            }
        }
    }
}
