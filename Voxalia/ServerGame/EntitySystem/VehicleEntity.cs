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
using System.Threading.Tasks;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.ServerGame.JointSystem;
using BEPUutilities;
using Voxalia.ServerGame.OtherSystems;
using FreneticGameCore;
using Voxalia.ServerGame.NetworkSystem;

namespace Voxalia.ServerGame.EntitySystem
{
    public abstract class VehicleEntity : ModelEntity, EntityUseable
    {
        public List<JointVehicleMotor> DrivingMotors = new List<JointVehicleMotor>();

        public List<JointVehicleMotor> SteeringMotors = new List<JointVehicleMotor>();

        public Seat DriverSeat;

        public string vehName;

        public void HandleWheelsInput(CharacterEntity character)
        {
            HandleWheelsSpecificInput(character.YMove, character.XMove);
        }

        public void HandleWheelsSpecificInput(double ymove, double xmove)
        {
            // TODO: Share with clients properly.
            // TODO: Dynamic multiplier values.
            foreach (JointVehicleMotor motor in DrivingMotors)
            {
                motor.Motor.Settings.VelocityMotor.GoalVelocity = ymove * 100;
            }
            foreach (JointVehicleMotor motor in SteeringMotors)
            {
                motor.Motor.Settings.Servo.Goal = MathHelper.Pi * -0.2f * xmove;
            }
        }

