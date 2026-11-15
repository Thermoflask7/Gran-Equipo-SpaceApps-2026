using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Sky condition for one calendar day.</summary>
public enum WeatherCondition { Sunny, Cloudy, Rain, Storm }

/// <summary>Weather of one day at one launch site (synthetic game data, not a real forecast).</summary>
[Serializable]
public struct DayWeather
{
    public int dayIndex;            // 0..Days-1
    public DateTime date;
    public WeatherCondition condition;
    public float windKnots;
    public int precipitationChance; // percent
    public float temperatureC;
    public float risk;              // 0 = perfect, 1 = no-go

    public string Weekday => date.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture);
    public bool IsGoodLaunchDay => risk < 0.35f;
}

/// <summary>
/// Semi-random weather generator for the launch sites (task 6, calendar).
/// Each site has a climate profile; days follow a Markov chain (weather tends to persist),
/// wind and rain depend on the condition plus noise. One seed per game run: the forecast
/// stays the same while you play and changes on the next run (or with Reseed).
/// Kennedy and Cape Canaveral are ~10 km apart, so they share the sky and only differ a bit in wind.
/// </summary>
public static class LaunchWeatherModel
{
    public const int Days = 7;

    /// <summary>Climate of a site. Transition rows: Sunny, Cloudy, Rain, Storm.</summary>
    public class ClimateProfile
    {
        public string region;
        public float[] start;          // chance of each condition on day 1
        public float[][] transition;   // [from][to]
        public float windMean;         // knots on a sunny day
        public float windSpread;       // standard deviation, knots
        public float temperatureC;     // typical day temperature
    }

    static readonly ClimateProfile Florida = new ClimateProfile
    {
        region = "Florida",
        start = new[] { 0.45f, 0.30f, 0.18f, 0.07f },
        transition = new[]
        {
            new[] { 0.60f, 0.25f, 0.12f, 0.03f },
            new[] { 0.30f, 0.35f, 0.25f, 0.10f },
            new[] { 0.20f, 0.30f, 0.35f, 0.15f },
            new[] { 0.25f, 0.30f, 0.30f, 0.15f },
        },
        windMean = 10f, windSpread = 4f, temperatureC = 26f,
    };

    // Dry Californian coast: few storms, more low clouds/fog and stronger wind.
    static readonly ClimateProfile CentralCalifornia = new ClimateProfile
    {
        region = "CentralCalifornia",
        start = new[] { 0.50f, 0.35f, 0.12f, 0.03f },
        transition = new[]
        {
            new[] { 0.60f, 0.30f, 0.08f, 0.02f },
            new[] { 0.35f, 0.50f, 0.12f, 0.03f },
            new[] { 0.30f, 0.35f, 0.30f, 0.05f },
            new[] { 0.40f, 0.30f, 0.25f, 0.05f },
        },
        windMean = 14f, windSpread = 5f, temperatureC = 17f,
    };

    static readonly float[] WindBoost = { 0f, 2f, 5f, 12f };
    static readonly float[] BaseRisk = { 0.05f, 0.15f, 0.45f, 0.80f };
    static readonly Vector2Int[] PrecipRange = { new Vector2Int(0, 10), new Vector2Int(10, 30), new Vector2Int(50, 80), new Vector2Int(70, 95) };

    static int seed;
    static bool seeded;
    static DateTime firstDay;
    static readonly Dictionary<string, DayWeather[]> cache = new Dictionary<string, DayWeather[]>();

    /// <summary>Raised after Reseed, so the calendar can redraw.</summary>
    public static event Action ForecastChanged;

    public static int Seed { get { EnsureSeed(); return seed; } }

    /// <summary>New random forecast (or a fixed one for tests when a seed is given).</summary>
    public static void Reseed(int? fixedSeed = null)
    {
        seed = fixedSeed ?? Environment.TickCount;
        seeded = true;
        firstDay = DateTime.Today.AddDays(1);
        cache.Clear();
        ForecastChanged?.Invoke();
    }

    static void EnsureSeed()
    {
        if (!seeded) Reseed();
    }

    /// <summary>Climate profile for a site id (KSC, CCSFS, VSFB).</summary>
    public static ClimateProfile ProfileFor(string siteId)
    {
        return siteId == "VSFB" ? CentralCalifornia : Florida;
    }

