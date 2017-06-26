using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticGameCore.Collision;
using FreneticGameCore;

namespace Voxalia.ServerGame.WorldSystem.SimpleGenerator.Helpers
{
    /// <summary>
    /// Helper to generate fair quality mountain heightmaps.
    /// </summary>
    public class SimpleMountainGenerator
    {
        /// <summary>
        /// Generates a mountain for the given 2D coordinates.
        /// Returns a basic heightmap.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <returns>Height map.</returns>
        public static double[,] GenerateMountain(int x, int y)
        {
            SimpleMountainGenerator smg = new SimpleMountainGenerator();
            smg.RunMountainGen((ulong)(x * 39 + y), true);
            return smg.HeightMap;
        }

        public static SimpleMountainGenerator PreGenerateMountain(int x, int y)
        {
            SimpleMountainGenerator smg = new SimpleMountainGenerator()
            {
                PreRunImage = false
            };
            smg.RunMountainGen((ulong)(x * 39 + y), false);
            smg.ToHandle = null;
            return smg;
        }

        public double GetHeightAt(int xRel, int yRel, double upScale)
        {
            double xPoint = (xRel) / upScale + AreaSize / 2;
            double yPoint = (yRel) / upScale + AreaSize / 2;
            int xLow = (int)Math.Floor(xPoint);
            int yLow = (int)Math.Floor(yPoint);
            double pLow = HeightPrecise(xLow, yLow);
            double pXP = HeightPrecise(xLow + 1, yLow);
            double pYP = HeightPrecise(xLow, yLow + 1);
            double pHigh = HeightPrecise(xLow + 1, yLow + 1);
            double xShift = xPoint - xLow;
            double yShift = yPoint - yLow;
            return pLow * (1.0 - xShift) * (1.0 - yShift) + pXP * xShift * (1.0 - yShift) + pYP * yShift * (1.0 - xShift) + pHigh * xShift * yShift;
        }

        double HeightPrecise(int x, int y)
        {
            if (x < 0 || y < 0 || x >= AreaSize || y >= AreaSize)
            {
                return 0;
            }
            if (HeightMap != null && HeightMap[x, y] > 0)
            {
                return HeightMap[x, y];
            }
            return FillPoint(Height, new Vector2i(x, y));
        }

        int AreaSize = 768;
        int Height = 255;
        int DetailScaler = 4;
        int NodeHelperSize = 64;
        int NodePasses = 5;
        double blurAmt = 0.7;
        int DetailHelper = 4;
        double AngleRandomer = 2.5;
        int Movement = 5;
        double Roughness = 1.5;
        bool PreRunImage = true;
        public void RunMountainGen(ulong seed, bool genmaps)
        {
            random = new MTRandom(seed);
            ADDER = AreaSize * 0.25 / 2 / DetailScaler;
            RandomMountainHeightMap(AreaSize, Height, Movement, Roughness, genmaps);
        }

        MTRandom random;

        public double[,] HeightMap;

        Dictionary<Vector2i, NodePoint> Nodes = new Dictionary<Vector2i, NodePoint>();
        Vector2i[] NodeArray;
        NodePoint[] NodeValueHeights;
        NodePoint[,][] NodeHelper;
        Vector2i Center;
        double MaxDistance;
        List<KeyValuePair<Vector2i, double>> ToHandle = new List<KeyValuePair<Vector2i, double>>();
        double[,] NDists;

