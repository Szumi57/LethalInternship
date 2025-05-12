using System.Collections.Generic;
using LethalInternship.Interfaces;

namespace LethalInternship.UI.Icons.Pools
{
    // https://gist.github.com/Maxstupo/141942f1ea74fd4de0342e54798b88f1
    public abstract class IconUIPoolBase<T> where T : IIconUI
    {
        private List<T> icons;

        protected IconUIPoolBase()
        {
            this.icons = new List<T>();
        }

        protected abstract T NewIcon(IIconUIInfos iconInfos);

        public T GetIcon(IIconUIInfos iconInfos)
        {
            T icon;
            for (int i = icons.Count - 1; i >= 0; i--)
            {
                if (icons[i].Key == iconInfos.GetUIKey())
                {
                    icon = icons[i];
                    icons.RemoveAt(i);
                    return icon;
                }
            }

            return NewIcon(iconInfos);
        }

        public void ReturnIcon(T icon)
        {
            icons.Add(icon);
        }

        public void DisableOtherIcons()
        {
            foreach (T icon in icons)
            {
                icon.SetIconActive(false);
            }
        }
    }
}
