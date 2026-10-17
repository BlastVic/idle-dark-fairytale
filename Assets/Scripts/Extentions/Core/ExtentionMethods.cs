using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Assets.Scripts.Extentions
{
    public static class ExtentionMethods
    {
        private const Single ComparisonTolerance = 0.0000001f;
        #region ActionsExtentions
        public static void SafeInvoke(this Action action)
        {
            if (action == null)
                return;

            action();
        }

        public static void SafeInvoke<T>(this Action<T> action, T arg1)
        {
            if (action == null)
                return;

            action(arg1);
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null)
                return;

            action(arg1, arg2);
        }

        public static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            if (action == null)
                return;

            action(arg1, arg2, arg3);
        }
        #endregion

        public static Boolean IsEqual(this Single a, Single b, Single comparisonTolerance)
        {
            return Math.Abs(a - b) < comparisonTolerance;
        }

        public static Boolean IsEqual(this Single a, Single b)
        {
            return Math.Abs(a - b) < ComparisonTolerance;
        }

        public static Boolean IsEqual(this Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) < ComparisonTolerance;
        }

        public static Byte[] ToByteArray(this Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static Object ToObject(this Byte[] arrBytes)
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

        public static Byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                var buffer = new Byte[bufferSize];
                Int32 count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture)
        {
            var temp = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };

            RenderTexture.active = renderTexture;
            temp.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            temp.Apply();
            RenderTexture.active = null;

            return temp;
        }

        public static String GetObjectPath(this Transform transform, Boolean includeRootName = true)
        {
            var path = "";
            do
            {
                if (transform.parent == null && !includeRootName)
                {
                    transform = null;
                    continue;
                }

                path = transform.name + "/" + path;
                transform = transform.parent;
            } while (transform != null);

            return path;
        }

        public static Boolean PlayState(this Animator animator, String stateName, Int32 layerIndex = 0)
        {
            if (animator != null)
            {
                if (animator.HasState(layerIndex, Animator.StringToHash(stateName)))
                {
                    animator.Play(stateName);
                    return true;
                }

                Debug.LogError(String.Format("State '{0}' not found on object: {1}", stateName, animator.transform.GetObjectPath()), animator);
            }
            return false;
        }

        public static void SetAlpha(this Image image, Single alpha)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }

        public static String UnescapeCharacters(this String value)
        {
            var result = "";
            for (var i = 0; i < value.Length; ++i)
            {
                if (value[i] == '\\' && i + 1 < value.Length)
                {
                    switch (value[i + 1])
                    {
                        case 'n':
                            {
                                result += '\n';
                                ++i;
                                break;
                            }
                        case 'r':
                            {
                                result += '\r';
                                ++i;
                                break;
                            }
                        case 't':
                            {
                                result += '\t';
                                ++i;
                                break;
                            }
                        case '\\':
                            {
                                result += '\\';
                                ++i;
                                break;
                            }
                        default:
                            {
                                Debug.LogError("Unexpected escape sequence: " + value[i] + value[i + 1]);
                                result += '\\';
                                break;
                            }
                    }
                }
                else
                {
                    result += value[i];
                }
            }
            return result;
        }

        public static IEnumerable<Transform> GetChilds(this Transform parent)
        {
            for (var i = 0; i < parent.childCount; ++i)
            {
                yield return parent.GetChild(i);
            }
        }

        public static Gradient Copy(this Gradient gradient)
        {
            var copy = new Gradient { mode = gradient.mode };
            var colorKeys = gradient.colorKeys;
            for (var i = 0; i < colorKeys.Length; ++i)
            {
                colorKeys[i] = new GradientColorKey(colorKeys[i].color, colorKeys[i].time);
            }

            var alphaKeys = gradient.alphaKeys;
            for (var i = 0; i < alphaKeys.Length; ++i)
            {
                alphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha, alphaKeys[i].time);
            }

            copy.SetKeys(colorKeys, alphaKeys);
            return copy;
        }

        public static T Random<T>(this IEnumerable<T> source)
        {
            if (source is IList<T> list)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }

            var current = default(T);
            var count = 0;
            foreach (var element in source)
            {
                count++;
                if (UnityEngine.Random.Range(0, count) == 0)
                {
                    current = element;
                }
            }

            if (count == 0)
            {
                throw new InvalidOperationException("Sequence was empty");
            }
            return current;
        }
    }
}