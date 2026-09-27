using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Delays that keep working on WebGL. System.Threading.Tasks.Task.Delay uses timers
/// that often never resume on the WebGL main thread, which freezes typing UIs and
/// leaves click TaskCompletionSources unset.
/// </summary>
public static class PlayerLoopDelay
{
    public static async Task WaitMs(int milliseconds)
    {
        await WaitSeconds(milliseconds / 1000f);
    }

    public static async Task WaitSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            await Task.Yield();
            return;
        }

        float endTime = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < endTime)
        {
            await Task.Yield();
        }
    }
}