        List<Model3DNode> GetNodes(Model3DNode node)
        {
            List<Model3DNode> nodes = new List<Model3DNode>()
            {
                node
            };
            if (node.Children.Count > 0)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    nodes.AddRange(GetNodes(node.Children[i]));
                }
            }
            return nodes;
        }

        public VehicleEntity(string vehicle, Region tregion)
            : base("vehicles/" + vehicle + "_base", tregion)
        {
            vehName = vehicle;
            mode = ModelCollisionMode.CONVEXHULL;
            SetMass(1500);
            DriverSeat = new Seat(this, Location.Zero); // TODO: proper placement
            Seats = new List<Seat>()
            {
                DriverSeat
            };
        }

        public double UseRelease = 0;

        public override void Tick()
        {
            if (UseRelease > 0)
            {
                UseRelease -= TheRegion.Delta;
            }
            else if (Driver != null && Driver.Use)
            {
                DriverSeat.Kick();
                UseRelease = 0.5;
            }
            base.Tick();
        }

        public void StartUse(Entity user)
        {
            if (UseRelease > 0)
            {
                return;
            }
            UseRelease = 0.5;
            DriverSeat.Accept(user as PhysicsEntity);
        }

        public CharacterEntity Driver
        {
            get
            {
                return DriverSeat.Sitter as CharacterEntity;
            }
        }

        public override void SendUpdate(PlayerEntity player, AbstractPacketOut packet)
        {
            if (Driver == null || Driver.EID != player.EID)
            {
                base.SendUpdate(player, packet);
            }
        }

        public override void SendSpawnPacket(PlayerEntity player)
        {
            base.SendSpawnPacket(player);
            if (Driver != null)
            {
                GainControlOfVehiclePacketOut gcovpo = new GainControlOfVehiclePacketOut(Driver, this);
                player.Network.SendPacket(gcovpo);
            }
        }
        
        public void StopUse(Entity user)
        {
            // Do nothing.
        }

        public virtual void Accepted(CharacterEntity character, Seat seat)
        {
            GainControlOfVehiclePacketOut gcovpo = new GainControlOfVehiclePacketOut(character, this);
            TheRegion.SendToVisible(lPos, gcovpo);
        }

        public virtual void SeatKicked(CharacterEntity character, Seat seat)
        {
            LoseControlOfVehiclePacketOut gcovpo = new LoseControlOfVehiclePacketOut(character, this);
            TheRegion.SendToVisible(lPos, gcovpo);
        }

        public abstract void HandleInput(CharacterEntity character);

        public bool hasWheels = false;

        public void HandleWheels()
        {
            if (!hasWheels)
            {
                Model mod = TheServer.Models.GetModel(model);
                if (mod == null) // TODO: mod should return a cube when all else fails?
                {
                    return;
                }
                Model3D scene = mod.Original;
                if (scene == null) // TODO: Scene should return a cube when all else fails?
                {
                    return;
                }
                SetOrientation(Quaternion.Identity); // TODO: Track and reset orientation maybe?
                List<Model3DNode> nodes = GetNodes(scene.RootNode);
                List<VehiclePartEntity> frontwheels = new List<VehiclePartEntity>();
                Location centerOfMass = Location.Zero;
                double mass = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    string name = nodes[i].Name.ToLowerFast();
                    if (name.Contains("wheel"))
                    {
                        Matrix mat = nodes[i].MatrixA;
                        mat.Transpose();
                        Model3DNode tnode = nodes[i].Parent;
                        while (tnode != null)
                        {
                            Matrix mb = tnode.MatrixA;
                            mb.Transpose();
                            mat = mat * mb;
                            tnode = tnode.Parent;
                        }
                        centerOfMass += (new Location(mat.M41, -mat.M43, mat.M42) + offset) * 30; // TODO: Arbitrary constant
                        mass += 30; // TODO: Arbitrary constant
                    }
                }
                if (mass > 0)
                {
                    centerOfMass /= mass;
                }
                Body.CollisionInformation.LocalPosition = -centerOfMass.ToBVector();
                ForceNetwork();
                for (int i = 0; i < nodes.Count; i++)
                {
                    string name = nodes[i].Name.ToLowerFast();
                    if (name.Contains("wheel"))
                    {
                        Matrix mat = nodes[i].MatrixA;
                        mat.Transpose();
                        Model3DNode tnode = nodes[i].Parent;
                        while (tnode != null)
                        {
                            Matrix mb = tnode.MatrixA;
                            mb.Transpose();
                            mat = mat * mb;
                            tnode = tnode.Parent;
                        }
                        Location pos = GetPosition() + new Location(Body.CollisionInformation.LocalPosition) + new Location(mat.M41, -mat.M43, mat.M42) + offset; // TODO: matrix gone funky?
                        VehiclePartEntity wheel = new VehiclePartEntity(TheRegion, "vehicles/" + vehName + "_wheel", true);
                        wheel.SetPosition(pos);
                        wheel.SetOrientation(Quaternion.Identity);
                        wheel.Gravity = Gravity;
                        wheel.CGroup = CGroup;
                        wheel.SetMass(30); // TODO: Arbitrary constant
                        wheel.mode = ModelCollisionMode.CONVEXHULL;
                        TheRegion.SpawnEntity(wheel);
                        wheel.ForceNetwork();
                        wheel.SetPosition(pos);
                        wheel.SetOrientation(Quaternion.Identity);
                        if (name.After("wheel").Contains("f"))
                        {
                            SteeringMotors.Add(ConnectWheel(wheel, false, true));
                            frontwheels.Add(wheel);
                        }
                        else if (name.After("wheel").Contains("b"))
                        {
                            DrivingMotors.Add(ConnectWheel(wheel, true, true));
                        }
                        else
                        {
                            ConnectWheel(wheel, true, false);
                        }
                        wheel.Body.ActivityInformation.Activate();
                    }
                }
                if (frontwheels.Count == 2)
                {
                    JointSpinner js = new JointSpinner(frontwheels[0], frontwheels[1], new Location(1, 0, 0));
                    TheRegion.AddJoint(js);
                }
                hasWheels = true;
            }
        }

        public JointVehicleMotor ConnectWheel(VehiclePartEntity wheel, bool driving, bool powered)
        {
            TheRegion.AddJoint(new ConstWheelStepUp(wheel, wheel.StepHeight));
            wheel.SetFriction(2.5f);
            Vector3 left = Quaternion.Transform(new Vector3(-1, 0, 0), wheel.GetOrientation());
            Vector3 up = Quaternion.Transform(new Vector3(0, 0, 1), wheel.GetOrientation());
            JointSlider pointOnLineJoint = new JointSlider(this, wheel, -new Location(up));
            JointLAxisLimit suspensionLimit = new JointLAxisLimit(this, wheel, 0f, 0.1, wheel.GetPosition(), wheel.GetPosition(), -new Location(up)); // TODO: 0.1 -> arbitrary constant
            JointPullPush spring = new JointPullPush(this, wheel, -new Location(up), true);
            if (driving)
            {
                JointSpinner spinner = new JointSpinner(this, wheel, new Location(-left));
                TheRegion.AddJoint(spinner);
            }
            else
            {
                JointSwivelHinge swivelhinge = new JointSwivelHinge(this, wheel, new Location(up), new Location(-left));
                TheRegion.AddJoint(swivelhinge);
            }
            TheRegion.AddJoint(pointOnLineJoint);
            TheRegion.AddJoint(suspensionLimit);
            TheRegion.AddJoint(spring);
            JointNoCollide jnc = new JointNoCollide(this, wheel);
            TheRegion.AddJoint(jnc);
            if (powered)
            {
                JointVehicleMotor motor = new JointVehicleMotor(this, wheel, new Location(driving ? left : up), !driving);
                TheRegion.AddJoint(motor);
                return motor;
            }
            return null;
        }
    }
}
