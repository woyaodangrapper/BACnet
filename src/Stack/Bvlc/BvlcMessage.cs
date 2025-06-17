namespace BACnet.Stack.Bvlc;

internal class BvlcMessage
{
    public IPEndPoint Sender { get; }
    public BvlcFunction Function { get; }
    public BvlcResult Result { get; }
    public object? Data { get; }

    public BvlcMessage(IPEndPoint sender, BvlcFunction function, BvlcResult result, object? data)
    {
        Sender = sender;
        Function = function;
        Result = result;
        Data = data;
    }
}