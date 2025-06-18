using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BACnet.Stack.Bvlc;

public enum DeviceType
{
    FD,
    Non,
    BBMD,
    Hybrid
}

public enum DeviceState
{
    Initial,        // 设备初始化
    Stopped,        // 设备停止状态
    Starting,       // 设备启动中
    Running,        // 设备运行中
    Faulted,        // 设备故障状态
    Registered,     // FD 专属状态：已注册
    Unregistered    // FD 专属状态：未注册
}

public class BvlcStateMachine(DeviceState initial) : IDisposable
{
    private readonly BehaviorSubject<DeviceState> _stateSubject = new(initial);
    private bool _disposed;

    public BvlcStateMachine()
        : this(DeviceState.Initial)

    { }

    public IObservable<DeviceState> StateChanged => _stateSubject.DistinctUntilChanged();
    public DeviceState Current => _stateSubject.Value;

    public void ChangeTo(DeviceState next)
    {
        Console.WriteLine($"State: {Current} → {next}");
        _stateSubject.OnNext(next);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _stateSubject.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BvlcStateMachine()
    {
        Dispose(false);
    }
}

public static class BvlcStateMachineExtensions
{
    /// <summary>
    /// 执行业务逻辑，然后切换状态机到 newState。
    /// </summary>
    public static void ExecuteAndChange(this BvlcStateMachine bvlcState, Action action, DeviceState newState)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(bvlcState);

        action();
        bvlcState.ChangeTo(newState);
    }

    /// <summary>
    /// 仅当当前状态等于 expected 时，才执行 action。
    /// </summary>
    public static void ExecuteIfState(this BvlcStateMachine bvlcState, Action action, DeviceState expected)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(bvlcState);

        if (bvlcState.Current == expected)
        {
            action();
        }
    }

    /// <summary>
    /// 注册一个订阅：当状态变为 <paramref name="state"/> 时，执行 <paramref name="callback"/>。
    /// 返回 IDisposable，可用于取消订阅。
    /// </summary>
    public static IDisposable AddSubscribe(this BvlcStateMachine bvlcState, Action callback, DeviceState state)
    {
        ArgumentNullException.ThrowIfNull(bvlcState);
        ArgumentNullException.ThrowIfNull(callback);

        return bvlcState.StateChanged
                 .Where(s => s == state)
                 .Subscribe(_ => callback());
    }

    /// <summary>
    /// 异步执行业务逻辑，然后切换状态机到 newState。
    /// </summary>
    public static async Task ExecuteAndChangeAsync(this BvlcStateMachine bvlcState, Func<Task> action, DeviceState newState)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(bvlcState);

        await action().ConfigureAwait(false);
        bvlcState.ChangeTo(newState);
    }

    /// <summary>
    /// 仅当当前状态等于 expected 时，才执行异步 action。
    /// </summary>
    public static async Task ExecuteIfStateAsync(this BvlcStateMachine bvlcState, Func<Task> action, DeviceState expected)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(bvlcState);

        if (bvlcState.Current == expected)
        {
            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 异步注册一个订阅：当状态变为 <paramref name="state"/> 时，执行 <paramref name="callback"/>。
    /// 返回 IDisposable，可用于取消订阅。
    /// </summary>
    public static IDisposable AddSubscribeAsync(this BvlcStateMachine bvlcState, Func<Task> callback, DeviceState state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(bvlcState);

        return bvlcState.StateChanged
            .Where(s => s == state)
            .SelectMany(async s =>
            {
                await callback().ConfigureAwait(false);
                return s;
            })
            .Subscribe(_ => { });
    }

    /// <summary>
    /// 满足条件 predicate 时执行 action。
    /// </summary>
    public static void ExecuteIf(this BvlcStateMachine sm, Func<bool> condition, Action action)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(sm);

        if (condition())
        {
            action();
        }
    }

    /// <summary>
    /// 允许访问当前状态进行判断。
    /// </summary>
    public static void ExecuteIf(this BvlcStateMachine sm, Func<DeviceState, bool> predicate, Action action)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(sm);

        if (predicate(sm.Current))
        {
            action();
        }
    }

    /// <summary>
    /// 满足异步条件时执行异步逻辑。
    /// </summary>
    public static async Task ExecuteIfAsync(
        [NotNull] this BvlcStateMachine sm,
        Func<bool> condition,
        Func<Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(asyncAction);

        if (condition())
        {
            await asyncAction().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 满足基于状态的条件时执行异步逻辑。
    /// </summary>
    public static async Task ExecuteIfAsync(
        [NotNull] this BvlcStateMachine sm,
        Func<DeviceState, bool> predicate,
        Func<Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(asyncAction);

        if (predicate(sm.Current))
        {
            await asyncAction().ConfigureAwait(false);
        }
    }
}