        void RandomMountainHeightMap(int Size, int Height, int Movement, double Roughness, bool genmaps)
        {
            if (genmaps)
            {
                HeightMap = new double[Size, Size];
                NDists = new double[Size, Size];
            }

            int CenterPosition = Size / 2;
            Center = new Vector2i(CenterPosition, CenterPosition);

            MaxDistance = Math.Sqrt(Size * Size * 2) / 2;

            for (int i = 0; i < Movement; i++)
            {
                MakeBranch(Center, Size, Movement, Roughness, 0, Height);
            }

            NodeArray = Nodes.Keys.ToArray();
            NodeValueHeights = new NodePoint[NodeArray.Length];
            for (int i = 0; i < NodeArray.Length; i++)
            {
                NodeValueHeights[i] = Nodes[NodeArray[i]];
            }

            List<NodePoint>[,] NPs = new List<NodePoint>[NodeHelperSize, NodeHelperSize];

            for (int i = 0; i < NodeHelperSize; i++)
            {
                for (int j = 0; j < NodeHelperSize; j++)
                {
                    NPs[i, j] = new List<NodePoint>();
                }
            }

            for (int i = 0; i < NodeValueHeights.Length; i++)
            {
                int x = NodeValueHeights[i].Point.X * DetailScaler / (AreaSize / NodeHelperSize);
                int y = NodeValueHeights[i].Point.Y * DetailScaler / (AreaSize / NodeHelperSize);
                if (x >= 0 && y >= 0 && x < NodeHelperSize && y < NodeHelperSize)
                {
                    NPs[x, y].Add(NodeValueHeights[i]);
                }
            }

            NodeHelper = new NodePoint[NodeHelperSize, NodeHelperSize][];

            for (int i = 0; i < NodeHelperSize; i++)
            {
                for (int j = 0; j < NodeHelperSize; j++)
                {
                    NodeHelper[i, j] = NPs[i, j].ToArray();
                }
            }
            
            if (PreRunImage)
            {
                int ct = Environment.ProcessorCount * 4;
                Task[] ts = new Task[ct];
                for (int i = 0; i < ct; i++)
                {
                    int starti = i;
                    ts[i] = Task.Factory.StartNew(() =>
                    {
                        for (int X = Size / ct * starti; X < Size / ct * (starti + 1); X++)
                        {
                            for (int Y = 0; Y < Size; Y++)
                            {
                                FillPoint(Height, new Vector2i(X, Y));
                            }
                        }
                    });
                }
                for (int i = 0; i < ct; i++)
                {
                    ts[i].Wait();
                }
                HeightMap[CenterPosition, CenterPosition] = HeightMap[CenterPosition + 1, CenterPosition - 1];
                for (int X = 0; X < CenterPosition; X++)
                {
                    for (int Y = 0; Y < X * 2; Y++)
                    {
                        BlurPoint(Height, new Vector2i(CenterPosition + X + 0, CenterPosition + X - Y));
                        BlurPoint(Height, new Vector2i(CenterPosition + X - Y, CenterPosition - X + 0));
                        BlurPoint(Height, new Vector2i(CenterPosition - X + 0, CenterPosition - X + Y));
                        BlurPoint(Height, new Vector2i(CenterPosition - X + Y, CenterPosition + X + 0));
                    }
                }
                for (int i = 0; i < ToHandle.Count; i++)
                {
                    Vector2i p = ToHandle[i].Key;
                    HeightMap[p.X, p.Y] = Math.Min(Height, Math.Max(1, HeightMap[p.X, p.Y] * 0.99 + ToHandle[i].Value * 0.01));
                }
            }
        }

        void BlurPoint(double Height, Vector2i point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= AreaSize || point.Y >= AreaSize)
            {
                return;
            }
            int movex = (point.X - Center.X);
            int movey = (point.Y - Center.Y);
            double len = Math.Sqrt(movex * movex + movey * movey);
            double mx;
            double my;
            if (len > 5)
            {
                mx = movex / len;
                my = movey / len;
            }
            else
            {
                return;
            }
            double hmc = HeightMap[point.X, point.Y];
            double surround = hmc;
            double dist = Math.Min(25, Math.Max(1, NDists[point.X, point.Y]));
            for (double modder = 5.0 + (25.0 - dist); modder > 2.0; modder -= 2.5)
            {
                Vector2i pt2 = new Vector2i((int)(point.X - mx * modder), (int)(point.Y - my * modder));
                surround = surround * 0.5 + 0.5 * HeightMap[pt2.X, pt2.Y];
            }
            HeightMap[point.X, point.Y] = hmc * (1.0 - blurAmt) + surround * blurAmt;
        }
        
