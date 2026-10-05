using System;
using System.Collections;
using Assets.Scripts.Extentions;
using UnityEngine;

namespace Assets.Scripts.Services
{
    public class CoroutinesHolder : MonoSingleton<CoroutinesHolder>
    {
        public IEnumerator AfterFrames(Int32 frames, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterFramesCoroutine(frames, action);
            StartCoroutine(coroutine);
            return coroutine;
        }

        public void NextFrame<TArg0>(Action<TArg0> action, TArg0 arg0)
        {
            AfterFrames(1, action, arg0);
        }

        public void AfterFrames<TArg0>(Int32 frames, Action<TArg0> action, TArg0 arg0)
        {
            if (action == null)
                return;

            StartCoroutine(AfterFramesCoroutine(frames, action, arg0));
        }

        public IEnumerator AfterSeconds(Single seconds, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterSecondsCoroutine(seconds, action);
            StartCoroutine(coroutine);
            return coroutine;
        }

        public IEnumerator AfterRealSeconds(Single seconds, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterRealSecondsCoroutine(seconds, action);
            StartCoroutine(coroutine);
            return coroutine;
        }

        public void AfterSeconds<TArg0>(Single seconds, Action<TArg0> action, TArg0 arg0)
        {
            if (action == null)
                return;

            StartCoroutine(AfterSecondsCoroutine(seconds, action, arg0));
        }

        public void EndOfFrame(Action action)
        {
            if (action == null)
                return;

            StartCoroutine(EndOfFrameCoroutine(action));
        }

        public void EndOfFrame<TArg0>(Action<TArg0> action, TArg0 arg0)
        {
            if (action == null)
                return;

            StartCoroutine(EndOfFrameCoroutine(action, arg0));
        }

        public void OnFixedUpdate(Action action)
        {
            if (action == null)
                return;

            StartCoroutine(OnFixedUpdateCoroutine(action));
        }

        public void OnFixedUpdate<T>(Action<T> action, T arg)
        {
            if (action == null)
                return;

            StartCoroutine(OnFixedUpdateCoroutine(action, arg));
        }

        public static void NextFrame(ref IEnumerator coroutinePointer, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = Instance.AfterFrames(1, action);
        }

        public static IEnumerator NextFrame(Action action)
        {
            return Instance.AfterFrames(1, action);
        }

        public static void AfterSeconds(ref IEnumerator coroutinePointer, Single seconds, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = Instance.AfterSeconds(seconds, action);
        }

        public static void AfterRealSeconds(ref IEnumerator coroutinePointer, Single seconds, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = Instance.AfterRealSeconds(seconds, action);
        }

        public static IEnumerator AfterNewAnimationComplete(Animator animator, Single completionPercentage, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterNewAnimationCompleteCoroutine(animator, completionPercentage, action);
            Instance.StartCoroutine(coroutine);
            return coroutine;
        }

        public static IEnumerator AfterNewAnimationComplete(Animator animator, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterNewAnimationCompleteCoroutine(animator, 1f, action);
            Instance.StartCoroutine(coroutine);
            return coroutine;
        }

        public static void AfterNewAnimationComplete(ref IEnumerator coroutinePointer, Animator animator, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = AfterNewAnimationComplete(animator, action);
        }

        public static void AfterNewAnimationComplete(ref IEnumerator coroutinePointer, Animator animator, Single completionPercentage, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = AfterNewAnimationComplete(animator, completionPercentage, action);
        }

        public static void AfterConditionInvalid(ref IEnumerator coroutinePointer, Func<Boolean> condition, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = ConditionalAwaitCoroutine(condition, action);
            Instance.StartCoroutine(coroutinePointer);
        }

        public static IEnumerator AfterYieldInstruction(YieldInstruction instruction, Action action)
        {
            if (action == null)
                return null;

            var coroutine = AfterYieldInstructionCoroutine(instruction, action);
            Instance.StartCoroutine(coroutine);
            return coroutine;
        }

