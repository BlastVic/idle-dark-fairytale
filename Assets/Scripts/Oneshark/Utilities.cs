using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

public static class Utilities
{
    #region MONO Extensions
    public static void ResetTransformation(this Transform trans)
    {
        trans.position = Vector3.zero;
        trans.localRotation = Quaternion.identity;
        trans.localScale = new Vector3(1, 1, 1);
    }


    public static float Distance2D(Vector3 v1, Vector3 v2)
    {

        Vector3 difference = new Vector3(
          v1.x - v2.x,
          v1.y - v2.y,
          v1.z - v2.z);

        //affect the difference by 2d
        difference.y = 0;

        float distance = (float)Math.Sqrt(
          Math.Pow(difference.x, 2f) +
          Math.Pow(difference.y, 2f) +
          Math.Pow(difference.z, 2f));

        if (Mathf.Abs(difference.z) > 2.9f) distance = 999;//if z is greater than one, make distance huge

        return distance;
    }

    #endregion
    #region COLOR TO STRING
    public static string colorToString(Color color)
    {
        return color.r + "," + color.g + "," + color.b + "," + color.a;
    }
    public static Color stringToColor(string colorString)
    {
        try
        {
            string[] colors = colorString.Split(',');
            return new Color(float.Parse(colors[0]), float.Parse(colors[1]), float.Parse(colors[2]), float.Parse(colors[3]));
        }
        catch
        {
            return Color.white;
        }
    }
    #endregion
    // Convert an object to a byte array
    public static byte[] ObjectToByteArray(System.Object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    // Convert a byte array to an Object
    public static System.Object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }

