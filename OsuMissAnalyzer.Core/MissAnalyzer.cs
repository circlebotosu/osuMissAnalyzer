﻿using System;
using osuDodgyMomentsFinder;
using ReplayAPI;
using BMAPI.v1;
using BMAPI;
using BMAPI.v1.HitObjects;
using System.Linq;
using OsuMissAnalyzer.Core.Utils;
using static OsuMissAnalyzer.Core.Utils.MathUtils;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Fonts;

namespace OsuMissAnalyzer.Core
{
    public class MissAnalyzer
    {
        private const int maxTime = 1000;
        private const int arrowLength = 4;
        public bool HitCircleOutlines { get; private set; } = false;
        private ReplayAnalyzer ReplayAnalyzer { get; }
        private Replay Replay { get; }
        private Beatmap Beatmap { get; }
        public int MissCount => ReplayAnalyzer.misses.Count;
        public int CurrentObject { get; set; } = 0;
        private bool drawAllHitObjects;
        private float scale = 0.8f;

        public ColourScheme ColourScheme {get; set;}


        public MissAnalyzer(IReplayLoader replayLoader) : this(replayLoader.Replay, replayLoader.Beatmap, replayLoader.ReplayAnalyzer)
        {
            ColourScheme = replayLoader.ColourScheme ?? ColourScheme.Default;
        }
        public MissAnalyzer(Replay replay, Beatmap beatmap) : this(replay, beatmap, new ReplayAnalyzer(beatmap, replay))
        {}
        public MissAnalyzer(Replay replay, Beatmap beatmap, ReplayAnalyzer analyzer)
        {
            Replay = replay;
            Beatmap = beatmap;
            ReplayAnalyzer = analyzer;
        }

        public void ToggleOutlines()
        {
            HitCircleOutlines = !HitCircleOutlines;
        }

        public void ToggleDrawAllHitObjects()
        {
            if (drawAllHitObjects)
            {
                drawAllHitObjects = false;
                CurrentObject = ReplayAnalyzer.misses.Count(x => x.StartTime < Beatmap.HitObjects[CurrentObject].StartTime);
            }
            else
            {
                drawAllHitObjects = true;
                CurrentObject = Beatmap.HitObjects.IndexOf(ReplayAnalyzer.misses[CurrentObject]);
            }
        }

        public void NextObject()
        {
            if (drawAllHitObjects ? CurrentObject < Beatmap.HitObjects.Count - 1 : CurrentObject < MissCount - 1) CurrentObject++;
        }
        public void PreviousObject()
        {
            if (CurrentObject > 0) CurrentObject--;
        }

        public void ScaleChange(int i)
        {
            scale += 0.1f * i;
            if (scale < 0.1) scale = 0.1f;
        }