        public static void AfterYieldInstruction(ref IEnumerator coroutinePointer, YieldInstruction instruction, Action action)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = AfterYieldInstruction(instruction, action);
        }

        public static void Run(ref IEnumerator coroutinePointer, Func<IEnumerator> coroutine)
        {
            Stop(ref coroutinePointer);
            coroutinePointer = coroutine();
            Instance.StartCoroutine(coroutinePointer);
        }

        public static void Stop(ref IEnumerator coroutinePointer)
        {
            if (coroutinePointer != null)
            {
                Instance.StopCoroutine(coroutinePointer);
                coroutinePointer = null;
            }
        }

        private static IEnumerator AfterYieldInstructionCoroutine(YieldInstruction instruction, Action action)
        {
            if (action == null)
                yield break;

            yield return instruction;

            action();
        }

        public static IEnumerator DownloadImageCoroutine(String url, Action<Sprite> callback)
        {
            using (var www = new WWW(url))
            {
                yield return www;

                if (www.error == null)
                {
                    var texture = www.texture;
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (sprite == null)
                    {
                        Debug.LogError(String.Format("Unable to create sprite for image at url {0}", url));
                        callback.SafeInvoke(null);
                        yield break;
                    }
                    callback.SafeInvoke(sprite);
                }
                else
                {
                    Debug.LogError("WWW Error: " + www.error);
                    callback.SafeInvoke(null);
                }
            }
        }

        private static IEnumerator AfterFramesCoroutine(Int32 frames, Action action)
        {
            if (action == null)
                yield break;

            for (var i = 0; i < frames; i++)
                yield return null;

            action();
        }

        private static IEnumerator AfterFramesCoroutine<TArg0>(Int32 frames, Action<TArg0> action, TArg0 arg0)
        {
            if (action == null)
                yield break;

            for (var i = 0; i < frames; i++)
                yield return null;

            action(arg0);
        }

        private static IEnumerator AfterSecondsCoroutine(Single seconds, Action action)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }
            action.SafeInvoke();
        }

        private static IEnumerator AfterRealSecondsCoroutine(Single seconds, Action action)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSecondsRealtime(seconds);
            }
            action.SafeInvoke();
        }

        private static IEnumerator AfterSecondsCoroutine<TArg0>(Single seconds, Action<TArg0> action, TArg0 arg0)
        {
            yield return new WaitForSeconds(seconds);
            action.SafeInvoke(arg0);
        }

        private static IEnumerator EndOfFrameCoroutine(Action action)
        {
            yield return new WaitForEndOfFrame();
            action.SafeInvoke();
        }

        private static IEnumerator EndOfFrameCoroutine<TArg0>(Action<TArg0> action, TArg0 arg0)
        {
            yield return new WaitForEndOfFrame();
            action.SafeInvoke(arg0);
        }

        private static IEnumerator OnFixedUpdateCoroutine(Action action)
        {
            yield return new WaitForFixedUpdate();
            action.SafeInvoke();
        }

        private static IEnumerator OnFixedUpdateCoroutine<T>(Action<T> action, T arg)
        {
            yield return new WaitForFixedUpdate();
            action.SafeInvoke(arg);
        }

        private static IEnumerator AfterNewAnimationCompleteCoroutine(Animator animator, Single completionPercentage, Action action)
        {
            var initialFrameTime = Time.time;
            yield return null;
            var awaitTime = animator.GetCurrentAnimatorStateInfo(0).length * completionPercentage - Time.time + initialFrameTime;
            if (awaitTime > 0)
            {
                if (animator.updateMode == AnimatorUpdateMode.UnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(awaitTime);
                }
                else
                {
                    yield return new WaitForSeconds(awaitTime);

                }
            }

            action.SafeInvoke();
        }

        private static IEnumerator ConditionalAwaitCoroutine(Func<Boolean> awaitCondition, Action action)
        {
            while (awaitCondition())
            {
                yield return null;
            }
            action.SafeInvoke();
        }
    }
}
