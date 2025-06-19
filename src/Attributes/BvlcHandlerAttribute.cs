using BACnet.Stack.Bvlc;

namespace BACnet.Attributes
{
    /// <summary>
    /// 标记 BVLC 功能处理方法的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BvlcHandlerAttribute(BvlcFunction function, int minLength = 0) : Attribute
    {
        /// <summary>
        /// 对应的 BVLC 功能枚举
        /// </summary>
        public BvlcFunction Function { get; } = function;

        /// <summary>
        /// 该功能数据包最小长度限制（可选）
        /// </summary>
        public int MinLength { get; } = minLength;
    }
}