        public Image DrawSelectedHitObject(Rectangle area) { return DrawHitObject(CurrentObject, area); }
        /// <summary>
        /// Draws the miss.
        /// </summary>
        /// <returns>A Bitmap containing the drawing</returns>
        /// <param name="num">Index of the miss as it shows up in r.misses.</param>
        public Image DrawHitObject(int num, Rectangle area)
        {
            Image img = new Image<Rgba32>(area.Width, area.Height, ColourScheme.BackgroundColour);

            img.Mutate(g => 
            {
                bool hr = Replay.Mods.HasFlag(Mods.HardRock);
                CircleObject hitObject;
                if (drawAllHitObjects) hitObject = Beatmap.HitObjects[num];
                else hitObject = ReplayAnalyzer.misses[num];
                bool isMiss = !drawAllHitObjects || ReplayAnalyzer.misses.Contains(hitObject);
                float radius = (float)hitObject.Radius;
                
                Func<Color, Pen> circlePen = color => new Pen(color, radius * 2 / scale)
                {
                    EndCapStyle = EndCapStyle.Round,
                    JointStyle = JointStyle.Round,
                };

                Func<Color, Pen> linePen = color => new Pen(color, 1.5f);

                RectangleF bounds = new RectangleF(PointF.Subtract(hitObject.Location.ToPointF(), Scale(area.Size, scale / 2)),
                    Scale(area.Size, scale));

                int replayFramesStart, replayFramesEnd, hitObjectsStart, hitObjectsEnd;

                for (hitObjectsStart = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                    hitObjectsStart >= 0 && bounds.Contains(Beatmap.HitObjects[hitObjectsStart].Location.ToPointF())
                    && hitObject.StartTime - Beatmap.HitObjects[hitObjectsStart].StartTime < maxTime;
                    hitObjectsStart--) ;

                for (hitObjectsEnd = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                    hitObjectsEnd < Beatmap.HitObjects.Count && bounds.Contains(Beatmap.HitObjects[hitObjectsEnd].Location.ToPointF())
                    && Beatmap.HitObjects[hitObjectsEnd].StartTime - hitObject.StartTime < maxTime;
                    hitObjectsEnd++) ;

                for (replayFramesStart = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[hitObjectsStart + 1].StartTime);
                    replayFramesStart > 1 && replayFramesStart < Replay.ReplayFrames.Count && bounds.Contains(Replay.ReplayFrames[replayFramesStart].GetPointF())
                    && hitObject.StartTime - Replay.ReplayFrames[replayFramesStart].Time < maxTime;
                    replayFramesStart--) ;

                for (replayFramesEnd = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[hitObjectsEnd - 1].StartTime);
                    replayFramesEnd < Replay.ReplayFrames.Count - 1 && (replayFramesEnd < 2 || bounds.Contains(Replay.ReplayFrames[replayFramesEnd - 2].GetPointF()))
                    && Replay.ReplayFrames[replayFramesEnd].Time - hitObject.StartTime < maxTime;
                    replayFramesEnd++) ;

                g.Draw(linePen(ColourScheme.PlayfieldColour), Rectangle.Round(ScaleToRect(new RectangleF(pSub(new PointF(0, hr? 384 : 0), bounds, hr), new SizeF(512, 384)), bounds, area)));
                
                for (int q = hitObjectsEnd - 1; q > hitObjectsStart; q--)
                {
                    if (Beatmap.HitObjects[q].Type.HasFlag(HitObjectType.Slider))
                    {
                        SliderObject slider = (SliderObject)Beatmap.HitObjects[q];
                        PointF[] pt = slider.Curves.SelectMany(curve => curve.CurveSnapshots)
                            .Select(c => c.point + slider.StackOffset.ToVector2())
                            .Select(s => ScaleToRect(pSub(s.ToPointF(), bounds, hr), bounds, area)).ToArray();
                        if (pt.Length > 1) g.DrawLines(circlePen(ColourScheme.SliderColour.WithAlpha(80 / 255f)), pt);
                    }

                    var color = ColourScheme.GetCircleColour(Math.Abs(Beatmap.HitObjects[q].StartTime - hitObject.StartTime) / maxTime);
                    var circleRect = ScaleToRect(new RectangleF(PointF.Subtract(
                            pSub(Beatmap.HitObjects[q].Location.ToPointF(), bounds, hr),
                            (Size)new SizeF(radius, radius)), new SizeF(radius * 2, radius * 2)), bounds, area);
                    var circle = new EllipsePolygon(RectangleF.Center(circleRect), circleRect.Size);
                    if (HitCircleOutlines)
                    {
                        g.Draw(linePen(color), circle);
                    }
                    else
                    {
                        g.Fill(color, circle);
                    }
                }
                float distance = 10.0001f;
                float? closestHit = null;
                float closestDistance = 0;
                string verdict = null;
                for (int k = replayFramesStart; k < replayFramesEnd - 2; k++)
                {
                    PointF p1 = pSub(Replay.ReplayFrames[k].GetPointF(), bounds, hr);
                    PointF p2 = pSub(Replay.ReplayFrames[k + 1].GetPointF(), bounds, hr);
                    float hitAcc = Replay.ReplayFrames[k].Time - hitObject.StartTime;
                    var pen = linePen(GetHitColor(Beatmap.OverallDifficulty, hitAcc) ?? ColourScheme.LineColour);
                    g.DrawLines(pen, ScaleToRect(p1, bounds, area), ScaleToRect(p2, bounds, area));
                    if (distance > 10 && Math.Abs(hitObject.StartTime - Replay.ReplayFrames[k + 1].Time) > 50)
                    {
                        Point2 v1 = new Point2(p1.X - p2.X, p1.Y - p2.Y);
                        if (v1.Length > 0)
                        {
                            v1.Normalize();
                            v1 *= (float)(Math.Sqrt(2) * arrowLength / 2);
                            PointF p3 = PointF.Add(p2, new SizeF(v1.X + v1.Y, v1.Y - v1.X));
                            PointF p4 = PointF.Add(p2, new SizeF(v1.X - v1.Y, v1.X + v1.Y));
                            p2 = ScaleToRect(p2, bounds, area);
                            p3 = ScaleToRect(p3, bounds, area);
                            p4 = ScaleToRect(p4, bounds, area);
                            g.DrawLines(pen, p2, p3);
                            g.DrawLines(pen, p2, p4);
                        }
                        distance = 0;
                    }
                    else
                    {
                        distance += new Point2(p1.X - p2.X, p1.Y - p2.Y).Length;
                    }

                    if (Replay.ReplayFrames[k].Time <= hitObject.StartTime && Replay.ReplayFrames[k+1].Time > hitObject.StartTime)
                    {
                        var lerp = (hitObject.StartTime - Replay.ReplayFrames[k].Time) 
                                / (Replay.ReplayFrames[k+1].Time - Replay.ReplayFrames[k].Time);
                        var p3 = new PointF(p1.X + (p2.X - p1.X) * lerp, p1.Y + (p2.Y - p1.Y) * lerp);

                        EllipsePolygon circle = new EllipsePolygon(ScaleToRect(p3, bounds, area), ScaleToRect(new SizeF(2, 2), bounds, area));
                        g.Draw(linePen(ColourScheme.MidpointColour), circle);

                        if (hitObject.ContainsPoint(Replay.ReplayFrames[k].GetPoint2())) verdict = "Misclick";
                        else verdict = "Misaim";
                    }

                    if (ReplayAnalyzer.getKey(k == 0 ? Keys.None : Replay.ReplayFrames[k - 1].Keys, Replay.ReplayFrames[k].Keys) > 0)
                    {
                        EllipsePolygon circle = new EllipsePolygon(ScaleToRect(p1, bounds, area), ScaleToRect(new SizeF(6, 6), bounds, area));
                        g.Draw(pen, circle);
                        if (Math.Abs(hitAcc) < GetHitWindow(Beatmap.OverallDifficulty, 50) 
                            && (!closestHit.HasValue || Math.Abs(closestHit.Value) > Math.Abs(hitAcc)))
                        {
                            closestHit = hitAcc;
                            closestDistance = hitObject.DistanceToPoint(Replay.ReplayFrames[k].GetPoint2());
                        }
                    }
                }
                if (closestDistance <= 0)
                {
                    verdict = "Notelock";
                    closestDistance = 0;
                }

                int textSize = 16;
                int textPadding = 3;
                Font f = new Font(SystemFonts.Get("Segoe UI"), textSize);
                var opts = new TextOptions(f)
                {
                    WrappingLength = area.Width - 2 * textPadding,
                    Origin = new System.Numerics.Vector2(textPadding, textPadding),
                };
                
                var topText = Beatmap.ToString();
                if (drawAllHitObjects) topText += $"\nObject {num + 1} of {Beatmap.HitObjects.Count}";
                else topText += $"\nMiss {num + 1} of {MissCount}";
                g.DrawText(opts, topText, ColourScheme.TextColour);



                float time = hitObject.StartTime;
                if (Replay.Mods.HasFlag(Mods.DoubleTime)) time /= 1.5f;
                else if (Replay.Mods.HasFlag(Mods.HalfTime)) time /= 0.75f;
                TimeSpan ts = TimeSpan.FromMilliseconds(time);
                opts = new TextOptions(f)
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Origin = new System.Numerics.Vector2(textPadding, area.Height - textPadding),
                };
                g.DrawText(opts, $"Time: {ts:mm\\:ss\\.fff}", ColourScheme.TextColour);

