using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace Clunker.Input
{
    public class InputTracker
    {
        private static HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private static HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private static HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private static HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();

        public static Vector2 MousePosition { get; private set; }
        public static Vector2 MouseDelta { get; private set; }

        public static float WheelDelta => FrameSnapshot.WheelDelta;
        public static InputSnapshot FrameSnapshot { get; private set; }
        public static bool LockMouse { get; set; }

        public static bool IsKeyPressed(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public static bool WasKeyDowned(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        public static bool IsMouseButtonPressed(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public static bool WasMouseButtonDowned(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public static void UpdateFrameInput(Sdl2Window window, InputSnapshot snapshot)
        {
            FrameSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            window.CursorVisible = !LockMouse;

            if (LockMouse)
            {
                window.SetMousePosition(window.Width / 2, window.Height / 2);
                MousePosition = new Vector2(window.Width / 2, window.Height / 2);
                MouseDelta = snapshot.MousePosition - MousePosition;
            }
            else
            {
                var newPosition = snapshot.MousePosition;
                MouseDelta = newPosition - MousePosition;
                MousePosition = newPosition;
            }
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent ke = snapshot.KeyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private static void MouseUp(MouseButton mouseButton)
        {
            _currentlyPressedMouseButtons.Remove(mouseButton);
            _newMouseButtonsThisFrame.Remove(mouseButton);
        }

        private static void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private static void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
        }

        private static void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
            }
        }
    }
}
