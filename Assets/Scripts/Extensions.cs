using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    private static System.Random random = new System.Random();

    public static void Clone<T>(this T source, T target)
    {
        var type = typeof(T);
        foreach (var sourceProperty in type.GetProperties())
        {
            var targetProperty = type.GetProperty(sourceProperty.Name);
            targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
        }
        foreach (var sourceField in type.GetFields())
        {
            var targetField = type.GetField(sourceField.Name);
            targetField.SetValue(target, sourceField.GetValue(source));
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static int Clamp(this int value, int range)
    {
        int a = 0;
        int b = value;

        while (b > 0)
        {
            a++;
            b--;

            if (a >= range - 1)
                a = 0;
        }

        return a;
    }

    public static Color ToColor(this string value)
    {
        if (ColorUtility.TryParseHtmlString(value, out Color color))
        {
            return color;
        }
        else 
        {
            return Color.clear;
        }
    }

    public static string ToHtmlColor(this Color value)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(value)}";
    }
}
