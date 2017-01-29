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
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using BEPUutilities;

namespace Voxalia.ClientGame.GraphicsSystems.ParticleSystem
{
    public class ParticleHelper
    {
        Texture[] Explosion;

        Texture SmokeT;
        
        Texture BlueFlameLick;

        Texture WhiteFlameLick;

        Texture White;

        Texture White_Blur;

        Texture Circle_Light;

        Texture Circle_Heavy;

        Client TheClient;

        public ParticleHelper(Client tclient)
        {
            TheClient = tclient;
            Explosion = new Texture[3];
            for (int i = 0; i < 3; i++)
            {
                Explosion[i] = TheClient.Textures.GetTexture("effects/explosion/0" + (i + 1));
            }
            White = TheClient.Textures.White;
            White_Blur = TheClient.Textures.GetTexture("common/white_blur");
            SmokeT = TheClient.Textures.GetTexture("effects/smoke/smoke1");
            //FlameLick = TheClient.Textures.GetTexture("effects/fire/flamelick01");
            BlueFlameLick = TheClient.Textures.GetTexture("effects/fire/blueflamelick01");
            WhiteFlameLick = TheClient.Textures.GetTexture("effects/fire/whiteflamelick01");
            Circle_Light = TheClient.Textures.GetTexture("effects/particle/circle_light");
            Circle_Heavy = TheClient.Textures.GetTexture("effects/particle/circle_heavy");
        }

        public void Sort()
        {
            Engine.ActiveEffects = Engine.ActiveEffects.OrderBy((o) => -(Engine.TheClient.MainWorldView.CameraPos - o.Start(o)).LengthSquared()).ToList();
        }

        public ParticleEngine Engine;

        public void PaintBomb(Location pos, float size, Location color, int dens = 50)
        {
            for (int i = 0; i < dens; i++)
            {
                Texture tex = Explosion[0];
                Location forward = Utilities.ForwardVector_Deg(Utilities.UtilRandom.NextDouble() * 360, Utilities.UtilRandom.NextDouble() * 360 - 180);
                double ssize = Utilities.UtilRandom.NextDouble() * 1.5 + 0.8;
                float ttl = (float)Utilities.UtilRandom.NextDouble() * 8f + 4f;
                double speed = Utilities.UtilRandom.NextDouble();
                Location loc = new Location(ssize);
                Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + (forward * size * speed) * (1.0 - o.TTL / o.O_TTL), (o) => loc, (o) => 0, ttl, color, color, true, tex);
            }
        }

        public void Explode(Location pos, float size, int dens = 200)
        {
            Location c1 = new Location(1, 0.7, 0);
            Location c2 = new Location(1);
            float spread = size * 0.25f;
            for (int i = 0; i < dens; i++)
            {
                Texture tex = Explosion[Utilities.UtilRandom.Next(Explosion.Length)];
                Location forward = Utilities.ForwardVector_Deg(Utilities.UtilRandom.NextDouble() * 360, Utilities.UtilRandom.NextDouble() * 360 - 180);
                double ssize = Utilities.UtilRandom.NextDouble() * 0.5 + 0.5;
                float ttl = (float)Utilities.UtilRandom.NextDouble() * 5f + 5f;
                double speed = Utilities.UtilRandom.NextDouble();
                Location loc = new Location(ssize);
                Location temp = forward * size * speed;
                double xoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                double yoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                double zoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                Location start = pos + new Location(xoff, yoff, zoff);
                Engine.AddEffect(ParticleEffectType.SQUARE, (o) => start + temp * (1 - o.TTL / o.O_TTL), (o) => loc, (o) => 0, ttl, c1, c2, true, tex);
            }
            for (int i = 0; i < dens / 2; i++)
            {
                double xoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                double yoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                double zoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                Location forward = Utilities.ForwardVector_Deg(Utilities.UtilRandom.NextDouble() * 360, Utilities.UtilRandom.NextDouble() * 360 - 180) * 2;
                SmokeyParticle(pos + new Location(xoff, yoff, zoff), size * 1.5f, new Location(0.5f), SmokeT, forward);
            }
        }

        public void JetpackEffect(Location pos, Location vel)
        {
            vel += TheClient.TheRegion.GravityNormal * 10;
            SmokeyParticle(pos, 1, Location.One, SmokeT, vel);
            SmokeyParticle(pos, 1, new Location(1, 0.2, 0), Explosion[Utilities.UtilRandom.Next(Explosion.Length)], vel);
        }
        
        public void Smoke(Location pos, float spread, Location color, Location vel = default(Location))
        {
            SmokeyParticle(pos, spread, color, SmokeT, vel);
        }

