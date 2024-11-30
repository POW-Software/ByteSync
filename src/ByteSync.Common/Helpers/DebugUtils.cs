using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ByteSync.Common.Helpers;

public class DebugUtils
{
    private static Random _random = new Random();
    
    public static async Task DebugTaskDelay(double seconds, 
        [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        await DebugTaskDelay(seconds, seconds, callerName, callerFilePath);
    }
    
    public static async Task DebugTaskDelay(double minSeconds, double maxSeconds, 
        [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
    #if DEBUG

        double milliseconds;
        if (minSeconds == maxSeconds)
        {
            milliseconds = minSeconds * 1000;
        }
        else
        {
            milliseconds = _random.Next((int)(minSeconds * 1000), (int)(maxSeconds * 1000));
        }

        double seconds = milliseconds / 1000d;
        
        Log.Debug("DebugTaskDelay {seconds} secs from {callerFilePath}.{callerName}", seconds, callerFilePath, callerName);

        await Task.Delay((int)milliseconds);
        
    #else
        await Task.CompletedTask;
    #endif
    }

    /// <summary>
    /// Effectue un "lancer de dés". Renvoie le résultat d'un test en fonction d'une change de réussité (probabilité)
    /// </summary>
    /// <param name="probability0To1">Probabilité que l'action soit exécutée. De 0 à 1. Si 0 : jamais, si 1 : toujours</param>
    /// <param name="callerName"></param>
    /// <param name="callerFilePath"></param>
    public static bool IsRandom(decimal probability0To1,
        [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
    #if DEBUG

        if (probability0To1 is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probability0To1));
        }
        
        if (probability0To1 == 0)
        {
            return false;
        }

        if (probability0To1 == 1)
        {
            return true;
        }

        var decimals = NumericUtils.GetDecimals(probability0To1);

        var iProbability = (int) (probability0To1 * (int) Math.Pow(10, decimals));
        
        var top = (int) Math.Pow(10, decimals);

        var r = _random.Next(top) + 1;

        bool isRandom = r < iProbability;
        
        if (isRandom)
        {
            Log.Debug("RandomExecute: Running action. Call from {callerFilePath}.{callerName}", callerFilePath, callerName);
        }

        return isRandom;

    #else
        return false;
    #endif
    }

    public static void DebugSleep(double seconds,
        [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        DebugSleep(seconds, seconds, callerName, callerFilePath);
    }

    public static void DebugSleep(double minSeconds, double maxSeconds, 
        [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
    #if DEBUG

        double milliseconds;
        if (Math.Abs(minSeconds - maxSeconds) < 0.001d)
        {
            milliseconds = minSeconds * 1000;
        }
        else
        {
            milliseconds = _random.Next((int)(minSeconds * 1000), (int)(maxSeconds * 1000));
        }

        double seconds = milliseconds / 1000d;
        
        Log.Debug("DebugSleep {seconds} secs from {callerFilePath}.{callerName}", seconds, callerFilePath, callerName);

        Thread.Sleep((int)milliseconds);
        
    #else

    #endif
    }
}