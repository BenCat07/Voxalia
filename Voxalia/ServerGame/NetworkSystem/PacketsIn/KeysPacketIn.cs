//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using FreneticGameCore.Files;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUutilities;
using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    class KeysPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(DataReader data)
        {
            int tid = data.ReadInt();
            KeysPacketData val = (KeysPacketData)data.ReadUShort();
            bool upw = val.HasFlag(KeysPacketData.UPWARD);
            bool downw = val.HasFlag(KeysPacketData.DOWNWARD);
            bool click = val.HasFlag(KeysPacketData.CLICK);
            bool aclick = val.HasFlag(KeysPacketData.ALTCLICK);
            bool use = val.HasFlag(KeysPacketData.USE);
            bool ileft = val.HasFlag(KeysPacketData.ITEMLEFT);
            bool iright = val.HasFlag(KeysPacketData.ITEMRIGHT);
            bool iup = val.HasFlag(KeysPacketData.ITEMUP);
            bool idown = val.HasFlag(KeysPacketData.ITEMDOWN);
            double yaw = data.ReadFloat();
            double pitch = data.ReadFloat();
            double x = data.ReadFloat();
            double y = data.ReadFloat();
            Location pos = data.ReadLocation();
            Location vel = data.ReadLocation();
            double sow = data.ReadFloat();
            Location itemdir = data.ReadLocation();
            Location isRel = data.ReadLocation();
            Vector2 tmove = new Vector2(x, y);
            if (tmove.LengthSquared() > 1.0)
            {
                tmove.Normalize();
            }
            if (itemdir.LengthSquared() != 1.0)
            {
                itemdir = itemdir.Normalize();
            }
            if (isRel.LengthSquared() > 3.0 * 3.0)
            {
                isRel = isRel.Normalize() * 3.0;
            }
            if (Player.Upward != upw || Player.Downward != downw || Player.Click != click || Player.AltClick != aclick
                || Player.Use != use || Math.Abs(Player.Direction.Yaw - yaw) > 0.05 || Math.Abs(Player.Direction.Pitch - pitch) > 0.05
                || Math.Abs(tmove.X - x) > 0.05 || Math.Abs(tmove.Y - y) > 0.05)
            {
                Player.NoteDidAction();
            }
            Player.Upward = upw;
            Player.Downward = downw;
            Player.Click = click;
            Player.AltClick = aclick;
            Player.Use = use;
            Player.ItemLeft = ileft;
            Player.ItemRight = iright;
            Player.ItemUp = iup;
            Player.ItemDown = idown;
            Player.LastKPI = Player.TheRegion.GlobalTickTime;
            Player.SprintOrWalk = sow;
            Player.ItemDir = itemdir;
            Player.ItemSourceRelative = isRel;
            if (Player.Flags.HasFlag(YourStatusFlags.NO_ROTATE))
            {
                Player.AttemptedDirectionChange.Yaw += yaw;
                Player.AttemptedDirectionChange.Pitch += pitch;
            }
            else
            {
                Player.Direction.Yaw = yaw;
                Player.Direction.Pitch = pitch;
            }
            Player.XMove = tmove.X;
            Player.YMove = tmove.Y;
            if (!Player.SecureMovement)
            {
                if (pos.IsNaN() || vel.IsNaN() || pos.IsInfinite() || vel.IsInfinite())
                {
                    return false;
                }
                Location up = new Location(0, 0, Player.CBHHeight);
                Location start = Player.GetPosition();
                Location rel = pos - start;
                double len = rel.Length();
                if (len > 50) // TODO: better sanity cap?
                {
                    return false;
                }
                rel /= len;
                if (Player.TheRegion.SpecialCaseConvexTrace(new BoxShape(1.1f, 1.1f, 1.1f), start + up, rel, (double)len, MaterialSolidity.FULLSOLID, Player.IgnoreThis, out RayCastResult rcr))
                {
                    Player.Teleport(start);
                }
                else
                {
                    Player.SetPosition(pos);
                }
                Player.SetVelocity(vel); // TODO: Validate velocity at all?
            }
            Player.Network.SendPacket(new YourPositionPacketOut(Player.TheRegion.GlobalTickTime, tid,
                Player.GetPosition(), Player.GetVelocity(), new Location(0, 0, 0), Player.CBody.StanceManager.CurrentStance, Player.pup));
            return true;
        }
    }
}