        bool TryPoint(ref Vector2i Lowest, ref double LowestScore, int tempX, int tempY, Vector2i point, Vector2i notMe)
        {
            if (tempX < 0 || tempY < 0 || tempX >= NodeHelperSize || tempY >= NodeHelperSize)
            {
                return false;
            }
            bool success = false;
            NodePoint[] nps = NodeHelper[tempX, tempY];
            for (int ix = 0; ix < nps.Length; ix++)
            {
                Vector2i node = nps[ix].Point;
                if (node.X == int.MaxValue)
                {
                    continue;
                }
                double x = point.X - node.X * DetailScaler;
                double y = point.Y - node.Y * DetailScaler;
                double CurrentScore = (x * x + y * y);
                if (CurrentScore < LowestScore && node != notMe)
                {
                    if (notMe.X != int.MaxValue)
                    {
                        double tx = notMe.X - node.X;
                        double ty = notMe.Y - node.Y;
                        if (tx * tx + ty * ty > DetailScaler * DetailScaler * DetailHelper)
                        {
                            continue;
                        }
                    }
                    LowestScore = CurrentScore;
                    Lowest = node;
                    success = true;
                }
            }
            return success;
        }
        
        bool FindLowest(Vector2i point, ref Vector2i Lowest, ref double LowestScore, Vector2i notMe)
        {
            Vector2i tp = notMe.X == int.MaxValue ? point : new Vector2i(notMe.X * DetailScaler, notMe.Y * DetailScaler);
            int baseX = tp.X / (AreaSize / NodeHelperSize);
            int baseY = tp.Y / (AreaSize / NodeHelperSize);

            bool had_success = false;
            int passes = 0;

            for (int X = 0; X < NodeHelperSize; X++)
            {
                for (int Y = 0; Y < X * 2; Y++)
                {
                    bool ba = TryPoint(ref Lowest, ref LowestScore, baseX + X + 0, baseY + X - Y, point, notMe);
                    bool bb = TryPoint(ref Lowest, ref LowestScore, baseX + X - Y, baseY - X + 0, point, notMe);
                    bool bc = TryPoint(ref Lowest, ref LowestScore, baseX - X + 0, baseY - X + Y, point, notMe);
                    bool bd = TryPoint(ref Lowest, ref LowestScore, baseX - X + Y, baseY + X + 0, point, notMe);
                    if (ba || bb || bc || bd)
                    {
                        had_success = true;
                    }
                }
                if (had_success)
                {
                    if (passes++ == NodePasses)
                    {
                        return true;
                    }
                }
            }
            return passes > 0;
        }

        double FillPoint(int Height, Vector2i point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= AreaSize || point.Y >= AreaSize)
            {
                return 0;
            }

            double multo = 1;
            double LowestScore = MaxDistance * MaxDistance;
            Vector2i Lowest = Center;
            
            bool had_success = FindLowest(point, ref Lowest, ref LowestScore, new Vector2i(int.MaxValue, int.MaxValue));

            if (!had_success)
            {
                return 0;
            }
            
            double SecondLowestScore = MaxDistance * MaxDistance;
            Vector2i SecondLowest = Lowest;

            bool had_success2 = FindLowest(point, ref SecondLowest, ref SecondLowestScore, Lowest);

            if (!had_success2)
            {
                return 0;
            }

            double fScore, nL;

            if (SecondLowestScore - LowestScore > 20)
            {
                double xert = Lowest.X * DetailScaler - point.X;
                double yert = Lowest.Y * DetailScaler - point.Y;
                fScore = Math.Sqrt(xert * xert + yert * yert);
                nL = Nodes[Lowest].Height;
            }
            else
            {
                double line_A = (Lowest.Y - SecondLowest.Y) * DetailScaler;
                double line_B = (SecondLowest.X - Lowest.X) * DetailScaler;
                double line_C = (SecondLowest.Y * Lowest.X * DetailScaler * DetailScaler - SecondLowest.X * Lowest.Y * DetailScaler * DetailScaler);
                fScore = (line_A * point.X + line_B * point.Y + line_C) / Math.Sqrt(line_A * line_A + line_B * line_B);
                double nA = Nodes[Lowest].Height;
                double nB = Nodes[SecondLowest].Height;
                double perpX = (SecondLowest.X - Lowest.X) * DetailScaler;
                double perpY = (SecondLowest.Y - Lowest.Y) * DetailScaler;
                double len = 1.0 / Math.Sqrt(perpX * perpX + perpY * perpY);
                perpX *= len;
                perpY *= len;

                fScore = Math.Abs(fScore);

                double pointerX = point.X + perpY * fScore;
                double pointerY = point.Y + perpX * fScore;
                double resX = (pointerX - Lowest.X);
                double resY = (Lowest.Y - pointerY);
                double nlen = Math.Sqrt(resX * resX + resY * resY);

                double lenner = nlen * len;
                
                if (lenner < 0.05 || lenner > 0.95)
                {
                    lenner = 0.5;
                }
                nL = nB * lenner + nA * (1.0 - lenner);
            }
            
