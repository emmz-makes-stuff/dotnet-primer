using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace AsyncOrders.Tests;

/// <summary>
/// Finds the calls in a method that make a thread wait for a task: <c>Task.Result</c>,
/// <c>Task&lt;T&gt;.Result</c>, <c>Task.Wait</c>, <c>Task.WaitAll</c>, <c>Task.WaitAny</c>, and an awaiter's <c>GetResult()</c> without an
/// <c>await</c>. It reads the IL, so it checks the code, not how long the code took to run.
/// For an <c>async</c> method, the body is in the compiler's state machine, so it reads that too.
/// It checks this one method only, not the methods it calls.
/// </summary>
public static class BlockingCalls
{
    private static readonly Dictionary<short, OpCode> _opCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    private static readonly HashSet<Type> _awaiters =
    [
        typeof(TaskAwaiter),
        typeof(TaskAwaiter<>),
        typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter),
        typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter),
    ];

    /// <summary>Each blocking call in <paramref name="method"/>, named like <c>Task&lt;T&gt;.Result</c>.</summary>
    public static IReadOnlyList<string> In(MethodInfo method)
    {
        var calls = CallsIn(method).ToList();
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } stateMachine)
        {
            calls.AddRange(CallsIn(stateMachine.StateMachineType.GetMethod(
                "MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!));
        }

        // The compiler never emits these, so any call to them is the code's own.
        var found = calls
            .Where(call => IsTask(call.DeclaringType!) && call.Name is "get_Result" or "Wait" or "WaitAll" or "WaitAny")
            .Select(Describe)
            .ToList();

        // The compiler's own await calls GetResult() too, but only after an IsCompleted check. So a
        // GetResult() blocks when there are more of them than IsCompleted checks on that awaiter.
        foreach (var awaiter in calls.Where(call => IsAwaiter(call.DeclaringType!)).GroupBy(call => call.DeclaringType))
        {
            var blocking = awaiter.Count(call => call.Name == "GetResult") - awaiter.Count(call => call.Name == "get_IsCompleted");
            found.AddRange(Enumerable.Repeat(Describe(awaiter.First(call => call.Name == "GetResult")), Math.Max(blocking, 0)));
        }

        return found;
    }

    private static IEnumerable<MethodBase> CallsIn(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        var typeArguments = method.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var at = 0;
        while (at < il.Length)
        {
            // Two-byte opcodes start with 0xFE.
            short value = il[at++];
            if (value == 0xFE)
            {
                value = unchecked((short)(0xFE00 | il[at++]));
            }

            var opCode = _opCodesByValue[value];
            if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
            {
                yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, at), typeArguments, methodArguments)!;
            }

            at += OperandSize(opCode, il, at);
        }
    }

    private static int OperandSize(OpCode opCode, byte[] il, int at) => opCode.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, at)),
        _ => 4,
    };

    private static bool IsTask(Type type) =>
        type == typeof(Task) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>));

    private static bool IsAwaiter(Type type) =>
        _awaiters.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type);

    private static string Describe(MethodBase call)
    {
        var member = call.Name == "get_Result" ? "Result" : call.Name;
        return $"{Name(call.DeclaringType!)}.{member}";
    }

    private static string Name(Type type)
    {
        // A type nested in a generic type is generic too, but only the outer name has the `1.
        var name = type.Name.Contains('`', StringComparison.Ordinal) ? $"{type.Name.Split('`')[0]}<T>" : type.Name;
        return type.IsNested ? $"{Name(type.DeclaringType!)}.{name}" : name;
    }
}
