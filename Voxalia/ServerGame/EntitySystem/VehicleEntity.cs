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
using FreneticDataSyntax;

namespace Voxalia.ServerGame.EntitySystem
{
    public abstract class VehicleEntity : ModelEntity, EntityUseable
    {
        public static VehicleEntity CreateVehicleFor(Region tregion, string name)
        {
            if (!tregion.TheServer.Files.Exists("info/vehicles/" + name + ".fds"))
            {
                return null;
            }
            string dat = tregion.TheServer.Files.ReadText("info/vehicles/" + name + ".fds");
            FDSSection sect = new FDSSection(dat);
            FDSSection vehicleSect = sect.GetSection("vehicle");
            string typer = vehicleSect.GetString("type");
            if (string.IsNullOrWhiteSpace(typer))
            {
                SysConsole.Output(OutputType.WARNING, "Invalid vehicle type!");
                return null;
            }
            if (typer == "plane")
            {
                string model = vehicleSect.GetString("model");
                if (string.IsNullOrWhiteSpace(model))
                {
                    SysConsole.Output(OutputType.WARNING, "Invalid vehicle model!");
                    return null;
                }
                PlaneEntity pe = new PlaneEntity(vehicleSect.GetString("name"), tregion, model)
                {
                    SourceName = name,
                    SourceFile = sect
                };
                FDSSection forceSect = vehicleSect.GetSection("forces");
                pe.FastStrength = forceSect.GetDouble("strong").Value;
                pe.RegularStrength = forceSect.GetDouble("weak").Value;
                pe.StrPitch = forceSect.GetDouble("pitch").Value;
                pe.StrRoll = forceSect.GetDouble("roll").Value;
                pe.StrYaw = forceSect.GetDouble("yaw").Value;
                pe.WheelStrength = forceSect.GetDouble("wheels").Value;
                pe.TurnStrength = forceSect.GetDouble("turn").Value;
                pe.ForwardHelper = vehicleSect.GetDouble("forwardness_helper").Value;
                pe.LiftHelper = vehicleSect.GetDouble("lift_helper").Value;
                pe.ViewBackMultiplier = vehicleSect.GetFloat("view_distance", 7).Value;
                pe.CenterOfMassOffset = Location.FromString(vehicleSect.GetString("center_of_mass", "0,0,0"));
                return pe;
            }
            else
            {
                SysConsole.Output(OutputType.WARNING, "Invalid vehicle type: " + typer);
                return null;
            }
        }

        /// <summary>
        /// Offset to the center of mass.
        /// </summary>
        public Location CenterOfMassOffset = Location.Zero;

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.VEHICLE;
        }

        // TODO: Save with just the vehicle source file name?

        public string SourceName;

        public FDSSection SourceFile;

        public List<JointVehicleMotor> DrivingMotors = new List<JointVehicleMotor>();

        public List<JointVehicleMotor> SteeringMotors = new List<JointVehicleMotor>();

        public Seat DriverSeat;

        public string vehName;

        public float ViewBackMultiplier = 7;

        public void HandleFlapsInput(double yawmove, double pitchmove, double rollmove)
        {
            foreach (VehicleFlap vf in Flaps_Yaw)
            {
                vf.JVM.SetCorrectiveSpeed(vf.Speed);
                vf.JVM.SetGoal(yawmove * Utilities.PI180 * vf.MaxAngle);
            }
            foreach (VehicleFlap vf in Flaps_Pitch)
            {
                vf.JVM.SetCorrectiveSpeed(vf.Speed);
                vf.JVM.SetGoal(pitchmove * Utilities.PI180 * vf.MaxAngle);
            }
            foreach (VehicleFlap vf in Flaps_RollL)
            {
                vf.JVM.SetCorrectiveSpeed(vf.Speed);
                vf.JVM.SetGoal(rollmove * -Utilities.PI180 * vf.MaxAngle);
            }
            foreach (VehicleFlap vf in Flaps_RollR)
            {
                vf.JVM.SetCorrectiveSpeed(vf.Speed);
                vf.JVM.SetGoal(rollmove * Utilities.PI180 * vf.MaxAngle);
            }
        }

        public void HandleWheelsInput(CharacterEntity character)
        {
            HandleWheelsSpecificInput(character.YMove, character.XMove);
        }

        public double WheelStrength = 100, TurnStrength = 0.2;

