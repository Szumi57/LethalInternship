using LethalInternship.Core.CommandsSystem.Abilities;

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
            new WaitForCommandAbility(wait: true).Activate();
        }

        public void ExitCommandMode()
        {
            new WaitForCommandAbility(wait: false).Activate();
        }
    }
}
