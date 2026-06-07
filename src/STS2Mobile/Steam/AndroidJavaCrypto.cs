using System;
using Godot;

namespace STS2Mobile.Steam;

public static partial class AndroidJavaCrypto
{
    private readonly struct Base64BridgeCall
    {
        private Base64BridgeCall(
            string operationName,
            string methodName,
            string emptyResponseMessage,
            string[] arguments
        )
        {
            OperationName = operationName;
            MethodName = methodName;
            EmptyResponseMessage = emptyResponseMessage;
            Arguments = arguments;
        }

        private string OperationName { get; }
        private string MethodName { get; }
        private string EmptyResponseMessage { get; }
        private string[] Arguments { get; }

        internal static Base64BridgeCall Create(
            string operationName,
            string methodName,
            string emptyResponseMessage,
            string[] arguments
        )
            => new(operationName, methodName, emptyResponseMessage, arguments);

        internal byte[] Run()
            => AndroidBridgeDispatcher.Run(RunOnMainThread);

        private byte[] RunOnMainThread()
        {
            if (
                !AndroidGodotAppBridge.TryGetInstance(
                    out var app,
                    "[Auth] Java crypto bridge unavailable"
                )
            )
            {
                throw new InvalidOperationException(
                    $"GodotApp Java bridge is unavailable for {OperationName}"
                );
            }

            return RunWithBridge(app);
        }

        private byte[] RunWithBridge(GodotObject app)
        {
            var encoded = (string)app.Call(
                MethodName,
                CreateVariantArguments(Arguments)
            );
            if (string.IsNullOrEmpty(encoded))
                throw new InvalidOperationException(EmptyResponseMessage);

            return Convert.FromBase64String(encoded);
        }
    }

    private static int CopyToDestination(byte[] source, Span<byte> destination, string tooShortMessage)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(tooShortMessage, nameof(destination));

        source.CopyTo(destination);
        return source.Length;
    }

    private static byte[] CallBase64Bridge(
        string operationName,
        string methodName,
        string emptyResponseMessage,
        params string[] arguments
    )
        => Base64BridgeCall
            .Create(
                operationName,
                methodName,
                emptyResponseMessage,
                arguments
            )
            .Run();

    private static Variant[] CreateVariantArguments(string[] arguments)
    {
        var variants = new Variant[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
            variants[i] = arguments[i];

        return variants;
    }
}
