using System;
using UnityEngine;

namespace LethalInternship.Core.UI
{
    public class CommandButtonController : MonoBehaviour
    {
        public event EventHandler OnSelected = null!;

        public int ID;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Selected()
        {
            OnSelected?.Invoke(this, null);
        }
    }
}
