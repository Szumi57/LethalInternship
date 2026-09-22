using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IPluginManager
    {
        /// <summary>
        /// <c>GameObject</c> prefab of the <c>SaveManager</c>, see: <see cref="SaveManager"><c>SaveManager</c></see>
        /// </summary>
        public GameObject SaveManagerPrefab { get; }
        /// <summary>
        /// <c>GameObject</c> prefab of the <c>InternManager</c>, see: <see cref="InternManager"><c>InternManager</c></see>
        /// </summary>
        public GameObject InternManagerPrefab { get; }
        /// <summary>
        /// <c>GameObject</c> prefab of the <c>TerminalManager</c>, see: <see cref="TerminalManager"><c>TerminalManager</c></see>
        /// </summary>
        public GameObject TerminalManagerPrefab { get; }
    }
}
