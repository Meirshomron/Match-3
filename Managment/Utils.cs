using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class Utils
{
    public static T GetRandom<T>(this ICollection<T> collection)
    {
        if (collection == null)
            return default(T);
        int t = UnityEngine.Random.Range(0, collection.Count);
        foreach (T element in collection)
        {
            if (t == 0)
                return element;
            t--;
        }
        return default(T);
    }

    public static int BoolToBinary(bool b) => b ? 1 : 0;
    public static bool BinaryToBool(int val) => val == 0 ? false : true;

    /// <summary>
    /// Return the values of a tuple (int, int) when it's reset.
    /// </summary>
    public static (int, int) GetTupleResetValues()
    {
        return (-1, -1);
    }

    /// <summary>
    /// Return if the given tuple's values are of a reset tuple.
    /// </summary>
    public static bool IsTupleEmpty((int, int) tuple)
    {
        return (tuple.Item1 == -1 && tuple.Item2 == -1);
    }

    public static Texture2D ConvertSpriteToTexture(Sprite sprite)
    {
        var croppedTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        var pixels = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                (int)sprite.textureRect.y,
                                                (int)sprite.textureRect.width,
                                                (int)sprite.textureRect.height);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        return croppedTexture;
    }

    public static void Shuffle(int[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            int rnd = Random.Range(0, arr.Length);
            int temp = arr[rnd];
            arr[rnd] = arr[i];
            arr[i] = temp;
        }
    }

    public static int[] GetRandomNumbersInArr(int amount, int[] arr)
    {
        int[] result = new int[amount];
        Shuffle(arr);
        for (int i = 0; i < amount; i++)
        {
            result[i] = arr[i];
        }
        return result;
    }
}