            double resx = point.X - Center.X;
            double resy = point.Y - Center.Y;
            double resser = Height - Math.Sqrt(resx * resx + resy * resy);

            double Result = Math.Min(Height, Math.Max(1, resser * 0.15 + (nL - fScore * Math.Max(1.0, (nL) / (Height / 2)) * 1.0) * 0.85 * multo));

            if (HeightMap != null)
            {
                HeightMap[point.X, point.Y] = Result;
                NDists[point.X, point.Y] = fScore;
            }

            return Result;
        }

        List<double> UsedAngles = new List<double>();
        int bounces = 0;

        double SHRINKER = 1.5;
        double ADDER;

        void MakeBranch(Vector2i Origin, int Size, int Movement, double Roughness, int Depth, double heightstart)
        {
            Increment increment = GetIncrement(Center.X - Origin.X, Center.Y - Origin.Y);
            Increment inc1 = increment;
            double OriginDistance = Math.Max(Math.Sqrt(increment.X * increment.X + increment.Y * increment.Y), 1);
            double DistanceModifier = Size / Movement * (Depth + 1);
            increment = GetIncrement((increment.X / OriginDistance + (random.NextDouble() - 0.5) * AngleRandomer) * DistanceModifier,
                (increment.Y / OriginDistance + (random.NextDouble() - 0.5) * AngleRandomer) * DistanceModifier);
            if (Depth == 0)
            {
                double ang = Math.Atan2(increment.Y, increment.X);
                for (int i = 0; i < UsedAngles.Count; i++)
                {
                    if (Math.Abs(UsedAngles[i] - ang) < (Math.PI * 1.5 / Movement))
                    {
                        if (bounces++ < 15)
                        {
                            MakeBranch(Origin, Size, Movement, Roughness, Depth, heightstart);
                            return;
                        }
                    }
                }
                UsedAngles.Add(ang);
            }

            Vector2i Destination = FixBounds(new Vector2i((int)Math.Round(Center.X + increment.X),
                (int)Math.Round(Center.Y + increment.Y)), Size);

            List<Vector2i> Points = MakeLine(Origin, Destination, Size, Roughness);
            int PointSize = Points.Count;

            if (PointSize == 0)
            {
                return;
            }

            Destination = Points[PointSize - 1];
            double distX = Center.X - Destination.X;
            double distY = Center.Y - Destination.Y;
            double DestinationDistance = Math.Max(Math.Sqrt(distX * distX + distY * distY), 1);

            double HeightStart = heightstart;
            double HeightDecay = (HeightStart - Height * ((MaxDistance - DestinationDistance) / MaxDistance)) / PointSize;

            int Branching = 0;

            Vector2i Previous = new Vector2i((int)(Origin.X / DetailScaler / SHRINKER + ADDER), (int)(Origin.Y / DetailScaler / SHRINKER / SHRINKER + ADDER));
            Vector2i prev2 = Previous;
            Vector2i Next;

            for (int i = 0; i < PointSize; i++)
            {
                double HeightValue = HeightStart - HeightDecay * (i + 1);

                Vector2i point = Points[i];

                Vector2i ScaledPoint = new Vector2i((int)(point.X / DetailScaler / SHRINKER + ADDER), (int)(point.Y / DetailScaler / SHRINKER + ADDER));

                if (prev2.X == ScaledPoint.X && prev2.Y == ScaledPoint.Y)
                {
                    prev2 = new Vector2i(ScaledPoint.X - Math.Sign(inc1.X), ScaledPoint.Y - Math.Sign(inc1.Y));
                    if (prev2.X == ScaledPoint.X && prev2.Y == ScaledPoint.Y)
                    {
                        prev2 = new Vector2i(ScaledPoint.X - 1, ScaledPoint.Y - 1);
                    }
                }


                Next = ScaledPoint;
                int n = 1;
                while (i + n < PointSize)
                {
                    Next = new Vector2i((int)(Points[i + n].X / DetailScaler / SHRINKER + ADDER), (int)(Points[i + n].Y / DetailScaler / SHRINKER + ADDER));
                    if (Next.X != ScaledPoint.X || Next.Y != ScaledPoint.Y)
                    {
                        break;
                    }
                    n++;
                }
                if (i + n >= PointSize)
                {
                    Next = new Vector2i((int)(Destination.X / DetailScaler / SHRINKER + ADDER), (int)(Destination.Y / DetailScaler / SHRINKER + ADDER));
                    if (Next.X == ScaledPoint.X && Next.Y == ScaledPoint.Y)
                    {
                        Next = new Vector2i(ScaledPoint.X + Math.Sign(increment.X), ScaledPoint.Y + Math.Sign(increment.Y));
                    }
                }
                NodePoint ScaledNodePoint = GetNodePoint(prev2, ScaledPoint, Next, HeightValue);

                if (!Nodes.ContainsKey(ScaledPoint))
                {
                    Nodes.Add(ScaledPoint, ScaledNodePoint);
                    prev2 = Previous;
                    Previous = ScaledPoint;
                }
                ToHandle.Add(new KeyValuePair<Vector2i, double>(new Vector2i((int)(point.X / SHRINKER + ADDER * DetailScaler), (int)(point.Y / SHRINKER + ADDER * DetailScaler)), HeightValue));

                if (random.NextDouble() < 0.2 && Depth < Movement && Branching < 5)
                {
                    Branching++;
                    MakeBranch(Destination, Size, Movement, Roughness, Depth + 1, HeightStart - HeightDecay * (PointSize + 1));
                }

            }



        }

        struct Increment
        {
            public double X;
            public double Y;
        }

        static Increment GetIncrement(double X, double Y)
        {
            return new Increment() { X = X, Y = Y };
        }

        struct NodePoint
        {
            public Vector2i Previous;
            public Vector2i Next;
            public Vector2i Point;
            public double Height;
        }

        static NodePoint GetNodePoint(Vector2i Previous, Vector2i Point, Vector2i Next, double Height)
        {
            return new NodePoint() { Previous = Previous, Point = Point, Next = Next, Height = Height };
        }

        List<Vector2i> MakeLine(Vector2i Origin, Vector2i Destination, int Size, double Roughness)
        {
            int Center = Size / 2;

            double distX = Origin.X - Destination.X;
            double distY = Origin.Y - Destination.Y;
            int Distance = (int)Math.Round(Math.Sqrt(distX * distX + distY * distY));

            int NodeCount = Math.Max(Distance, 1);

            List<Vector2i> Line = new List<Vector2i>(NodeCount);

            Increment increment = GetIncrement((Origin.X - Destination.X) / (double)NodeCount, (Origin.Y - Destination.Y) / (double)NodeCount);
            Increment Modified = increment;

            Vector2i CurrentPoint = new Vector2i(Origin.X, Origin.Y);

            Line.Add(CurrentPoint);
            for (int i = 0; i < NodeCount; i++)
            {

                if (random.NextDouble() > 0.95)
                {
                    Modified = GetIncrement((increment.X + (random.NextDouble() - 0.5) * Roughness), increment.Y + (random.NextDouble() - 0.5) * Roughness);
                }

                CurrentPoint = new Vector2i((int)Math.Round(CurrentPoint.X + Modified.X + ((random.NextDouble() - 0.5) * Roughness)),
                (int)Math.Round(CurrentPoint.Y + Modified.Y + ((random.NextDouble() - 0.5) * Roughness)));

                if (CurrentPoint.X < 0 || CurrentPoint.X >= Size || CurrentPoint.Y < 0 || CurrentPoint.Y >= Size)
                {
                    return Line;
                }

                CurrentPoint = FixBounds(CurrentPoint, Size);



                Line.Add(CurrentPoint);
            }


            Line.Add(CurrentPoint);

            return Line;
        }

        Vector2i FixBounds(Vector2i point, int Size)
        {
            Size--;
            if (point.X < 0)
            {
                point.X = 0;
            }
            else if (point.X > Size)
            {
                point.X = Size;
            }
            if (point.Y < 0)
            {
                point.Y = 0;
            }
            else if (point.Y > Size)
            {
                point.Y = Size;
            }
            return point;
        }
    }
}
