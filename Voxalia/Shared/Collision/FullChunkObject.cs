//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries.Events;
using BEPUphysics.OtherSpaceStages;
using BEPUphysics.NarrowPhaseSystems;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using BEPUutilities.DataStructures;
using System;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.Entities;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using BEPUphysics.PositionUpdating;
using BEPUphysics.Settings;

namespace Voxalia.Shared.Collision
{
    public class FullChunkObject : StaticCollidable
    {
        public static void RegisterMe()
        {
            NarrowPhasePairFactory<ConvexFCOPairHandler> fact = new NarrowPhasePairFactory<ConvexFCOPairHandler>();
            NarrowPhasePairFactory<MCCFCOPairHandler> fact2 = new NarrowPhasePairFactory<MCCFCOPairHandler>();
            NarrowPhasePairFactory<ConvexMCCPairHandler> fact3 = new NarrowPhasePairFactory<ConvexMCCPairHandler>();
            NarrowPhasePairFactory<MCCMCCPairHandler> fact4 = new NarrowPhasePairFactory<MCCMCCPairHandler>();
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(FullChunkObject)), fact);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(MobileChunkCollidable), typeof(FullChunkObject)), fact2);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(MobileChunkCollidable)), fact3);
            NarrowPhaseHelper.CollisionManagers.Add(new TypePair(typeof(MobileChunkCollidable), typeof(MobileChunkCollidable)), fact4);
        }

        public FullChunkObject(Vector3 pos, FullChunkShape shape)
        {
            ChunkShape = shape;
            base.Shape = ChunkShape;
            Position = pos;
            boundingBox = new BoundingBox(Position, Position + new Vector3(30, 30, 30));
            Events = new ContactEventManager<FullChunkObject>(this);
            Material.Bounciness = 0.75f;
        }

        public FullChunkObject(Vector3 pos, BlockInternal[] blocks)
        {
            ChunkShape = new FullChunkShape(blocks);
            base.Shape = ChunkShape;
            Position = pos;
            boundingBox = new BoundingBox(Position, Position + new Vector3(30, 30, 30));
            Events = new ContactEventManager<FullChunkObject>(this);
            Material.Bounciness = 0.75f;
        }

        public FullChunkShape ChunkShape;

        public Vector3 Position;

        public ContactEventManager<FullChunkObject> Events;

        protected override IContactEventTriggerer EventTriggerer
        {
            get { return Events; }
        }

        protected override IDeferredEventCreator EventCreator
        {
            get { return Events; }
        }

        public override void UpdateBoundingBox()
        {
            boundingBox = new BoundingBox(Position, Position + new Vector3(30, 30, 30));
        }

        public bool ConvexCast(ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweepnorm, double slen, MaterialSolidity solidness, out RayHit hit)
        {
            RigidTransform rt = new RigidTransform(startingTransform.Position - Position, startingTransform.Orientation);
            RayHit rHit;
            bool h = ChunkShape.ConvexCast(castShape, ref rt, ref sweepnorm, slen, solidness, out rHit);
            rHit.Location = rHit.Location + Position;
            hit = rHit;
            return h;
        }

        public override bool ConvexCast(ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweep, Func<BroadPhaseEntry, bool> filter, out RayHit hit)
        {
            RigidTransform rt = new RigidTransform(startingTransform.Position - Position, startingTransform.Orientation);
            RayHit rHit;
            double slen = sweep.Length();
            Vector3 sweepnorm = sweep / slen;
            bool h = ChunkShape.ConvexCast(castShape, ref rt, ref sweepnorm, slen, MaterialSolidity.FULLSOLID, out rHit);
            rHit.Location = rHit.Location + Position;
            hit = rHit;
            return h;
        }

        public override bool ConvexCast(ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweep, out RayHit hit)
        {
            return ConvexCast(castShape, ref startingTransform, ref sweep, null, out hit);
        }

        public bool RayCast(Ray ray, double maximumLength, Func<BroadPhaseEntry, bool> filter, MaterialSolidity solidness, out RayHit rayHit)
        {
            Ray r2 = new Ray(ray.Position - Position, ray.Direction);
            RayHit rHit;
            bool h = ChunkShape.RayCast(ref r2, maximumLength, solidness, out rHit);
            rHit.Location = rHit.Location + Position;
            rayHit = rHit;
            return h;
        }

        public override bool RayCast(Ray ray, double maximumLength, Func<BroadPhaseEntry, bool> filter, out RayHit rayHit)
        {
            return RayCast(ray, maximumLength, filter, MaterialSolidity.FULLSOLID, out rayHit);
        }

        public override bool RayCast(Ray ray, double maximumLength, out RayHit rayHit)
        {
            return RayCast(ray, maximumLength, null, out rayHit);
        }
    }
}
