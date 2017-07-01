//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.OtherSystems;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using Voxalia.ClientGame.JointSystem;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.EntitySystem
{
    public class ModelEntity: PhysicsEntity
    {
        public Model model;

        public Location scale = Location.One;

        public string mod;

        public Matrix4d transform;

        public Location Offset;

        public ModelCollisionMode mode = ModelCollisionMode.AABB;

        public BEPUutilities.Vector3 ModelMin;
        public BEPUutilities.Vector3 ModelMax;

        public ModelEntity(string model_in, Region tregion)
            : base(tregion, true, true)
        {
            mod = model_in;
        }

        public void TurnIntoPlane(PlayerEntity pilot) // TODO: Character!
        {
            PlanePilot = pilot;
            Plane = new PlaneMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Plane);
            foreach (InternalBaseJoint joint in Joints) // TODO: Just track this detail on the joint itself ffs
            {
                if (joint is JointFlyingDisc)
                {
                    ((FlyingDiscConstraint)((JointFlyingDisc)joint).CurrentJoint).IsAPlane = true;
                }
            }
            Body.LinearDamping = 0.0;
            WeakenThisAndJointed();
        }

        public float PlaneFastStrength
        {
            get
            {
                return GetMass() * 10f;
            }
        }

        public float PlaneRegularStrength
        {
            get
            {
                return GetMass() * 3f;
            }
        }

        public PlaneMotionConstraint Plane = null;

        public PlayerEntity PlanePilot = null; // TODO: Character!

        public class PlaneMotionConstraint : SingleEntityConstraint
        {
            ModelEntity Plane;
            
            public PlaneMotionConstraint(ModelEntity pln)
            {
                Plane = pln;
                Entity = pln.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Plane.PlanePilot == null)
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // TODO: Special case for motion on land: only push forward if FORWARD key is pressed? Or maybe apply that rule in general?
                // Collect the plane's relative vectors
                BEPUutilities.Vector3 forward = BEPUutilities.Quaternion.Transform(BEPUutilities.Vector3.UnitY, Entity.Orientation);
                BEPUutilities.Vector3 side = BEPUutilities.Quaternion.Transform(BEPUutilities.Vector3.UnitX, Entity.Orientation);
                BEPUutilities.Vector3 up = BEPUutilities.Quaternion.Transform(BEPUutilities.Vector3.UnitZ, Entity.Orientation);
                // Engines!
                if (Plane.PlanePilot.SprintOrWalk >= 0.0)
                {
                    BEPUutilities.Vector3 force = forward * (Plane.PlaneRegularStrength +  Plane.PlaneFastStrength * Plane.PlanePilot.SprintOrWalk) * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                double dotforw = BEPUutilities.Vector3.Dot(entity.LinearVelocity, forward);
                double mval = 2.0 * (1.0 / Math.Max(1.0, entity.LinearVelocity.Length()));
                double rot_x = -Plane.PlanePilot.YMove * 0.5 * Delta * dotforw * mval;
                double rot_y = Plane.PlanePilot.XMove * dotforw * 0.5 * Delta * mval;
                double rot_z = -((Plane.PlanePilot.ItemRight ? 1 : 0) + (Plane.PlanePilot.ItemLeft ? -1 : 0)) * dotforw * 0.1 * Delta * mval;
                entity.AngularVelocity += BEPUutilities.Quaternion.Transform(new BEPUutilities.Vector3(rot_x, rot_y, rot_z), entity.Orientation);
                double vellen = entity.LinearVelocity.Length();
                BEPUutilities.Vector3 newVel = forward * vellen;
                double forwVel = BEPUutilities.Vector3.Dot(entity.LinearVelocity, forward);
                double root = Math.Sqrt(Math.Sign(forwVel) * forwVel);
                entity.LinearVelocity += (newVel - entity.LinearVelocity) * BEPUutilities.MathHelper.Clamp(Delta, 0.01, 0.3) * Math.Min(2.0, root * 0.05);
                // Apply air drag
                Entity.ModifyLinearDamping(Plane.PlanePilot.SprintOrWalk < 0.0 ? 0.5 : 0.1); // TODO: arbitrary constants
                Entity.ModifyAngularDamping(0.995); // TODO: arbitrary constant
                // Ensure we're active if flying!
                Entity.ActivityInformation.Activate();
            }

            public override double SolveIteration()
            {
                return 0; // Do nothing
            }

            double Delta;

            public override void Update(double dt)
            {
                Delta = dt;
            }
        }

        public void NoLongerAPlane() // TODO: Use me!
        {

        }

        public float LiftStrength
        {
            get
            {
                return GetMass() * 20f;
            }
        }

        public float FallStrength
        {
            get
            {
                return GetMass() * 9f;
            }
        }

        public void TurnIntoHelicopter(PlayerEntity pilot) // TODO: Character!
        {
            HeloPilot = pilot;
            Helo = new HelicopterMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Helo);
        }

        public HelicopterMotionConstraint Helo = null;

        public PlayerEntity HeloPilot = null; // TODO: Character!

        public float HeloTiltMod = 1f;

        public class HelicopterMotionConstraint : SingleEntityConstraint
        {
            ModelEntity Helicopter;

            public HelicopterMotionConstraint(ModelEntity heli)
            {
                Helicopter = heli;
                Entity = heli.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Helicopter.HeloPilot == null)
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // Collect the helicopter's relative "up" vector
                BEPUutilities.Vector3 up = BEPUutilities.Quaternion.Transform(BEPUutilities.Vector3.UnitZ, Entity.Orientation);
                // Apply the amount of force necessary to counteract downward force, within a limit.
                // POTENTIAL: Adjust according to orientation?
                double uspeed = Math.Min(Helicopter.LiftStrength, -(Entity.LinearVelocity.Z + Entity.Space.ForceUpdater.Gravity.Z) * Entity.Mass);
                if (uspeed < 0f)
                {
                    uspeed += (uspeed - Helicopter.FallStrength) * Helicopter.HeloPilot.SprintOrWalk;
                }
                else
                {
                    uspeed += (Helicopter.LiftStrength - uspeed) * Helicopter.HeloPilot.SprintOrWalk;
                }
                BEPUutilities.Vector3 upvel = up * uspeed * Delta;
                Entity.ApplyLinearImpulse(ref upvel);
                // Rotate slightly to move in a direction.
                // At the same time, fight against existing rotation.
                BEPUutilities.Vector3 VecUp = new BEPUutilities.Vector3(Helicopter.HeloPilot.XMove * 0.2f * Helicopter.HeloTiltMod, Helicopter.HeloPilot.YMove * -0.2f * Helicopter.HeloTiltMod, 1);
                // TODO: Simplify yawrel calculation.
                float tyaw = (float)(Utilities.MatrixToAngles(Matrix.CreateFromQuaternion(Entity.Orientation)).Z * Utilities.PI180);
                BEPUutilities.Quaternion yawrel = BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.UnitZ, tyaw);
                VecUp = BEPUutilities.Quaternion.Transform(VecUp, yawrel);
                VecUp.Normalize();
                VecUp.Y = -VecUp.Y;
                BEPUutilities.Vector3 axis = BEPUutilities.Vector3.Cross(VecUp, up);
                double len = axis.Length();
                if (len > 0)
                {
                    axis /= len;
                    float angle = (float)Math.Asin(len);
                    if (!float.IsNaN(angle))
                    {
                        double avel = BEPUutilities.Vector3.Dot(Entity.AngularVelocity, axis);
                        BEPUutilities.Vector3 torque = axis * ((-angle) - 0.3f * avel);
                        torque *= Entity.Mass * Delta * 30;
                        Entity.ApplyAngularImpulse(ref torque);
                    }
                }
                // Spin in place
                float rotation = (Helicopter.HeloPilot.ItemRight ? -1f : 0f) + (Helicopter.HeloPilot.ItemLeft ? 1f : 0f);
                if (rotation * rotation > 0f)
                {
                    BEPUutilities.Vector3 rot = new BEPUutilities.Vector3(0, 0, rotation * 15f * Delta * Entity.Mass);
                    Entity.ApplyAngularImpulse(ref rot);
                }
                // Apply air drag
                Entity.ModifyLinearDamping(0.3f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.6f); // TODO: arbitrary constant
                // Ensure we're active if flying!
                Entity.ActivityInformation.Activate();
            }

            public override double SolveIteration()
            {
                return 0; // Do nothing
            }

            double Delta;

            public override void Update(double dt)
            {
                Delta = dt;
            }
        }

        public override void Tick()
        {
            if (Body == null)
            {
                // TODO: Make it safe to -> TheRegion.DespawnEntity(this); ?
                return;
            }
            base.Tick();
        }

        public void PreHandleSpawn()
        {
            model = TheClient.Models.GetModel(mod);
            model.LoadSkin(TheClient.Textures);
            int ignoreme;
            // TODO: Cache this info below!
            if (mode == ModelCollisionMode.PRECISE)
            {
                Shape = TheClient.Models.Handler.MeshToBepu(model.Original, out ignoreme);
            }
            else if (mode == ModelCollisionMode.CONVEXHULL)
            {
                Shape = TheClient.Models.Handler.MeshToBepuConvex(model.Original, out ignoreme, out BEPUutilities.Vector3 center);
                Offset = new Location(-center);
            }
            else if (mode == ModelCollisionMode.AABB)
            {
                List<BEPUutilities.Vector3> vecs = TheClient.Models.Handler.GetCollisionVertices(model.Original);
                Location zero = new Location(vecs[0]);
                AABB abox = new AABB() { Min = zero, Max = zero };
                for (int v = 1; v < vecs.Count; v++)
                {
                    abox.Include(new Location(vecs[v]));
                }
                Location size = abox.Max - abox.Min;
                Location center = abox.Max - size / 2;
                Shape = new BoxShape((float)size.X * (float)scale.X, (float)size.Y * (float)scale.Y, (float)size.Z * (float)scale.Z);
                Offset = -center;
            }
            else
            {
                // TODO: Recentering logic
                List<BEPUutilities.Vector3> vecs = TheClient.Models.Handler.GetCollisionVertices(model.Original);
                // Location zero = new Location(vecs[0].X, vecs[0].Y, vecs[0].Z);
                double distSq = 0;
                for (int v = 1; v < vecs.Count; v++)
                {
                    if (vecs[v].LengthSquared() > distSq)
                    {
                        distSq = vecs[v].LengthSquared();
                    }
                }
                double size = Math.Sqrt(distSq);
                Offset = Location.Zero;
                Shape = new SphereShape((float)size * (float)scale.X);
            }
        }

        public override void SpawnBody()
        {
            PreHandleSpawn();
            base.SpawnBody();
            if (mode == ModelCollisionMode.PRECISE)
            {
                Offset = InternalOffset;
            }
            BEPUutilities.Vector3 offs = Offset.ToBVector();
            transform = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(Offset));
            if (model.ModelBoundsSet)
            {
                ModelMin = model.ModelMin;
                ModelMax = model.ModelMax;
            }
            else
            {
                List<BEPUutilities.Vector3> tvecs = TheClient.Models.Handler.GetVertices(model.Original);
                if (tvecs.Count == 0)
                {
                    ModelMin = new BEPUutilities.Vector3(0, 0, 0);
                    ModelMax = new BEPUutilities.Vector3(0, 0, 0);
                }
                else
                {
                    ModelMin = tvecs[0];
                    ModelMax = tvecs[0];
                    foreach (BEPUutilities.Vector3 vec in tvecs)
                    {
                        BEPUutilities.Vector3 tvec = vec + offs;
                        if (tvec.X < ModelMin.X) { ModelMin.X = tvec.X; }
                        if (tvec.Y < ModelMin.Y) { ModelMin.Y = tvec.Y; }
                        if (tvec.Z < ModelMin.Z) { ModelMin.Z = tvec.Z; }
                        if (tvec.X > ModelMax.X) { ModelMax.X = tvec.X; }
                        if (tvec.Y > ModelMax.Y) { ModelMax.Y = tvec.Y; }
                        if (tvec.Z > ModelMax.Z) { ModelMax.Z = tvec.Z; }
                    }
                }
                model.ModelMin = ModelMin;
                model.ModelMax = ModelMax;
                model.ModelBoundsSet = true;
            }
            if (GenBlockShadows)
            {
                double tx = ModelMax.X - ModelMin.X;
                double ty = ModelMax.Y - ModelMin.Y;
                BoxShape bs = new BoxShape(tx, ty, ModelMax.Z - ModelMin.Z);
                EntityCollidable tempCast = bs.GetCollidableInstance();
                tempCast.LocalPosition = (ModelMax + ModelMin) * 0.5f + Body.Position;
                RigidTransform def = RigidTransform.Identity;
                tempCast.UpdateBoundingBoxForTransform(ref def);
                ShadowCastShape = tempCast.BoundingBox;
                BEPUutilities.Vector3 size = ShadowCastShape.Max - ShadowCastShape.Min;
                ShadowRadiusSquaredXY = (size.X * size.X + size.Y * size.Y) * 0.25;
                if (model.LODHelper == null)
                {
                    model.LODBox = new AABB() { Min = new Location(ModelMin), Max = new Location(ModelMax) };
                    TheClient.LODHelp.PreRender(model, model.LODBox, ClientUtilities.Convert(transform));
                }
            }
        }

        /// <summary>
        /// Map overview render.
        /// </summary>
        public override void RenderForMap()
        {
            if (GenBlockShadows)
            {
                if (!Visible || model.Meshes.Count == 0)
                {
                    return;
                }
                model.DrawLOD(GetPosition() + ClientUtilities.ConvertD(transform.ExtractTranslation()));
            }
        }

        /// <summary>
        /// Used for item rendering.
        /// </summary>
        public void RenderSimpler()
        {
            if (!Visible || model.Meshes.Count == 0)
            {
                return;
            }
            TheClient.SetEnts();
            Matrix4d mat = GetTransformationMatrix();
            TheClient.MainWorldView.SetMatrix(2, mat);
            if (model.Meshes[0].vbo.Tex == null)
            {
                TheClient.Textures.White.Bind();
            }
            model.Draw(); // TODO: Animation?
        }

        public override void RenderWithOffsetLOD(Location pos)
        {
            model.DrawLOD(GetPosition() + pos + ClientUtilities.ConvertD(transform.ExtractTranslation()));
        }

        /// <summary>
        /// General entity render.
        /// </summary>
        public override void Render()
        {
            if (!Visible || model.Meshes.Count == 0)
            {
                return;
            }
            TheClient.SetEnts();
            RigidTransform rt = new RigidTransform(Body.Position, Body.Orientation);
            RigidTransform.Transform(ref ModelMin, ref rt, out BEPUutilities.Vector3 bmin);
            RigidTransform.Transform(ref ModelMax, ref rt, out BEPUutilities.Vector3 bmax);
            if (TheClient.MainWorldView.CFrust != null && !TheClient.MainWorldView.CFrust.ContainsBox(new Location(bmin), new Location(bmax)))
            {
                return;
            }
            double maxr = TheClient.CVars.r_modeldistance.ValueF;
            double distsq = GetPosition().DistanceSquared(TheClient.MainWorldView.RenderRelative);
            if (GenBlockShadows && distsq > maxr * maxr) // TODO: LOD-able option?
            {
                // TODO: Rotation?
                model.DrawLOD(GetPosition() + ClientUtilities.ConvertD(transform.ExtractTranslation()));
                return;
            }
            Matrix4d orient = GetOrientationMatrix();
            Matrix4d mat = (Matrix4d.Scale(ClientUtilities.ConvertD(scale)) * transform * orient * Matrix4d.CreateTranslation(ClientUtilities.ConvertD(GetPosition())));
            TheClient.MainWorldView.SetMatrix(2, mat);
            if (!TheClient.MainWorldView.RenderingShadows)
            {
                TheClient.Rendering.SetMinimumLight(0.0f);
            }
            if (model.Meshes[0].vbo.Tex == null)
            {
                TheClient.Textures.White.Bind();
            }
            if (!TheClient.MainWorldView.RenderingShadows && (TheClient.CVars.r_fast.ValueB || !TheClient.CVars.r_lighting.ValueB)) // TODO: handle for forward lighting?
            {
                OpenTK.Vector4 sadj = TheRegion.GetSunAdjust();
                float skyl = TheRegion.GetSkyLightBase(GetPosition() + new Location(0, 0, ModelMax.Z));
                TheClient.Rendering.SetColor(new OpenTK.Vector4(sadj.X * skyl, sadj.Y * skyl, sadj.Z * skyl, 1.0f));
            }
            model.Draw(); // TODO: Animation(s)?
        }
    }

    public class ModelEntityConstructor : EntityTypeConstructor
    {
        public override Entity Create(Region tregion, byte[] data)
        {
            ModelEntity me = new ModelEntity(tregion.TheClient.Network.Strings.StringForIndex(Utilities.BytesToInt(Utilities.BytesPartial(data, PhysicsEntity.PhysicsNetworkDataLength, 4))), tregion);
            me.ApplyPhysicsNetworkData(data);
            byte moder = data[PhysicsEntity.PhysicsNetworkDataLength + 4];
            me.mode = (ModelCollisionMode)moder;
            me.scale = Location.FromDoubleBytes(data, PhysicsEntity.PhysicsNetworkDataLength + 4 + 1);
            return me;
        }
    }
}
