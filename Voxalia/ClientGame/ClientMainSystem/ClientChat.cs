//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using Voxalia.Shared;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using OpenTK;
using OpenTK.Input;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        public UIGroup ChatMenu;

        public List<ChatMessage> ChatMessages = new List<ChatMessage>(600);
        
        public UIInputBox ChatBox;

        public UIScrollBox ChatScroller;

        public bool[] Channels;

        public void InitChatSystem()
        {
            FontSet font = FontSets.Standard;
            int minY = 10 + (int)font.font_default.Height;
            ChatMenu = new UIGroup(UIAnchor.TOP_CENTER, () => Window.Width, () => Window.Height - minY - UIBottomHeight, () => 0, () => 0);
            ChatScroller = new UIScrollBox(UIAnchor.TOP_CENTER, () => ChatMenu.GetWidth() - (30 * 2), () => ChatMenu.GetHeight() - minY, () => 0, () => minY) { Color = new Vector4(0f, 0.5f, 0.5f, 0.6f) };
            ChatBox = new UIInputBox("", "Enter a /command or a chat message...", font, UIAnchor.TOP_CENTER, ChatScroller.GetWidth, () => 0, () => (int)ChatScroller.GetHeight() + minY);
            ChatBox.EnterPressed = EnterChatMessage;
            ChatMenu.AddChild(ChatBox);
            ChatMenu.AddChild(ChatScroller);
            Channels = new bool[(int)TextChannel.COUNT];
            Func<int> xer = () => 30;
            Channels[0] = true;
            for (int i = 1; i < Channels.Length; i++)
            {
                Channels[i] = true;
                string n = ((TextChannel)i).ToString();
                int len = (int)FontSets.Standard.MeasureFancyText(n);
                UITextLink link = null;
                Func<int> fxer = xer;
                int chan = i;
                link = new UITextLink(null, "^r^t^0^h^o^2" + n, "^!^e^t^0^h^o^2" + n, "^2^e^t^0^h^o^0" + n, FontSets.Standard, () => ToggleLink(link, n, chan), UIAnchor.TOP_LEFT, fxer, () => 10);
                xer = () => fxer() + len + 10;
                ChatMenu.AddChild(link);
            }
            ClearChat();
            ChatScrollToBottom();
        }

        void EnterChatMessage()
        {
            if (ChatBox.Text.Length == 0)
            {
                CloseChat();
                return;
            }
            if (ChatBox.Text.StartsWith("/"))
            {
                Commands.ExecuteCommands(ChatBox.Text);
            }
            else
            {
                CommandPacketOut packet = new CommandPacketOut("say\n" + ChatBox.Text);
                Network.SendPacket(packet);
            }
            CloseChat();
        }

        void ToggleLink(UITextLink link, string n, int chan)
        {
            char c = '2';
            Channels[chan] = !Channels[chan];
            if (!Channels[chan])
            {
                c = '&';
            }
            link.Text = "^r^t^0^h^o^" + c + n;
            link.TextHover = "^!^e^t^0^h^o^" + c + n;
            link.TextClick = "^" + c + "^e^t^0^h^o^0" + n;
            UpdateChats();
        }

        bool WVis = false;

        public bool ChatBottomLastTick = true;

        public void TickChatSystem()
        {
            if (IsChatVisible())
            {
                ChatBottomLastTick = ChatIsAtBottom();
                if (ChatBox.TriedToEscape)
                {
                    CloseChat();
                    return;
                }
                if (!WVis)
                {
                    KeyHandler.GetKBState();
                    WVis = true;
                }
                ChatBox.Selected = true;
            }
            else
            {
                ChatBottomLastTick = true;
            }
        }

        public void SetChatText(string text)
        {
            ChatBox.Text = text;
        }

        public string GetChatText()
        {
            return ChatBox.Text;
        }

        public void ShowChat()
        {
            if (!IsChatVisible())
            {
                KeyHandler.GetKBState();
                TheGameScreen.AddChild(ChatMenu);
                FixMouse();
            }
        }

        /// <summary>
        /// NOTE: Do not call this without good reason, let's not annoy players!
        /// </summary>
        public void CloseChat()
        {
            if (IsChatVisible())
            {
                KeyHandler.GetKBState();
                TheGameScreen.RemoveChild(ChatMenu);
                WVis = false;
                FixMouse();
                ChatBox.Clear();
            }
        }

        public void ClearChat()
        {
            ChatMessages.Clear();
            for (int i = 0; i < 100; i++)
            {
                ChatMessage cm = new ChatMessage() { Channel = TextChannel.ALWAYS, Text = "" };
                ChatMessages.Add(cm);
            }
            UpdateChats();
        }

        public bool IsChatVisible()
        {
            return TheGameScreen.HasChild(ChatMenu);
        }

        public void WriteMessage(TextChannel channel, string message)
        {
            bool bottomed = ChatIsAtBottom();
            UIConsole.WriteLine(channel + ": " + message);
            ChatMessage cm = new ChatMessage() { Channel = channel, Text = message };
            ChatMessages.Add(cm);
            if (ChatMessages.Count > 550)
            {
                ChatMessages.RemoveRange(0, 50);
            }
            UpdateChats();
            if (bottomed)
            {
                ChatScrollToBottom();
            }
        }

        public int ChatBottom = 0;

        public void ChatScrollToBottom()
        {
            ChatScroller.Scroll = ChatBottom;
        }

        public bool ChatIsAtBottom()
        {
            return ChatScroller.Scroll >= (ChatBottom - 5);
        }
        
        public void UpdateChats()
        {
            ChatScroller.RemoveAllChildren();
            float by = 0;
            for (int i = 0; i < ChatMessages.Count; i++)
            {
                if (Channels[(int)ChatMessages[i].Channel])
                {
                    by += FontSets.Standard.font_default.Height;
                    int y = (int)by;
                    string ch = (ChatMessages[i].Channel == TextChannel.ALWAYS) ? "" : (ChatMessages[i].Channel.ToString() + ": ");
                    ChatScroller.AddChild(new UILabel(ch + ChatMessages[i].Text, FontSets.Standard, UIAnchor.TOP_LEFT, () => 0, () => y, () => (int)ChatScroller.GetWidth()));
                }
            }
            by += FontSets.Standard.font_default.Height;
            ChatBottom = (int)(by - ChatScroller.GetHeight());
            ChatScroller.MaxScroll = ChatBottom;
        }
    }
}