                if (closestHit.HasValue && isMiss)
                {
                    opts = new TextOptions(f)
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.End,
                        Origin = new System.Numerics.Vector2(area.Width - textPadding, area.Height - textPadding),
                    };
                    string clickText = $"Closest click: {Math.Abs(closestHit.Value)}ms {(closestHit.Value < 0? "early":"late")}";
                    if (closestDistance > 0) clickText += $", {closestDistance:N} units off";
                    g.DrawText(opts, $"Verdict: {verdict}\n{clickText}", ColourScheme.TextColour);
                }
            });
            return img;
        }

        public IEnumerable<Image> DrawAllMisses(Rectangle area)
        {
            bool savedAllVal = drawAllHitObjects;
            drawAllHitObjects = false;
            var images = Enumerable.Range(0, ReplayAnalyzer.misses.Count).Select(i => DrawHitObject(i, area));
            drawAllHitObjects = savedAllVal;
            return images;
        }

        /// <summary>
        /// Gets the hit window.
        /// </summary>
        /// <returns>The hit window in ms.</returns>
        /// <param name="od">OD of the map.</param>
        /// <param name="hit">Hit value (300, 100, or 50).</param>
        private static float GetHitWindow(float od, int hit)
        {
            switch (hit)
            {
                case 300:
                    return 79.5f - 6 * od;
                case 100:
                    return 139.5f - 8 * od;
                case 50:
                    return 199.5f - 10 * od;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hit), hit, "Hit value is not 300, 100, or 50");
            }
        }

        /// <summary>
        /// Gets the color associated with the hit window.
        /// Blue for 300s, green for 100s, purple for 50s.
        /// </summary>
        /// <returns>The hit color.</returns>
        /// <param name="od">OD of the map.</param>
        /// <param name="ms">Hit timing in ms (can be negative).</param>
        private Color? GetHitColor(float od, float ms)
        {
            if (Math.Abs(ms) < GetHitWindow(od, 300)) return ColourScheme.Colour300;
            if (Math.Abs(ms) < GetHitWindow(od, 100)) return ColourScheme.Colour100; //SpringGreen
            if (Math.Abs(ms) < GetHitWindow(od, 50)) return ColourScheme.Colour50;
            return null;
        }
    }
}