    public static int LayermaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }

    public static void WriteToText(string contents, string path)
    {
        string prefix = "Assets/Resources/";
        string combinedPath = prefix + path;
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(combinedPath, true);
        writer.WriteLine(contents);
        writer.Close();

        //Debug.Log("WriteToText");
    }

    public static bool IsEven(float num)
    {
        if (num % 2 == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static List<StringPair> ImportLanguageCSV(string langTag)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Translation_" + langTag + ".csv");

        List<StringPair> list = new List<StringPair>();

        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');
                list.Add(new StringPair { m_Key = values[0], m_Value = values[2] });
            }
        }
        return list;
    }

    public static string FirstCharToUpper(string input)
    {
        if (String.IsNullOrEmpty(input))
            throw new ArgumentException("ARGH!");
        return input.First().ToString().ToUpper() + input.Substring(1);
    }

    public static T DeepClone<T>(T obj)
    //example         List<Wave> newWaves = Utilities.DeepClone(currentWaveset.waves);

    {
        using (var ms = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;

            return (T)formatter.Deserialize(ms);
        }
    }

    public static float Truncate(this float value, int digits)
    {
        double mult = Math.Pow(10.0, digits);
        double result = Math.Truncate(mult * value) / mult;
        return (float)result;
    }

    public static int getRandomElement(int[] array, int searchForThisValue)
    {
        List<int> possibleChoices = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == searchForThisValue) possibleChoices.Add(i);
        }
        return possibleChoices[UnityEngine.Random.Range(0, possibleChoices.Count)]; //pick one at random and return it;
    }

    private static System.Random rng = new System.Random();

    public static void ShuffleList<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static GameObject[] Shuffle(GameObject[] charArray)
    {
        GameObject[] shuffledArray = new GameObject[charArray.Length];
        int rndNo;

        System.Random rnd = new System.Random();
        for (int i = charArray.Length; i >= 1; i--)
        {
            rndNo = rnd.Next(1, i + 1) - 1;
            shuffledArray[i - 1] = charArray[rndNo];
            charArray[rndNo] = charArray[i - 1];
        }
        return shuffledArray;
    }

    public static string FormatCurrency(int raw, string characterSeperator)
    {
        string formatted = string.Format("{0:n0}", raw);
        formatted = formatted.Replace(",", characterSeperator);
        return formatted;
    }

    public static string FormatComma(int val)
    {
        return string.Format("{0:n0}", val);
    }

    //EX 1.3K   300.2M
    public static string ConvertNumber(float raw)
    {
        float num = raw;
        string convertedString = "";
       
        //if (raw >= 1000 && raw <1000000)
        //{
        //     num = raw * .001f;
        //    convertedString =num.ToString("0.0");
        //    return string.Concat(convertedString, "K");
        //}
        if (raw < 1000)
        {
            //just need to round these
            float convertedFloat = Mathf.Round(raw);
            return convertedFloat.ToString();
        }
        if (raw >= 1000 && raw < 1000000)
        {
            float firstSegment = raw / 1000;
            firstSegment *= 10;
            firstSegment = Mathf.Round(firstSegment);
            firstSegment /= 10;
            return string.Concat(firstSegment.ToString(), "K");
        }

        if (raw >= 1000000 && raw < 1000000000)
        {
            float firstSegment = raw / 1000000;
            firstSegment*= 10;
            firstSegment = Mathf.Round(firstSegment);
            firstSegment /= 10;
            return string.Concat(firstSegment.ToString(), "M");
        }

        if (raw >= 1000000000 && raw < 1000000000000)
        {
            float firstSegment = raw / 1000000000;
            firstSegment *= 10;
            firstSegment = Mathf.Round(firstSegment);
            firstSegment /= 10;
            return string.Concat(firstSegment.ToString(), "B");
        }

        if (raw >= 1000000000000 && raw < 1000000000000000)
        {
            float firstSegment = raw / 1000000000000;
            firstSegment *= 10;
            firstSegment = Mathf.Round(firstSegment);
            firstSegment /= 10;
            return string.Concat(firstSegment.ToString(), "T");
        }


        //if (num >= 1000000000 & num < 1000000000000)
        //    return string.Concat(convertedString, "B");

        //if (num >= 1000000000000 & num < 1000000000000000)
        //    return string.Concat(convertedString, "T");

        return num.ToString();
    }

    public static string FormatThouMilBil(float raw)
    {
        //over 1k under 1m
        if (raw >= 1000 & raw < 1000000)
        {
            float newRaw = raw / 1000;
            int rawInt = (int)newRaw;

            string formattedString = rawInt.ToString();
            //formattedString.Substring (formattedString.Length - 3);
            return rawInt + "K";
        }

        //over 1m under 1b
        if (raw >= 1000000 & raw < 1000000000)
        {
            float newRaw = raw / 1000000;
            int rawInt = (int)newRaw;

            string formattedString = rawInt.ToString();
            //formattedString.Substring (formattedString.Length - 3);
            return rawInt + "M";
        }

        //over 1b under 1t
        if (raw >= 1000000000 & raw < 1000000000000)
        {
            float newRaw = raw / 1000000000;
            int rawInt = (int)newRaw;

            string formattedString = rawInt.ToString();
            //formattedString.Substring (formattedString.Length - 3);
            return rawInt + "B";
        }

        //over 1t under 1qa
        if (raw >= 1000000000000 & raw < 1000000000000000)
        {
            float newRaw = raw / 1000000000000;
            int rawInt = (int)newRaw;

            string formattedString = rawInt.ToString();
            //formattedString.Substring (formattedString.Length - 3);
            return rawInt + "T";
        }

        //over 1qa under 1qu
        if (raw >= 1000000000000000 & raw < 1000000000000000000)
        {
            float newRaw = raw / 1000000000000000;
            int rawInt = (int)newRaw;

            string formattedString = rawInt.ToString();
            //formattedString.Substring (formattedString.Length - 3);
            return rawInt + "Q";
        }


        //you should never reach this
        int defaultRawValue = (int)raw;
        return defaultRawValue.ToString();
    }

    public static string GradeReplace(string s)
    {
        if (s == "EPIC") return "Epic";
        if (s == "ORDINARY") return "Ordinary";
        if (s == "RARE") return "Rare";
        return "Ordinary";
    }

    public static string getMonthByNumber(int i)
    {
        string monthName = "Jan";
        switch (i)
        {
            case 1:
                monthName = "Jan";
                break;
            case 2:
                monthName = "Feb";
                break;
            case 3:
                monthName = "Mar";
                break;
            case 4:
                monthName = "Apr";
                break;
            case 5:
                monthName = "May";
                break;
            case 6:
                monthName = "Jun";
                break;
            case 7:
                monthName = "Jul";
                break;
            case 8:
                monthName = "Aug";
                break;
            case 9:
                monthName = "Sept";
                break;
            case 10:
                monthName = "Oct";
                break;
            case 11:
                monthName = "Nov";
                break;
            case 12:
                monthName = "Dec";
                break;
        }
        return monthName;
    }

    public static string getDayByNumber(int i)
    {
        string s = "";
        switch (i)
        {
            case 1:
                s = "Sun";
                break;
            case 2:
                s = "Mon";
                break;
            case 3:
                s = "Tues";
                break;
            case 4:
                s = "Wed";
                break;
            case 5:
                s = "Thurs";
                break;
            case 6:
                s = "Fri";
                break;
            case 7:
                s = "Sat";
                break;
        }
        return s;
    }

    public static bool IsDivisible(int x, int n)
    {
        return (x % n) == 0;
    }



    public static bool IsDivisible(double x, double n)
    {
        return (x % n) == 0;
    }

}

[System.Serializable]
public class FloatPair
{
    public float m_Key;
    public float m_Val;

}

[System.Serializable]
public class StatUpgradeSet
{

    [Header("Applies to: ATK, ATK SPD, HP, SP, BLOCK, DEF")]
    public float m_Stat1;
    [Header("Applies to: CRIT POW, CRIT, PIERCE")]
    public float m_Stat2;
    //[Header("Applies to: CRIT POW")]
    //public float m_Stat3;
}

[System.Serializable]
public class StringPair
{
    public string m_Key;
    public string m_Value;
}

[System.Serializable]
public class StringFloatPair
{
    public string m_Key;
    public float m_Value;
}


[System.Serializable]
public class StringIntPair
{
    public string m_Key;
    public int m_Value;
}