        public void HandleWheelsSpecificInput(double ymove, double xmove)
        {
            foreach (JointVehicleMotor motor in DrivingMotors)
            {
                motor.Motor.Settings.VelocityMotor.GoalVelocity = ymove * WheelStrength;
            }
            foreach (JointVehicleMotor motor in SteeringMotors)
            {
                motor.Motor.Settings.Servo.Goal = MathHelper.Pi * -TurnStrength * xmove;
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

        public VehicleEntity(string vehicle, Region tregion, string model = null)
            : base(model ?? "vehicles/" + vehicle + "_base", tregion)
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
                string wheelsModFront = SourceFile.GetString("vehicle.wheels.front.model");
                string wheelsModBack = SourceFile.GetString("vehicle.wheels.back.model");
                double wheelsSuspFront = SourceFile.GetDouble("vehicle.wheels.front.suspension", 0.1).Value;
                double wheelsSuspBack = SourceFile.GetDouble("vehicle.wheels.back.suspension", 0.1).Value;
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
                    else if (name.Contains("flap"))
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
                        centerOfMass += (new Location(mat.M41, -mat.M43, mat.M42) + offset) * 20; // TODO: Arbitrary constant
                        mass += 20; // TODO: Arbitrary constant
                    }
                }
                if (mass > 0)
                {
                    centerOfMass /= mass;
                }
                Body.CollisionInformation.LocalPosition = -centerOfMass.ToBVector() - CenterOfMassOffset.ToBVector();
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
                        VehiclePartEntity wheel = new VehiclePartEntity(TheRegion, (name.After("wheel").Contains("f") ? wheelsModFront : wheelsModBack), true);
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
                            SteeringMotors.Add(ConnectWheel(wheel, false, true, wheelsSuspFront));
                            frontwheels.Add(wheel);
                        }
                        else if (name.After("wheel").Contains("b"))
                        {
                            DrivingMotors.Add(ConnectWheel(wheel, true, true, wheelsSuspBack));
                        }
                        else
                        {
                            ConnectWheel(wheel, true, false, wheelsSuspBack);
                        }
                        wheel.Body.ActivityInformation.Activate();
                    }
                    else if (name.Contains("flap"))
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
                        FDSSection flapDat = SourceFile.GetSection("vehicle.flaps").GetSection(name.After("flap").Replace("_", ""));
                        VehiclePartEntity flap = new VehiclePartEntity(TheRegion, flapDat.GetString("model"), true);
                        flap.SetPosition(pos);
                        flap.SetOrientation(Quaternion.Identity);
                        flap.Gravity = Gravity;
                        flap.CGroup = CGroup;
                        flap.SetMass(20);
                        flap.mode = ModelCollisionMode.CONVEXHULL;
                        TheRegion.SpawnEntity(flap);
                        flap.ForceNetwork();
                        flap.SetPosition(pos);
                        flap.SetOrientation(Quaternion.Identity);
                        ConnectFlap(flap, flapDat);
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

        public class VehicleFlap
        {
            public JointVehicleMotor JVM;

            public double MaxAngle;

            public double Speed;
        }

        public List<VehicleFlap> Flaps_RollR = new List<VehicleFlap>();

        public List<VehicleFlap> Flaps_RollL = new List<VehicleFlap>();

        public List<VehicleFlap> Flaps_Yaw = new List<VehicleFlap>();
        
        public List<VehicleFlap> Flaps_Pitch = new List<VehicleFlap>();

        public void ConnectFlap(VehiclePartEntity flap, FDSSection flapDat)
        {
            JointBallSocket jbs = new JointBallSocket(this, flap, flap.GetPosition()); // TODO: necessity?
            TheRegion.AddJoint(jbs);
            JointNoCollide jnc = new JointNoCollide(this, flap);
            TheRegion.AddJoint(jnc);
            string mode = flapDat.GetString("mode");
            VehicleFlap vf = new VehicleFlap()
            {
                MaxAngle = flapDat.GetDouble("max_angle", 10).Value,
                Speed = flapDat.GetDouble("corrective_speed", 2.25).Value
            };
            if (mode == "roll/l")
            {
                JointHinge jh = new JointHinge(this, flap, new Location(1, 0, 0));
                TheRegion.AddJoint(jh);
                JointVehicleMotor jvm = new JointVehicleMotor(this, flap, new Location(1, 0, 0), true);
                TheRegion.AddJoint(jvm);
                vf.JVM = jvm;
                Flaps_RollL.Add(vf);
            }
            else if (mode == "roll/r")
            {
                JointHinge jh = new JointHinge(this, flap, new Location(1, 0, 0));
                TheRegion.AddJoint(jh);
                JointVehicleMotor jvm = new JointVehicleMotor(this, flap, new Location(1, 0, 0), true);
                TheRegion.AddJoint(jvm);
                vf.JVM = jvm;
                Flaps_RollR.Add(vf);
            }
            else if (mode == "yaw")
            {
                JointHinge jh = new JointHinge(this, flap, new Location(0, 0, 1));
                TheRegion.AddJoint(jh);
                JointVehicleMotor jvm = new JointVehicleMotor(this, flap, new Location(0, 0, 1), true);
                TheRegion.AddJoint(jvm);
                vf.JVM = jvm;
                Flaps_Yaw.Add(vf);
            }
            else if (mode == "pitch")
            {
                JointHinge jh = new JointHinge(this, flap, new Location(1, 0, 0));
                TheRegion.AddJoint(jh);
                JointVehicleMotor jvm = new JointVehicleMotor(this, flap, new Location(1, 0, 0), true);
                TheRegion.AddJoint(jvm);
                vf.JVM = jvm;
                Flaps_Pitch.Add(vf);
            }
        }

        public JointVehicleMotor ConnectWheel(VehiclePartEntity wheel, bool driving, bool powered, double susp)
        {
            TheRegion.AddJoint(new ConstWheelStepUp(wheel, wheel.StepHeight));
            wheel.SetFriction(2.5f);
            Vector3 left = Quaternion.Transform(new Vector3(-1, 0, 0), wheel.GetOrientation());
            Vector3 up = Quaternion.Transform(new Vector3(0, 0, 1), wheel.GetOrientation());
            JointSlider pointOnLineJoint = new JointSlider(this, wheel, -new Location(up));
            JointLAxisLimit suspensionLimit = new JointLAxisLimit(this, wheel, 0f, susp, wheel.GetPosition(), wheel.GetPosition(), -new Location(up));
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
