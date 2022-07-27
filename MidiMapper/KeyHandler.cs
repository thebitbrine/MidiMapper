using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MidiMapper
{
    public class KeyHandler
    {
        #region Singleton
        private static KeyHandler instance;
        private KeyHandler()
        {
            currentlyPressedKeys = new List<Keys>();
        }

        public static KeyHandler Instance
        {
            get
            {
                if (instance is null)
                {
                    instance = new KeyHandler();
                }
                return instance;
            }
        }
        #endregion Singleton

        private List<Keys> currentlyPressedKeys;
        public List<Keys> GetCurrentlyPressedKeys { get { return currentlyPressedKeys; } }

        public void FireKeyDown(object sender, KeyEventArgs e)
        {
            if (!currentlyPressedKeys.Contains(e.KeyCode))
            {
                currentlyPressedKeys.Add(e.KeyCode);
                KeyEventKeyPressed(sender, e);
            }
        }

        public void FireKeyUp(object sender, KeyEventArgs e)
        {
            currentlyPressedKeys.Remove(e.KeyCode);
            KeyEventKeyReleased(sender, e);
        }

        public event EventHandler<KeyEventArgs> KeyPressed;
        protected virtual void KeyEventKeyPressed(object sender, KeyEventArgs e)
        {
            EventHandler<KeyEventArgs> handler = KeyPressed;
            handler?.Invoke(sender, e);
        }

        public event EventHandler<KeyEventArgs> KeyReleased;
        protected virtual void KeyEventKeyReleased(object sender, KeyEventArgs e)
        {
            EventHandler<KeyEventArgs> handler = KeyReleased;
            handler?.Invoke(sender, e);
        }

        public void Init()
        {
            // Nothing to initialize yet
        }
    }
}
