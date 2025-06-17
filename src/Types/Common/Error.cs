namespace BACnet.Types.Common;

public struct Error
{
    internal ErrorClasses _errorClass;
    internal ErrorCodes _errorCode;

    public ErrorClasses ErrorClass
    {
        readonly get => _errorClass;
        set => _errorClass = value;
    }

    public ErrorCodes ErrorCode
    {
        readonly get => _errorCode;
        set => _errorCode = value;
    }
    public Error(ErrorClasses errorClass, ErrorCodes errorCode)
    {
        _errorClass = errorClass;
        _errorCode = errorCode;
    }

    public Error(uint errorClass, uint errorCode)
    {
        _errorClass = (ErrorClasses)errorClass;
        _errorCode = (ErrorCodes)errorCode;
    }

    public override readonly string ToString() => $"{_errorClass}: {_errorCode}";
}