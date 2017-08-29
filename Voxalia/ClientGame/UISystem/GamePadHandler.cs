//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Input;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.UISystem
{
    /// <summary>
    /// All standard gamepad buttons.
    /// </summary>
    public enum GamePadButton : byte
    {
        X = 0,
        Y = 1,
        A = 2,
        B = 3,
        D_LEFT = 4,
        D_RIGHT = 5,
        D_UP = 6,
        D_DOWN = 7,
        LEFT_BUMPER = 8,
        RIGHT_BUMPER = 9,
        LEFT_STICK = 10,
        RIGHT_STICK = 11,
        LEFT_TRIGGER = 12,
        RIGHT_TRIGGER = 13,
        SELECT = 14,
        START = 15
    }

    public class GamePadHandler
    {
        /// <summary>
        /// Number of buttons on the gamepad.
        /// See <see cref="GamePadButton"/>.
        /// </summary>
        public const int GP_BUTTON_COUNT = 16;

        public CommandScript[] ButtonBinds;
        public CommandScript[] ButtonInverseBinds;
        public bool[] WasDown;
        public double[] StartedTime;

        public Client TheClient;

        public void Init(Client tclient)
        {
            // Set up
            TheClient = tclient;
            ButtonBinds = new CommandScript[GP_BUTTON_COUNT];
            ButtonInverseBinds = new CommandScript[GP_BUTTON_COUNT];
            WasDown = new bool[GP_BUTTON_COUNT];
            StartedTime = new double[GP_BUTTON_COUNT];
            // Default binds
            BindButton(GamePadButton.A, "+upward");
            BindButton(GamePadButton.B, "+movedown");
            BindButton(GamePadButton.X, "weaponreload");
            BindButton(GamePadButton.Y, "+use");
            BindButton(GamePadButton.RIGHT_TRIGGER, "+attack");
            BindButton(GamePadButton.LEFT_TRIGGER, "+secondary");
            BindButton(GamePadButton.LEFT_BUMPER, "itemprev");
            BindButton(GamePadButton.RIGHT_BUMPER, "itemnext");
            BindButton(GamePadButton.D_UP, "+itemup");
            BindButton(GamePadButton.D_DOWN, "+itemdown");
            BindButton(GamePadButton.D_LEFT, "+itemleft");
            BindButton(GamePadButton.D_RIGHT, "+itemright");
        }

        public bool Modified = false;

        /// <summary>
        /// Binds a button to a command.
        /// </summary>
        /// <param name="btn">The button to bind.</param>
        /// <param name="bind">The command to bind to it (null to unbind).</param>
        public void BindButton(GamePadButton btn, string bind)
        {
            ButtonBinds[(int)btn] = null;
            ButtonInverseBinds[(int)btn] = null;
            if (bind != null)
            {
                CommandScript script = CommandScript.SeparateCommands("bind_" + btn, bind, TheClient.Commands.CommandSystem);
                script.Debug = DebugMode.MINIMAL;
                ButtonBinds[(int)btn] = script;
                if (script.Created.Entries.Length == 1 && script.Created.Entries[0].Marker == 1)
                {
                    CommandEntry fixedentry = script.Created.Entries[0].Duplicate();
                    fixedentry.Marker = 2;
                    CommandScript nscript = new CommandScript("inverse_bind_" + btn, new List<CommandEntry>() { fixedentry }, mode: DebugMode.MINIMAL);
                    ButtonInverseBinds[(int)btn] = nscript;
                }
            }
            Modified = true;
        }

        /// <summary>
        /// Binds a button to a command.
        /// </summary>
        /// <param name="btn">The button to bind.</param>
        /// <param name="bind">The command to bind to it (null to unbind).</param>
        /// <param name="adj">adjustment location for the script.</param>
        public void BindButton(GamePadButton btn, List<CommandEntry> bind, int adj)
        {
            ButtonBinds[(int)btn] = null;
            ButtonInverseBinds[(int)btn] = null;
            if (bind != null)
            {
                CommandScript script = new CommandScript("_bind_for_" + btn, bind, adj)
                {
                    Debug = DebugMode.MINIMAL
                };
                ButtonBinds[(int)btn] = script;
                if (script.Created.Entries.Length == 1 && script.Created.Entries[0].Marker == 1)
                {
                    CommandEntry fixedentry = script.Created.Entries[0].Duplicate();
                    fixedentry.Marker = 2;
                    CommandScript nscript = new CommandScript("inverse_bind_" + btn, new List<CommandEntry>() { fixedentry }, mode: DebugMode.MINIMAL);
                    ButtonInverseBinds[(int)btn] = nscript;
                }
            }
            Modified = true;
        }

        /// <summary>
        /// Activates the "pressed" or released script for a specific button.
        /// </summary>
        /// <param name="btn">The button.</param>
        /// <param name="pressed">Whether the button is pressed.</param>
        public void Activate(GamePadButton btn, bool pressed)
        {
            if (pressed)
            {
                if (!WasDown[(int)btn] || TheClient.GlobalTickTimeLocal - StartedTime[(int)btn] > PressRepTime)
                {
                    if (ButtonBinds[(int)btn] != null)
                    {
                        CommandQueue queue = ButtonBinds[(int)btn].ToQueue(TheClient.Commands.CommandSystem);
                        queue.CommandStack.Peek().Debug = DebugMode.MINIMAL;
                        queue.Execute();
                    }
                }
                if (StartedTime[(int)btn] == 0)
                {
                    StartedTime[(int)btn] = TheClient.GlobalTickTimeLocal;
                }
            }
            else if (!pressed && WasDown[(int)btn])
            {
                if (ButtonInverseBinds[(int)btn] != null)
                {
                    CommandQueue queue = ButtonInverseBinds[(int)btn].ToQueue(TheClient.Commands.CommandSystem);
                    queue.CommandStack.Peek().Debug = DebugMode.MINIMAL;
                    queue.Execute();
                }
                StartedTime[(int)btn] = 0;
            }
            WasDown[(int)btn] = pressed;
        }

        /// <summary>
        /// Time to hold a button before it refires every tick.
        /// </summary>
        public double PressRepTime = 0.75;

        /// <summary>
        /// Minimum push on a stick before it activates. For error/calibration correction.
        /// </summary>
        public float MinimumPush = 0.1f;

        /// <summary>
        /// How sensitive the turning should be. Higher values = turn fast.
        /// </summary>
        public float TurnSensitivity = 1f;

        public Vector2 DirectionControl = Vector2.Zero; // TODO: Use these two vars properly.
        public Vector2 MovementControl = Vector2.Zero; // TODO: Also allow switching their places on the gamepad!

        public void Tick(double delta)
        {
            DirectionControl = Vector2.Zero;
            MovementControl = Vector2.Zero;
            for (int i = 0; i < 4; i++)
            {
                GamePadCapabilities cap = GamePad.GetCapabilities(i);
                if (cap.IsConnected)
                {
                    GamePadState state = GamePad.GetState(i);
                    if (cap.HasRightXThumbStick && Math.Abs(state.ThumbSticks.Right.X) > MinimumPush)
                    {
                        DirectionControl.X -= state.ThumbSticks.Right.X * TurnSensitivity;
                    }
                    if (cap.HasRightYThumbStick && Math.Abs(state.ThumbSticks.Right.Y) > MinimumPush)
                    {
                        DirectionControl.Y += state.ThumbSticks.Right.Y * TurnSensitivity;
                    }
                    if (cap.HasLeftXThumbStick && Math.Abs(state.ThumbSticks.Left.X) > MinimumPush)
                    {
                        MovementControl.X += state.ThumbSticks.Left.X;
                    }
                    if (cap.HasLeftYThumbStick && Math.Abs(state.ThumbSticks.Left.Y) > MinimumPush)
                    {
                        MovementControl.Y += state.ThumbSticks.Left.Y;
                    }
                    Activate(GamePadButton.LEFT_TRIGGER, cap.HasLeftTrigger && state.Triggers.Left > 0.8);
                    Activate(GamePadButton.RIGHT_TRIGGER, cap.HasRightTrigger && state.Triggers.Right > 0.8);
                    Activate(GamePadButton.Y, cap.HasYButton && state.Buttons.Y == ButtonState.Pressed);
                    Activate(GamePadButton.X, cap.HasXButton && state.Buttons.X == ButtonState.Pressed);
                    Activate(GamePadButton.A, cap.HasAButton && state.Buttons.A == ButtonState.Pressed);
                    Activate(GamePadButton.B, cap.HasBButton && state.Buttons.B == ButtonState.Pressed);
                    Activate(GamePadButton.LEFT_BUMPER, cap.HasLeftShoulderButton && state.Buttons.LeftShoulder == ButtonState.Pressed);
                    Activate(GamePadButton.RIGHT_BUMPER, cap.HasRightShoulderButton && state.Buttons.RightShoulder == ButtonState.Pressed);
                    Activate(GamePadButton.D_UP, cap.HasDPadUpButton && state.DPad.Up == ButtonState.Pressed);
                    Activate(GamePadButton.D_DOWN, cap.HasDPadDownButton && state.DPad.Down == ButtonState.Pressed);
                    Activate(GamePadButton.D_LEFT, cap.HasDPadLeftButton && state.DPad.Left == ButtonState.Pressed);
                    Activate(GamePadButton.D_RIGHT, cap.HasDPadRightButton && state.DPad.Right == ButtonState.Pressed);
                    Activate(GamePadButton.LEFT_STICK, cap.HasLeftStickButton && state.Buttons.LeftStick == ButtonState.Pressed);
                    Activate(GamePadButton.RIGHT_STICK, cap.HasRightStickButton && state.Buttons.RightStick == ButtonState.Pressed);
                }
            }
        }
    }
}
