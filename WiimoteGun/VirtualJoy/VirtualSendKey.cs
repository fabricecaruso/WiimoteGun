using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WiimoteGun
{
    class VirtualSendKey : IVirtualJoy
    {
        private InputConfig _kb;

        public VirtualSendKey()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(VirtualSendKey).Assembly.Location), ".emulationstation", "es_input.cfg");

#if DEBUG
            if (!File.Exists(path))
                path = Path.Combine(@"H:\[Emulz]\emulationstation", ".emulationstation", "es_input.cfg");
#endif

            if (!File.Exists(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".emulationstation", "es_input.cfg");

            if (File.Exists(path))
            {
                var inputConfig = EsInput.Load(path);
                if (inputConfig != null)
                    _kb = inputConfig.FirstOrDefault(c => c.DeviceName == "Keyboard" || c.Type == "keyboard");
            }
            
            if (_kb == null)
            {
                _kb = new InputConfig() { Type = "keyboard", DeviceName = "Keyboard", DeviceGUID = "-1" };
                _kb.Input = new List<Input>();
                _kb.Input.Add(new Input() { Name = InputKey.a, Type = "key", Id = 122, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.b, Type = "key", Id = 120, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.down, Type = "key", Id = 1073741905, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.hotkey, Type = "key", Id = 1073742052, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.left, Type = "key", Id = 1073741904, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.right, Type = "key", Id = 1073741903, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.select, Type = "key", Id = 8, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.start, Type = "key", Id = 13, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.up, Type = "key", Id = 1073741906, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.x, Type = "key", Id = 113, Value = 1 });
                _kb.Input.Add(new Input() { Name = InputKey.y, Type = "key", Id = 115, Value = 1 });
            }
        }

        public bool IsEnabled { get { return _kb != null; } }

        private void SendButtonState(InputKey nButton, bool value)
        {
            if (_kb == null)
                return;

            var input = _kb[nButton];
            if (input == null)
                return;

            if ((input.Id & SDLK_SCANCODE_MASK) == SDLK_SCANCODE_MASK)
            {
                var id = input.Id & ~SDLK_SCANCODE_MASK;
                switch (id)
                {
                    case 79:
                        Send(Keys.Right, value, true);
                        break;
                    case 80:
                        Send(Keys.Left, value, true);
                        break;
                    case 81:
                        Send(Keys.Down, value, true);
                        break;
                    case 82:
                        Send(Keys.Up, value, true); 
                        break;
                    case 0x39: // Maj
                        Send(Keys.ShiftKey, value, true);
                        break;
                    case 0xE0: // Left control
                        Send(Keys.ControlKey, value, false);
                        break;                
                    case 0xE1: // LShift
                        Send(Keys.ShiftKey, value, false);
                        break;
                    case 0xE2: // LAlt
                        Send(Keys.Alt, value, false);
                        break;
                    case 0xE4: // RCtrl
                        Send(Keys.ControlKey, value, true);
                        break;
                    case 0xE6: // RShift
                        Send(Keys.ShiftKey, value, true);
                        break;
                }
            }
            else if (input.Id < 255)
            {
                var key = ConvertCharToVirtualKey((char)input.Id);
                Send(key, value);
            }
            else
                SendScanCode((uint)input.Id, value);
        }

        class StateInfo
        {
            public bool State { get; set; }
            public bool Changed { get; set; }
        };

        private Dictionary<InputKey, StateInfo> _states = new Dictionary<InputKey, StateInfo>();

        private void SetState(InputKey key, bool value)
        {
            StateInfo info;
            if (!_states.TryGetValue(key, out info))
            {
                info = new StateInfo() { Changed = value };
                _states[key] = info;
            }

            if (value != info.State)
            {
                info.State = value;
                info.Changed = true;
            }
        }

        public void SetAxis(bool AxisX, int value)
        {
            if (AxisX)
            {
                if (value == 0)
                {
                    SetState(InputKey.left, false);
                    SetState(InputKey.right, false);
                }
                else if (value < 0)
                {
                    SetState(InputKey.left, false);
                    SetState(InputKey.right, true);
                }
                else
                {
                    SetState(InputKey.left, true);
                    SetState(InputKey.right, false);
                }
            }
            else
            {
                if (value == 0)
                {
                    SetState(InputKey.up, false);
                    SetState(InputKey.down, false);
                }
                else if (value < 0)
                {
                    SetState(InputKey.up, false);
                    SetState(InputKey.down, true);
                }
                else
                {
                    SetState(InputKey.up, true);
                    SetState(InputKey.down, false);
                }
            }

        }

        public void SetButton(uint nButton, bool value)
        {
            switch (nButton)
            {
                case 1:
                    SetState(InputKey.a, value);
                    break;
                case 2:
                    SetState(InputKey.b, value);
                    break;
                case 3:
                    SetState(InputKey.x, value);
                    break;
                case 4:
                    SetState(InputKey.y, value);
                    break;
                case 5:
              //      SetState(InputKey.select, value);

                    if (_kb != null && _kb[InputKey.hotkey] != null && _kb[InputKey.select] != null && _kb[InputKey.select].Id != _kb[InputKey.hotkey].Id)
                        SetState(InputKey.hotkey, value);

                    break;
                case 6:
                    SetState(InputKey.start, value);
                    break;
                case 7:
                    SetState(InputKey.hotkey, value);
                    break;
            }
        }

        public void CommitChanges()
        {
            foreach (var key in _states)
            {
                if (!key.Value.Changed)
                    continue;
                
                key.Value.Changed = false;
                SendButtonState(key.Key, key.Value.State);                                   
            }
        }

        /* cf. SDL_Scancode
        void SDL_GetScancodeFromKey(SDL_Keycode key)
        {
            SDL_Keyboard *keyboard = &SDL_keyboard;
            SDL_Scancode scancode;

            for (scancode = SDL_SCANCODE_UNKNOWN; scancode < SDL_NUM_SCANCODES; ++scancode) 
                if (keyboard->keymap[scancode] == key)
                    return scancode;
            
            return SDL_SCANCODE_UNKNOWN;
        }*/

        #region Apis
        public const int SDLK_SCANCODE_MASK = (1 << 30);

        public static Keys ConvertCharToVirtualKey(char ch)
        {
            short vkey = VkKeyScan(ch);
            Keys retval = (Keys)(vkey & 0xff);
            int modifiers = vkey >> 8;
            if ((modifiers & 1) != 0) retval |= Keys.Shift;
            if ((modifiers & 2) != 0) retval |= Keys.Control;
            if ((modifiers & 4) != 0) retval |= Keys.Alt;
            return retval;
        }

        public static void SendScanCode(uint scanCode, bool keyDown, bool isEXTEND = false)
        {           
            Debug.WriteLine(scanCode.ToString() + " " + (keyDown ? "(DOWN)" : "(UP)"));

            if (IntPtr.Size == 8)
            {
                INPUT64 inp = new INPUT64();

                inp.type = INPUT_KEYBOARD;
                inp.ki.wScan = (short)(scanCode & 0xFF);

                inp.ki.dwFlags = (uint)(KEYEVENTF_SCANCODE | (keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP));
                if ((scanCode & 0xE000) != 0)
                    inp.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;

                inp.ki.time = 0;
                inp.ki.dwExtraInfo = IntPtr.Zero;
                SendInput64(1, ref inp, Marshal.SizeOf(inp));
            }
            else
            {
                INPUT inp = new INPUT();

                inp.type = INPUT_KEYBOARD;
                inp.ki.wScan = (ushort)(scanCode & 0xFFFF);

                inp.ki.dwFlags = (uint)(KEYEVENTF_SCANCODE | (keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP));
                if ((scanCode & 0xE000) != 0)
                    inp.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;

                inp.ki.time = 0;
                inp.ki.dwExtraInfo = IntPtr.Zero;
                SendInput(1, ref inp, Marshal.SizeOf(inp));
            }
        }

        public static void Send(Keys key, bool keyDown, bool isEXTEND = false)
        {
            Debug.WriteLine(key.ToString() + " " + (keyDown ? "(DOWN)" : "(UP)"));

            if (IntPtr.Size == 8)
            {
                INPUT64 inp = new INPUT64();

                inp.type = INPUT_KEYBOARD;
                inp.ki.wVk = (ushort)key;
                inp.ki.wScan = (short)MapVirtualKey(inp.ki.wVk, 0);
                inp.ki.dwFlags = (uint)(((isEXTEND) ? (KEYEVENTF_EXTENDEDKEY) : 0x0) | (keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP));
                inp.ki.time = 0;
                inp.ki.dwExtraInfo = IntPtr.Zero;
                SendInput64(1, ref inp, Marshal.SizeOf(inp));
            }
            else
            {
                INPUT inp = new INPUT();

                inp.type = INPUT_KEYBOARD;
                inp.ki.wVk = (ushort)key;
                inp.ki.wScan = (ushort)MapVirtualKey(inp.ki.wVk, 0);
                inp.ki.dwFlags = (uint)(((isEXTEND) ? (KEYEVENTF_EXTENDEDKEY) : 0x0) | (keyDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP));
                inp.ki.time = 0;
                inp.ki.dwExtraInfo = IntPtr.Zero;
                SendInput(1, ref inp, Marshal.SizeOf(inp));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        };

        [StructLayout(LayoutKind.Explicit)]
        struct MouseKeybdHardwareInputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct KEYBDINPUT64
        {
            public ushort wVk;
            public short wScan;
            public uint dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct MOUSEINPUT64
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct HARDWAREINPUT64
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT64
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(8)]
            public MOUSEINPUT64 mi;
            [FieldOffset(8)]
            public KEYBDINPUT64 ki;
            [FieldOffset(8)]
            public HARDWAREINPUT64 hi;
        }

        [DllImport("user32.dll")]
        private extern static short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private extern static void SendInput(int nInputs, ref INPUT pInputs, int cbsize);

        [DllImport("user32.dll", EntryPoint = "SendInput")]
        private extern static void SendInput64(int nInputs, ref INPUT64 pInputs, int cbsize);

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        private extern static int MapVirtualKey(int wCode, int wMapType);

        const int INPUT_MOUSE = 0;
        const int INPUT_KEYBOARD = 1;

        const int KEYEVENTF_KEYDOWN = 0x0;
        const int KEYEVENTF_EXTENDEDKEY = 0x1;
        const int KEYEVENTF_KEYUP = 0x2;
        const int KEYEVENTF_UNICODE = 0x4;
        const int KEYEVENTF_SCANCODE = 0x8;
        #endregion
    }
}
