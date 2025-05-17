using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;

namespace LethalInternship.Core.TerminalAdapter
{
    /// <summary>
    /// Abstract state main class for the <c>TerminalState</c>
    /// </summary>
    public abstract class TerminalState
    {
        private EnumTerminalStates currentState;
        protected EnumTerminalStates CurrentState
        {
            get
            {
                return this.currentState;
            }
            set
            {
                this.currentState = value;
                PluginLoggerHook.LogDebug?.Invoke($"TerminalState new state :                 {this.currentState}");
            }
        }

        protected TerminalParser terminalParser;
        protected Dictionary<EnumTerminalStates, TerminalNode> dictTerminalNodeByState;

        /// <summary>
        /// Constructor from another state
        /// </summary>
        /// <param name="oldState"></param>
        protected TerminalState(TerminalState oldState) : this(oldState.terminalParser)
        {
            this.dictTerminalNodeByState = oldState.dictTerminalNodeByState;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="terminalParser"></param>
        /// <exception cref="System.NullReferenceException"><c>TerminalParser</c> null in parameters</exception>
        protected TerminalState(TerminalParser terminalParser)
        {
            if (terminalParser == null)
            {
                throw new System.NullReferenceException("TerminalParser is null.");
            }

            this.terminalParser = terminalParser;

            if (this.dictTerminalNodeByState == null)
            {
                dictTerminalNodeByState = new Dictionary<EnumTerminalStates, TerminalNode>();
            }
        }

        /// <summary>
        /// Get the <see cref="Enums.EnumTerminalStates"><c>Enums.EnumTerminalStates</c></see> of current State
        /// </summary>
        /// <returns></returns>
        public virtual EnumTerminalStates GetTerminalState() { return CurrentState; }

        /// <summary>
        /// Analyze the words in the command send by the player in the terminal,<br/>
        /// and decide for actions for the current state
        /// </summary>
        /// <param name="words"></param>
        /// <returns>true if the state could parse the command and make an action</returns>
        public abstract bool ParseCommandValid(string[] words);

        /// <summary>
        /// What to display on the terminal for this current page/state
        /// </summary>
        /// <returns>TerminalNode created to return to the base game terminal to display.</returns>
        public abstract TerminalNode? DisplayNode();
    }
}
