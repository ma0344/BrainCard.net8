using BrainCard.Models.FileFormatV2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainCard.Legacy;

public static class LegacyTimeReconstruction
{
    public static void EnsureMonotonicRelativeMs(IList<Bcf2Stroke> strokes, int defaultStepMs = 16)
    {
        if (strokes == null || strokes.Count == 0) return;

        if (defaultStepMs <= 0) defaultStepMs = 16;

        foreach (var s in strokes)
        {
            if (s?.Points == null || s.Points.Count == 0) continue;
            EnsureMonotonicRelativeMs(s.Points, defaultStepMs);
        }
    }

    public static void EnsureMonotonicRelativeMs(IList<Bcf2Point> points, int defaultStepMs = 16)
    {
        if (points == null || points.Count == 0) return;
        if (defaultStepMs <= 0) defaultStepMs = 16;

        // If all points have t=0, treat as missing.
        var hasNonZero = points.Any(p => p != null && p.T != 0);

        var t = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;

            if (hasNonZero)
            {
                // Respect existing t when monotonic; otherwise fix minimally.
                if (i == 0)
                {
                    t = Math.Max(0, p.T);
                    p.T = t;
                    continue;
                }

                if (p.T > t)
                {
                    t = p.T;
                }
                else
                {
                    t = t + defaultStepMs;
                    p.T = t;
                }

                continue;
            }

            // Missing case: fill evenly.
            p.T = t;
            t += defaultStepMs;
        }

        // Normalize start at 0 for both cases.
        var t0 = points[0]?.T ?? 0;
        if (t0 != 0)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p == null) continue;
                p.T -= t0;
                if (p.T < 0) p.T = 0;
            }
        }

        // Final guard: enforce monotonic again after normalization.
        var last = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;
            if (i == 0)
            {
                p.T = 0;
                last = 0;
                continue;
            }

            if (p.T <= last)
            {
                p.T = last + defaultStepMs;
            }
            last = p.T;
        }
    }
}
