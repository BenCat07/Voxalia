//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.JointSystem;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class AddJointPacketOut : AbstractPacketOut
    {
        public AddJointPacketOut(InternalBaseJoint joint)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.ADD_JOINT;
            int len = 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4 + 24 + 4 + 4 + 4 + 4;
            // TODO: joint registry!
            if (joint is JointBallSocket)
            {
                Data = new byte[len + 24];
                Data[0] = 0;
                ((JointBallSocket)joint).Position.ToDoubleBytes().CopyTo(Data, len);
            }
            else if (joint is JointSlider)
            {
                Data = new byte[len + 24];
                Data[0] = 1;
                ((JointSlider)joint).Direction.ToDoubleBytes().CopyTo(Data, len);

            }
            else if (joint is JointDistance)
            {
                Data = new byte[len + 4 + 4 + 24 + 24];
                Data[0] = 2;
                Utilities.FloatToBytes((float)((JointDistance)joint).Min).CopyTo(Data, len);
                Utilities.FloatToBytes((float)((JointDistance)joint).Max).CopyTo(Data, len + 4);
                ((JointDistance)joint).Ent1Pos.ToDoubleBytes().CopyTo(Data, len + 4 + 4);
                ((JointDistance)joint).Ent2Pos.ToDoubleBytes().CopyTo(Data, len + 4 + 4 + 24);
            }
            else if (joint is JointPullPush)
            {
                Data = new byte[len + 24 + 1];
                Data[0] = 3;
                ((JointPullPush)joint).Axis.ToDoubleBytes().CopyTo(Data, len);
                Data[len + 24] = (byte)(((JointPullPush)joint).Mode ? 1 : 0);
            }
            else if (joint is JointForceWeld)
            {
                Data = new byte[len];
                Data[0] = 4;
            }
            else if (joint is JointSpinner)
            {
                Data = new byte[len + 24];
                Data[0] = 5;
                ((JointSpinner)joint).Direction.ToDoubleBytes().CopyTo(Data, len);
            }
            else if (joint is JointTwist)
            {
                Data = new byte[len + 24 + 24];
                Data[0] = 6;
                ((JointTwist)joint).AxisOne.ToDoubleBytes().CopyTo(Data, len);
                ((JointTwist)joint).AxisTwo.ToDoubleBytes().CopyTo(Data, len + 24);
            }
            else if (joint is JointWeld)
            {
                Data = new byte[len];
                Data[0] = 7;
            }
            else if (joint is JointVehicleMotor)
            {
                Data = new byte[len + 24 + 1];
                Data[0] = 8;
                ((JointVehicleMotor)joint).Direction.ToDoubleBytes().CopyTo(Data, len);
                Data[len + 24] = (byte)(((JointVehicleMotor)joint).IsSteering ? 1 : 0);
            }
            else if (joint is JointLAxisLimit)
            {
                Data = new byte[len + 24 + 24 + 24 + 4 + 4];
                Data[0] = 9;
                ((JointLAxisLimit)joint).CPos1.ToDoubleBytes().CopyTo(Data, len);
                ((JointLAxisLimit)joint).CPos2.ToDoubleBytes().CopyTo(Data, len + 24);
                ((JointLAxisLimit)joint).Axis.ToDoubleBytes().CopyTo(Data, len + 24 + 24);
                Utilities.FloatToBytes((float)((JointLAxisLimit)joint).Min).CopyTo(Data, len + 24 + 24 + 24);
                Utilities.FloatToBytes((float)((JointLAxisLimit)joint).Max).CopyTo(Data, len + 24 + 24 + 24 + 4);
            }
            else if (joint is JointSwivelHinge)
            {
                Data = new byte[len + 24 + 24];
                Data[0] = 10;
                ((JointSwivelHinge)joint).WorldHinge.ToDoubleBytes().CopyTo(Data, len);
                ((JointSwivelHinge)joint).WorldTwist.ToDoubleBytes().CopyTo(Data, len + 24);
            }
            else if (joint is ConstWheelStepUp)
            {
                Data = new byte[len + 4];
                Data[0] = 11;
                Utilities.FloatToBytes((float)((ConstWheelStepUp)joint).Height).CopyTo(Data, len);
            }
            else if (joint is ConnectorBeam)
            {
                Data = new byte[len + 4 * 4 + 1];
                Data[0] = 12;
                Utilities.FloatToBytes(((ConnectorBeam)joint).color.R).CopyTo(Data, len);
                Utilities.FloatToBytes(((ConnectorBeam)joint).color.G).CopyTo(Data, len + 4);
                Utilities.FloatToBytes(((ConnectorBeam)joint).color.B).CopyTo(Data, len + 4 * 2);
                Utilities.FloatToBytes(((ConnectorBeam)joint).color.A).CopyTo(Data, len + 4 * 3);
                Data[len + 4 * 4] = (byte)((ConnectorBeam)joint).type;
            }
            else if (joint is JointFlyingDisc)
            {
                Data = new byte[len];
                Data[0] = 13;
            }
            else if (joint is JointNoCollide)
            {
                Data = new byte[len];
                Data[0] = 14;
            }
            else if (joint is JointHinge)
            {
                Data = new byte[len + 24];
                Data[0] = 15;
                ((JointHinge)joint).WorldHinge.ToDoubleBytes().CopyTo(Data, len);
            }
            Utilities.LongToBytes(joint.One.EID).CopyTo(Data, 1);
            Utilities.LongToBytes(joint.Two.EID).CopyTo(Data, 1 + 8);
            Utilities.LongToBytes(joint.JID).CopyTo(Data, 1 + 8 + 8);
            joint.One.GetPosition().ToDoubleBytes().CopyTo(Data, 1 + 8 + 8 + 8);
            BEPUutilities.Quaternion quat = joint.One.GetOrientation();
            Utilities.FloatToBytes((float)quat.X).CopyTo(Data, 1 + 8 + 8 + 8 + 24);
            Utilities.FloatToBytes((float)quat.Y).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4);
            Utilities.FloatToBytes((float)quat.Z).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4);
            Utilities.FloatToBytes((float)quat.W).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4);
            joint.Two.GetPosition().ToDoubleBytes().CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4);
            BEPUutilities.Quaternion quat2 = joint.Two.GetOrientation();
            Utilities.FloatToBytes((float)quat2.X).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4 + 24);
            Utilities.FloatToBytes((float)quat2.Y).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4 + 24 + 4);
            Utilities.FloatToBytes((float)quat2.Z).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4 + 24 + 4 + 4);
            Utilities.FloatToBytes((float)quat2.W).CopyTo(Data, 1 + 8 + 8 + 8 + 24 + 4 + 4 + 4 + 4 + 24 + 4 + 4 + 4);
        }
    }
}