        public void SmokeyParticle(Location pos, float spread, Location color, Texture tex, Location vel = default(Location))
        {
            double xoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            double yoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            double zoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            Location temp = new Location(xoff, yoff, -TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity.Z * 0.33f + zoff);
            // TODO: Gravity directionalism fix.
            ParticleEffect pe = Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + temp * (1 - o.TTL / o.O_TTL)
                + new Location(xoff, yoff, 0) * Math.Sqrt(1 - o.TTL / o.O_TTL) + vel * (1 - o.TTL / o.O_TTL),
                (o) => new Location(1f), (o) => 0, 10, color, color, true, tex);
            pe.AltAlpha = ParticleEffect.FadeInOut;
        }

        public void BigSmoke(Location pos, float spread, Location color) // TODO: Take a vel?
        {
            double xoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            double yoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            double zoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
            Location temp = new Location(xoff, yoff, -TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity.Z * 0.5f + zoff);
            Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + temp * (1 - o.TTL / o.O_TTL),
                (o) => new Location(10f * (1 - o.TTL / o.O_TTL)), (o) => 0, 35, color, color, true, SmokeT);
        }

        public void PathMark(Location pos, Func<Location> target)
        {
            Location height = new Location(0, 0, 5);
            //Location height2 = new Location(0, 0, 4);
            //Location height3 = new Location(0, 0, 3);
            Engine.AddEffect(ParticleEffectType.CYLINDER, (o) => pos, (o) => pos + height * ((o.TTL / o.O_TTL) / 2 + 0.5f),
                (o) => 0.5f, 4f, new Location(0, 1, 1), new Location(0, 1, 1), true, White_Blur, 0.70f);
            Engine.AddEffect(ParticleEffectType.CYLINDER, (o) => pos, (o) => pos + height * ((o.TTL / o.O_TTL) / 2 + 0.5f),
                (o) => 0.6f, 4f, new Location(0, 1, 1), new Location(0, 1, 1), true, White_Blur, 0.55f);
            Engine.AddEffect(ParticleEffectType.CYLINDER, (o) => pos, (o) => pos + height * ((o.TTL / o.O_TTL) / 2 + 0.5f),
                (o) => 0.7f, 4f, new Location(0, 1, 1), new Location(0, 1, 1), true, White_Blur, 0.40f);
            Engine.AddEffect(ParticleEffectType.LINE, (o) => pos, (o) => target(), (o) => 1, 4f, new Location(0, 0.5f, 0.5f), new Location(0, 0.5f, 0.5f), true, White, 1);
        }

        public void Steps(Location pos, Material mat, Location vel, float vlen)
        {
            const double spread = 0.5f;
            int c = Utilities.UtilRandom.Next(5) + 3;
            Vector3 tvec = vel.ToBVector();
            for (int i = 0; i < c; i++)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(Utilities.UtilRandom.NextDouble() * (Math.PI / 2.0)));
                Location nvel = new Location(Quaternion.Transform(tvec, quat));
                nvel.Z += 3;
                double xoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                double yoff = Utilities.UtilRandom.NextDouble() * spread - spread * 0.5;
                Location temp = new Location(xoff, yoff, TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity.Z * 0.15f);
                float ttl = (float)Utilities.UtilRandom.NextDouble() * 3f + 3f;
                Texture tex = TheClient.Textures.GetTexture(TheClient.TBlock.IntTexs[mat.TextureID(MaterialSide.TOP)]);
                Location size = new Location(0.1, 0.1, 0.1);
                Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + temp * (1 - o.TTL / o.O_TTL)
                + new Location(xoff, yoff, 0) * Math.Sqrt(1 - o.TTL / o.O_TTL) + nvel * (1 - o.TTL / o.O_TTL), (o) => size, (o) => 1, ttl, Location.One, Location.One, true, tex, 1);
            }
        }

        public void FireBlue(Location pos)
        {
            Location temp = new Location(0, 0, -TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity.Z * 0.033f);
            Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + temp * (1 - o.TTL / o.O_TTL), (o) => new Location(0.25f), (o) => 0, 3, Location.One, Location.One, true, BlueFlameLick);
        }

        public void Fire(Location pos, float sizemult)
        {
            Location colOne = new Location(3.0, 3.0, 0);
            Location colTwo = new Location(3.0, 2.0, 0.0);
            Location temp = new Location(0, 0, -TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity.Z * 0.09f * sizemult);
            ParticleEffect pe = Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + temp * (1 - o.TTL / o.O_TTL),
                (o) => new Location((o.TTL / o.O_TTL) * 2.0f), (o) => 0, sizemult, colOne, colTwo, true, WhiteFlameLick);
            pe.AltAlpha = ParticleEffect.FadeInOut;
            pe.OnDestroy = (o) =>
            {
                if (Utilities.UtilRandom.Next(5) == 1)
                {
                    Smoke(o.Start(o) - new Location(0, 0, 1), 1, Location.One);
                }
            };
        }

        public void Firework(Location pos, float rad, int lod, Location color, Location color2)
        {
            Location temp = new Location(TheClient.TheRegion.PhysicsWorld.ForceUpdater.Gravity * 0.1f);
            Location size = new Location(0.3);
            for (int i = 0; i < lod; i++)
            {
                Location col = color;
                if (col.X < 0)
                {
                    col.X = Utilities.UtilRandom.NextDouble();
                }
                if (col.Y < 0)
                {
                    col.Y = Math.Max(Utilities.UtilRandom.NextDouble() - col.X * 0.25, 0.0);
                }
                if (col.Z < 0)
                {
                    col.Z = Math.Max(Utilities.UtilRandom.NextDouble() - col.X * 0.25 - col.Y * 0.25, 0.0);
                }
                Location col2 = color2;
                if (col2.X < 0)
                {
                    col2.X = Utilities.UtilRandom.NextDouble();
                }
                if (col2.Y < 0)
                {
                    col2.Y = Math.Max(Utilities.UtilRandom.NextDouble() - col2.X * 0.25, 0.0);
                }
                if (col2.Z < 0)
                {
                    col2.Z = Math.Max(Utilities.UtilRandom.NextDouble() - col2.X * 0.25 - col2.Y * 0.25, 0.0);
                }
                Location speed = new Location(Utilities.UtilRandom.NextDouble() * (rad * 2) - rad, Utilities.UtilRandom.NextDouble() * (rad * 2) - rad, Utilities.UtilRandom.NextDouble() * (rad * 2) - rad) * 3.0;
                ParticleEffect pe = Engine.AddEffect(ParticleEffectType.SQUARE, (o) => pos + (temp + speed) * (1 - o.TTL / o.O_TTL), (o) => size, (o) => 0, 8, col, col2, true, Utilities.UtilRandom.Next(1000) < 500 ? Circle_Light : Circle_Heavy);
                pe.WindMod = 0.1f;
            }
        }
    }
}
