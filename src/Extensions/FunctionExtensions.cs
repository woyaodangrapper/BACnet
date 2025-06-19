using BACnet.Stack.Bvlc;

namespace BACnet.Extensions;

internal static class FunctionExtensions
{
    /// <summary>
    /// 满足异步条件时执行异步逻辑。
    /// </summary>
    public static async Task FunctionIf(
        [NotNull] BvlcFunction function,
        Func<BvlcFunction, bool> condition,
        Func<Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(asyncAction);

        if (condition(function))
        {
            await asyncAction().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 满足条件时执行同步逻辑。
    /// </summary>
    public static void FunctionIf(
        [NotNull] BvlcFunction function,
        Func<BvlcFunction, bool> condition,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(action);

        if (condition(function))
        {
            action();
        }
    }

    public static void IfOk(
       [NotNull] this BvlcFunction now, BvlcFunction condition,
       Action action, bool state)
    {
        if (now == condition && state)
        {
            action();
        }
    }

    public static async ValueTask IfOk(
        this BvlcFunction actual,
        BvlcFunction expected,
        Func<Task> action,
        bool condition,
        CancellationToken cancellationToken = default)
    {
        if (actual == expected && condition && !cancellationToken.IsCancellationRequested)
        {
            await action().ConfigureAwait(false);
        }
    }
}