    /// <summary>The 7-day forecast for a site, starting tomorrow. Cached per run.</summary>
    public static DayWeather[] GetForecast(string siteId)
    {
        EnsureSeed();
        siteId = siteId ?? "";
        if (!cache.TryGetValue(siteId, out var days))
        {
            days = Generate(siteId);
            cache[siteId] = days;
        }
        return days;
    }

    /// <summary>Weather of one day (index 0..Days-1) at a site.</summary>
    public static DayWeather GetDay(string siteId, int dayIndex)
    {
        var days = GetForecast(siteId);
        return days[Mathf.Clamp(dayIndex, 0, days.Length - 1)];
    }

    static DayWeather[] Generate(string siteId)
    {
        ClimateProfile p = ProfileFor(siteId);
        // Sky: same seed for the whole region. Wind/temperature details: per site.
        var sky = new System.Random(seed ^ StableHash(p.region));
        var local = new System.Random(seed ^ StableHash(siteId));

        var days = new DayWeather[Days];
        int state = Pick(sky, p.start);
        for (int i = 0; i < Days; i++)
        {
            if (i > 0) state = Pick(sky, p.transition[state]);
            days[i] = MakeDay(i, (WeatherCondition)state, p, sky, local);
        }

        // Keep the week playable: at least one good launch day.
        bool anyGood = false;
        foreach (var d in days) anyGood |= d.IsGoodLaunchDay;
        if (!anyGood)
        {
            int best = 0;
            for (int i = 1; i < Days; i++) if (days[i].risk < days[best].risk) best = i;
            days[best] = MakeDay(best, WeatherCondition.Sunny, p, sky, local, calmWind: true);
        }
        return days;
    }

    static DayWeather MakeDay(int index, WeatherCondition c, ClimateProfile p, System.Random sky, System.Random local, bool calmWind = false)
    {
        int ci = (int)c;
        float wind = p.windMean + WindBoost[ci] + Gaussian(sky) * p.windSpread * 0.7f + Gaussian(local) * 1.5f;
        if (calmWind) wind = Mathf.Min(wind, p.windMean);
        wind = Mathf.Round(Mathf.Clamp(wind, 2f, 45f));

        Vector2Int pr = PrecipRange[ci];
        int precip = sky.Next(pr.x, pr.y + 1);
        float temp = Mathf.Round(p.temperatureC + Gaussian(local) * 2f - (c == WeatherCondition.Rain || c == WeatherCondition.Storm ? 2f : 0f));

        // Risk: sky condition plus a penalty for wind above ~12 knots (launch rules limit wind).
        float windPenalty = Mathf.Max(0f, wind - 12f) / 20f * 0.5f;
        float risk = Mathf.Clamp01(BaseRisk[ci] + windPenalty);
        risk = Mathf.Round(risk * 100f) / 100f;

        return new DayWeather
        {
            dayIndex = index,
            date = firstDay.AddDays(index),
            condition = c,
            windKnots = wind,
            precipitationChance = precip,
            temperatureC = temp,
            risk = risk,
        };
    }

    /// <summary>One line per day, for logs and debugging.</summary>
    public static string Describe(string siteId)
    {
        var sb = new StringBuilder();
        sb.Append(siteId).Append(" (").Append(ProfileFor(siteId).region).Append(", seed ").Append(Seed).Append(")\n");
        foreach (var d in GetForecast(siteId))
        {
            sb.Append(d.Weekday).Append(' ').Append(d.date.ToString("MM-dd")).Append(": ")
              .Append(d.condition).Append(", wind ").Append(d.windKnots.ToString("0")).Append(" kt, rain ")
              .Append(d.precipitationChance).Append("%, ").Append(d.temperatureC.ToString("0")).Append(" C, risk ")
              .Append(d.risk.ToString("0.00")).Append('\n');
        }
        return sb.ToString();
    }

    static int Pick(System.Random rng, float[] weights)
    {
        float total = 0f;
        foreach (float w in weights) total += w;
        float r = (float)rng.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r <= 0f) return i;
        }
        return weights.Length - 1;
    }

    static float Gaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }

    // FNV-1a: stable across runs and platforms (string.GetHashCode is not guaranteed).
    static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char ch in s) { h ^= ch; h *= 16777619; }
            return (int)h;
        }
    }
}
