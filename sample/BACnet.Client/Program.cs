// See https://aka.ms/new-console-template for more information
//using BACnet;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;

//Console.WriteLine("Hello, World!");
//var builder = WebApplication.CreateSlimBuilder();

//var console = LoggerFactory.Create(builder =>
//{
//    builder
//        .AddConsole()
//        .SetMinimumLevel(LogLevel.Trace);
//});

//WebApplication app = builder.Build();
//using IServiceScope scope = app.Services.CreateScope();

//var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
//var bacnet = new Bacnet(new("bacnet"), loggerFactory);
//bacnet.Start();
//bacnet.WhoIs();
using System.Reactive.Linq;
using System.Reactive.Subjects;

// —— 1. 状态枚举 ——
public enum DeviceState
{
    Stopped,
    Starting,
    Running,
    Faulted,
    Registered,
    Unregistered
}

// —— 2. 事件驱动的状态机 ——
public class EventDrivenStateMachine
{
    private readonly BehaviorSubject<DeviceState> _stateSubject;
    public IObservable<DeviceState> StateChanged => _stateSubject.DistinctUntilChanged();
    public DeviceState Current => _stateSubject.Value;

    public EventDrivenStateMachine(DeviceState initial)
        => _stateSubject = new BehaviorSubject<DeviceState>(initial);

    public void ChangeTo(DeviceState next)
    {
        Console.WriteLine($"State: {Current} → {next}");
        _stateSubject.OnNext(next);
    }
}

// —— 3. ExecuteWithState 保留 ——
public static class StateMachineExtensions
{
    /// <summary>
    /// 执行业务逻辑，然后切换状态机到 newState。
    /// </summary>
    public static void ExecuteAndChange(this EventDrivenStateMachine sm, Action action, DeviceState newState)
    {
        action();
        sm.ChangeTo(newState);
    }

    /// <summary>
    /// 仅当当前状态等于 expected 时，才执行 action。
    /// </summary>
    public static void ExecuteIfState(this EventDrivenStateMachine sm, DeviceState expected, Action action)
    {
        if (sm.Current == expected)
        {
            action();
        }
    }

    /// <summary>
    /// 注册一个订阅：当状态变为 <paramref name="state"/> 时，执行 <paramref name="callback"/>。
    /// 返回 IDisposable，可用于取消订阅。
    /// </summary>
    public static IDisposable AddSubscribe(this EventDrivenStateMachine sm, DeviceState state, Action callback)
    {
        return sm.StateChanged
                 .Where(s => s == state)
                 .Subscribe(_ => callback());
    }
}

// —— 4. 业务类 ——
public class BbmdBehavior
{
    private readonly EventDrivenStateMachine _sm;

    public BbmdBehavior(EventDrivenStateMachine sm)
    {
        _sm = sm;

        // —— 5. 订阅状态变化事件 ——
        _sm.StateChanged
           .Where(s => s == DeviceState.Running)
           .Subscribe(_ => InitListeners());

        _sm.StateChanged
           .Where(s => s == DeviceState.Faulted)
           .Subscribe(_ => LogFault());
    }

    // 核心启动逻辑（不再直接调用 ChangeTo）
    public void StartCore()
        => Console.WriteLine(">>> OnStartCore 业务逻辑");

    private void InitListeners()
        => Console.WriteLine(">>> InitListeners 回调逻辑");

    private void LogFault()
        => Console.WriteLine(">>> LogFault 回调逻辑");
}

// —— 6. 示例运行 ——
internal class Program
{
    private static void Main()
    {
        var sm = new EventDrivenStateMachine(DeviceState.Stopped);
        var biz = new BbmdBehavior(sm);

        // 1) 执行业务并切换到 Starting
        sm.ExecuteAndChange(biz.StartCore, DeviceState.Starting);
        // 控制台：
        //   >>> OnStartCore 业务逻辑
        //   State: Stopped → Starting

        // 2) 模拟内部逻辑后进 Running
        sm.ExecuteAndChange(() => { }, DeviceState.Running);
        // 控制台：
        //   State: Starting → Running
        //   >>> InitListeners 回调逻辑

        // 3) 模拟故障
        sm.ExecuteAndChange(() => { }, DeviceState.Faulted);
        // 控制台：
        //   State: Running → Faulted
        //   >>> LogFault 回调逻辑
    }
}