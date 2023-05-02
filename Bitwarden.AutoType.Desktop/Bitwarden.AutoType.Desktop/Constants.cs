using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitwarden.AutoType.Desktop
{
    internal static class Constants
    {
        internal const string BitwardenClientConfigurationDeviceName = "autotype";
        internal const string BitwardenDefaultSequence = "{USERNAME}{TAB}{PASSWORD}{ENTER}";
        internal const string BitwardenCustomFieldName = "AutoType:Custom";
        internal const string DefaultAddFieldToBitwardenHelpText =
            """
            1. Find a target window by dragging the target icon over the window.
            2. Determine the "Type" of the Window to match by, either the Title, Process Name, or Class Name of the window.
            3. Select the combobox to choose the "Type" of the Window to match by.
            4. Observe the "Target Regex" textbox to see the regex that will be used to match the window, modiify as needed.
            5. Obseve the "Keyboard" textbox to see the keyboard sequence that will be sent to the window, modify as needed.
            6. When satisifed with the "Target Regex" and "Keyboard" textbox values, Open Bitwarden.
            7. Select an entry in Bitwarden to add the field to.
            8. Add a Custom Field with the name of "AutoType:Custom", which is the "Custom Name" field in AutoType.
            9. For the Custom Field value, copy and paste the "Custom Value" field from AutoType.
            """;

        internal const string DefaultSequenceHelpText =
            """
            {NAME} - Name of the login item
            {USERNAME} - Username of the login item
            {PASSWORD} - Password of the login item
            {URL} - URL of the login item
            {NOTES} - Notes of the login item
            {TOTP} - Time-based one-time password of the login item
            {CUSTOM:FieldName} - Custom field value of the login item, example {CUSTOM:pin}

            {LEFTCURLYBRACE} - Left curly brace key
            {RIGHTCURLYBRACE} - Right curly brace key
            {SHIFT} - Shift key
            {RIGHTSHIFT} - Right Shift key
            {LEFTSHIFT} - Left Shift key
            {ALT} - Alt key
            {LEFTALT} - Left Alt key
            {RIGHTALT} - Right Alt key
            {CONTROL} - Control key
            {LEFTCONTROL} - Left Control key
            {RIGHTCONTROL} - Right Control key
            {TAB} - Tab key
            {LEFTWINDOWS} - Left Windows key
            {RIGHTWINDOWS} - Right Windows key
            {ENTER} - Enter key
            {BACK} - Backspace key
            {SPACE} - Space key
            {LEFT} - Left arrow key
            {DOWN} - Down arrow key
            {RIGHT} - Right arrow key
            {UP} - Up arrow key
            {INSERT} - Insert key
            {DELETE} - Delete key
            {HOME} - Home key
            {END} - End key
            {PGUP} - Page Up key
            {PGDOWN} - Page Down key
            {CAPSLOCK} - Caps Lock key
            {ESCAPE} - Escape key
            {NUMLOCK} - Num Lock key
            {PRINTSCREEN} - Print Screen key
            {SCROLLLOCK} - Scroll Lock key
            {F1} - F1 key
            {F2} - F2 key
            {F3} - F3 key
            {F4} - F4 key
            {F5} - F5 key
            {F6} - F6 key
            {F7} - F7 key
            {F8} - F8 key
            {F9} - F9 key
            {F10} - F10 key
            {F11} - F11 key
            {F12} - F12 key

            Key Press Time:
            {ENTER:100} - Enter key with 100ms press time

            Key Press Direction:
            {SPACE:Up} - Space key with up action.
            {SPACE:Down} - Space key with down action.
            {SPACE:Press} - Space key with down, then up action.

            Delay:
            {4000} - 4000ms delay

            Virtual Key Codes:
            {VK54} - Virtual key code 54, which is the number 6 key

            Virtual Key Codes List:
            LeftButton = 1
            RightButton = 2
            Cancel = 3
            MiddleButton = 4
            ExtraButton1 = 5
            ExtraButton2 = 6
            Back = 8
            Tab = 9
            Clear = 12
            Return = 13
            Shift = 16
            Control = 17
            Menu = 18
            Pause = 19
            CapsLock = 20
            Kana = 21
            Hangeul = 21
            Hangul = 21
            Junja = 23
            Final = 24
            Hanja = 25
            Kanji = 25
            Escape = 27
            Convert = 28
            NonConvert = 29
            Accept = 30
            ModeChange = 31
            Space = 32
            Prior = 33
            Next = 34
            End = 35
            Home = 36
            Left = 37
            Up = 38
            Right = 39
            Down = 40
            Select = 41
            Print = 42
            Execute = 43
            Snapshot = 44
            Insert = 45
            Delete = 46
            Help = 47
            N0 = 48
            N1 = 49
            N2 = 50
            N3 = 51
            N4 = 52
            N5 = 53
            N6 = 54
            N7 = 55
            N8 = 56
            N9 = 57
            A = 65
            B = 66
            C = 67
            D = 68
            E = 69
            F = 70
            G = 71
            H = 72
            I = 73
            J = 74
            K = 75
            L = 76
            M = 77
            N = 78
            O = 79
            P = 80
            Q = 81
            R = 82
            S = 83
            T = 84
            U = 85
            V = 86
            W = 87
            X = 88
            Y = 89
            Z = 90
            LeftWindows = 91
            RightWindows = 92
            Application = 93
            Sleep = 95
            Numpad0 = 96
            Numpad1 = 97
            Numpad2 = 98
            Numpad3 = 99
            Numpad4 = 100
            Numpad5 = 101
            Numpad6 = 102
            Numpad7 = 103
            Numpad8 = 104
            Numpad9 = 105
            Multiply = 106
            Add = 107
            Separator = 108
            Subtract = 109
            Decimal = 110
            Divide = 111
            F1 = 112
            F2 = 113
            F3 = 114
            F4 = 115
            F5 = 116
            F6 = 117
            F7 = 118
            F8 = 119
            F9 = 120
            F10 = 121
            F11 = 122
            F12 = 123
            F13 = 124
            F14 = 125
            F15 = 126
            F16 = 127
            F17 = 128
            F18 = 129
            F19 = 130
            F20 = 131
            F21 = 132
            F22 = 133
            F23 = 134
            F24 = 135
            NumLock = 144
            ScrollLock = 145
            NEC_Equal = 146
            Fujitsu_Jisho = 146
            Fujitsu_Masshou = 147
            Fujitsu_Touroku = 148
            Fujitsu_Loya = 149
            Fujitsu_Roya = 150
            LeftShift = 160
            RightShift = 161
            LeftControl = 162
            RightControl = 163
            LeftMenu = 164
            RightMenu = 165
            BrowserBack = 166
            BrowserForward = 167
            BrowserRefresh = 168
            BrowserStop = 169
            BrowserSearch = 170
            BrowserFavorites = 171
            BrowserHome = 172
            VolumeMute = 173
            VolumeDown = 174
            VolumeUp = 175
            MediaNextTrack = 176
            MediaPrevTrack = 177
            MediaStop = 178
            MediaPlayPause = 179
            LaunchMail = 180
            LaunchMediaSelect = 181
            LaunchApplication1 = 182
            LaunchApplication2 = 183
            OEM1 = 186
            OEMPlus = 187
            OEMComma = 188
            OEMMinus = 189
            OEMPeriod = 190
            OEM2 = 191
            OEM3 = 192
            OEM4 = 219
            OEM5 = 220
            OEM6 = 221
            OEM7 = 222
            OEM8 = 223
            OEMAX = 225
            OEM102 = 226
            ICOHelp = 227
            ICO00 = 228
            ProcessKey = 229
            ICOClear = 230
            Packet = 231
            OEMReset = 233
            OEMJump = 234
            OEMPA1 = 235
            OEMPA2 = 236
            OEMPA3 = 237
            OEMWSCtrl = 238
            OEMCUSel = 239
            OEMATTN = 240
            OEMFinish = 241
            OEMCopy = 242
            OEMAuto = 243
            OEMENLW = 244
            OEMBackTab = 245
            ATTN = 246
            CRSel = 247
            EXSel = 248
            EREOF = 249
            Play = 250
            Zoom = 251
            Noname = 252
            PA1 = 253
            OEMClear = 254
            """;
    }
}
