using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.UI
{
    public class CommandButtonController : MonoBehaviour
    {
        public event EventHandler OnSelected;

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
