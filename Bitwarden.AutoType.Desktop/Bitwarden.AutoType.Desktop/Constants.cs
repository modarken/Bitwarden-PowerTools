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
            1 = LeftButton
            2 = RightButton
            3 = Cancel
            4 = MiddleButton
            5 = ExtraButton1
            6 = ExtraButton2
            8 = Back
            9 = Tab
            12 = Clear
            13 = Return
            16 = Shift
            17 = Control
            18 = Menu
            19 = Pause
            20 = CapsLock
            21 = Kana/Hangeul/Hangul
            23 = Junja
            24 = Final
            25 = Hanja/Kanji
            27 = Escape
            28 = Convert
            29 = NonConvert
            30 = Accept
            31 = ModeChange
            32 = Space
            33 = Prior
            34 = Next
            35 = End
            36 = Home
            37 = Left
            38 = Up
            39 = Right
            40 = Down
            41 = Select
            42 = Print
            43 = Execute
            44 = Snapshot
            45 = Insert
            46 = Delete
            47 = Help
            48 = N0
            49 = N1
            50 = N2
            51 = N3
            52 = N4
            53 = N5
            54 = N6
            55 = N7
            56 = N8
            57 = N9
            65 = A
            66 = B
            67 = C
            68 = D
            69 = E
            70 = F
            71 = G
            72 = H
            73 = I
            74 = J
            75 = K
            76 = L
            77 = M
            78 = N
            79 = O
            80 = P
            81 = Q
            82 = R
            83 = S
            84 = T
            85 = U
            86 = V
            87 = W
            88 = X
            89 = Y
            90 = Z
            91 = LeftWindows
            92 = RightWindows
            93 = Application
            95 = Sleep
            96 = Numpad0
            97 = Numpad1
            98 = Numpad2
            99 = Numpad3
            100 = Numpad4
            101 = Numpad5
            102 = Numpad6
            103 = Numpad7
            104 = Numpad8
            105 = Numpad9
            106 = Multiply
            107 = Add
            108 = Separator
            109 = Subtract
            110 = Decimal
            111 = Divide
            112 = F1
            113 = F2
            114 = F3
            115 = F4
            116 = F5
            117 = F6
            118 = F7
            119 = F8
            120 = F9
            121 = F10
            122 = F11
            123 = F12
            124 = F13
            125 = F14
            126 = F15
            127 = F16
            128 = F17
            129 = F18
            130 = F19
            131 = F20
            132 = F21
            133 = F22
            134 = F23
            135 = F24
            144 = NumLock
            145 = ScrollLock
            146 = NEC_Equal/Fujitsu_Jisho
            147 = Fujitsu_Masshou
            148 = Fujitsu_Touroku
            149 = Fujitsu_Loya
            150 = Fujitsu_Roya
            160 = LeftShift
            161 = RightShift
            162 = LeftControl
            163 = RightControl
            164 = LeftMenu
            165 = RightMenu
            166 = BrowserBack
            167 = BrowserForward
            168 = BrowserRefresh
            169 = BrowserStop
            170 = BrowserSearch
            171 = BrowserFavorites
            172 = BrowserHome
            173 = VolumeMute
            174 = VolumeDown
            175 = VolumeUp
            176 = MediaNextTrack
            177 = MediaPrevTrack
            178 = MediaStop
            179 = MediaPlayPause
            180 = LaunchMail
            181 = LaunchMediaSelect
            182 = LaunchApplication1
            183 = LaunchApplication2
            186 = OEM1
            187 = OEMPlus
            188 = OEMComma
            189 = OEMMinus
            190 = OEMPeriod
            191 = OEM2
            192 = OEM3
            219 = OEM4
            220 = OEM5
            221 = OEM6
            222 = OEM7
            223 = OEM8
            225 = OEMAX
            226 = OEM102
            227 = ICOHelp
            228 = ICO00
            229 = ProcessKey
            230 = ICOClear
            231 = Packet
            233 = OEMReset
            234 = OEMJump
            235 = OEMPA1
            236 = OEMPA2
            237 = OEMPA3
            238 = OEMWSCtrl
            239 = OEMCUSel
            240 = OEMATTN
            241 = OEMFinish
            242 = OEMCopy
            243 = OEMAuto
            244 = OEMENLW
            245 = OEMBackTab
            246 = ATTN
            247 = CRSel
            248 = EXSel
            249 = EREOF
            250 = Play
            251 = Zoom
            252 = Noname
            253 = PA1
            254 = OEMClear
            """;
    }
}
