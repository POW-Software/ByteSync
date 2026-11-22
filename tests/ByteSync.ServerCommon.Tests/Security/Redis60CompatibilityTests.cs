using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Repositories;
using ByteSync.ServerCommon.Services;
using FakeItEasy;
using FluentAssertions;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Tests.Security;

public class Redis60CompatibilityTests
{
    private const string AuditRequired = "Redis commands, options and SDK mappings must be reviewed against Azure Redis 6.0 before updating this audit baseline";

    private static readonly HashSet<Type> CommandInterfaces = new[]
        { typeof(IDatabase), typeof(IDatabaseAsync), typeof(ITransaction), typeof(IServer), typeof(ISubscriber) }
        .SelectMany(t => t.GetInterfaces().Append(t)).ToHashSet();

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => o.Value);

    private static readonly HashSet<string> AuditedApis = new(StringComparer.Ordinal)
    {
        Signature("StringGetAsync", typeof(RedisKey), typeof(CommandFlags)),
        Signature("StringSetAsync", typeof(RedisKey), typeof(RedisValue), typeof(TimeSpan?), typeof(When)),
        Signature("StringSetAsync", typeof(RedisKey), typeof(RedisValue), typeof(TimeSpan?), typeof(When), typeof(CommandFlags)),
        Signature("StringSetAsync", typeof(RedisKey), typeof(RedisValue), typeof(TimeSpan?), typeof(bool), typeof(When), typeof(CommandFlags)),
        Signature("KeyDeleteAsync", typeof(RedisKey), typeof(CommandFlags)),
        Signature("SetAddAsync", typeof(RedisKey), typeof(RedisValue), typeof(CommandFlags)),
        Signature("SetRemoveAsync", typeof(RedisKey), typeof(RedisValue), typeof(CommandFlags)),
        Signature("SetMembersAsync", typeof(RedisKey), typeof(CommandFlags)),
        Signature("ScriptEvaluateAsync", typeof(string), typeof(RedisKey[]), typeof(RedisValue[]), typeof(CommandFlags)),
        Signature("CreateTransaction", typeof(object)),
        Signature("ExecuteAsync", typeof(CommandFlags))
    };

    private static readonly HashSet<string> AuditedScripts = new(StringComparer.Ordinal)
    {
        "9B8A14D849ACA456C4D959D2B256B849361DF9E44C1A903A814533F16FCB2549",
        "077D24608F05B5FCC125F221E02CF31FF189127ACF7DF7C6DD12CBE5D43DC197",
        "0FC555D8BD3E0FD9BE8FC585E833428D7C75489ABB3290212D4D5D5EE32F459C"
    };

    private sealed record Operand(MethodBase? Method = null, string? Text = null);
    private sealed record Body(MethodBase Method, List<Operand> Operands);

    private static List<Body> ProductionBodies()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static | BindingFlags.DeclaredOnly;
        return typeof(AuthenticationChallengeService).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(flags).Cast<MethodBase>().Concat(t.GetConstructors(flags))
                .Concat(t.TypeInitializer == null ? Array.Empty<MethodBase>() : new[] { t.TypeInitializer }))
            .Distinct().Select(m => new Body(m, ReadOperands(m))).ToList();
    }

    [Test]
    public void ProductionRedisCalls_UseOnlyAuditedRedis60Overloads()
    {
        var calls = ProductionBodies().SelectMany(b => b.Operands
            .Where(o => IsCommandApi(o.Method)).Select(o => new { Caller = b.Method, Target = o.Method! })).ToList();

        calls.Should().NotBeEmpty();
        calls.Where(c => !IsAuditedApi(c.Target))
            .Select(c => c.Caller.DeclaringType + "." + c.Caller.Name + ": " + Signature(c.Target.Name, c.Target.GetParameters().Select(p => p.ParameterType).ToArray()))
            .Should().BeEmpty(AuditRequired);
    }

    [Test]
    public void ProductionLuaCalls_UseOneAuditedConstantScriptPerMethod()
    {
        var bodies = ProductionBodies().Where(b => b.Operands.Any(o =>
            IsCommandApi(o.Method) && o.Method!.Name.StartsWith("ScriptEvaluate", StringComparison.Ordinal))).ToList();

        bodies.Should().NotBeEmpty();
        foreach (var body in bodies)
        {
            body.Operands.Count(o => IsCommandApi(o.Method) && o.Method!.Name.StartsWith("ScriptEvaluate", StringComparison.Ordinal))
                .Should().Be(1, AuditRequired + ": " + body.Method);
            var scripts = body.Operands.Where(o => o.Text?.Contains("redis.", StringComparison.Ordinal) == true).ToList();
            scripts.Should().ContainSingle(AuditRequired + ": " + body.Method);
            IsAuditedScript(scripts.Single().Text!).Should().BeTrue(AuditRequired + ": " + body.Method);
        }
    }

    [Test]
    public void RedLockEmbeddedScripts_MatchTheAuditedRedis60Scripts()
    {
        var assembly = typeof(RedLockNet.SERedis.RedLockFactory).Assembly;
        var resources = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".lua", StringComparison.Ordinal)).ToList();

        resources.Should().BeEquivalentTo("RedLockNet.SERedis.Lua.Extend.lua", "RedLockNet.SERedis.Lua.Unlock.lua");
        foreach (var resource in resources)
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
            IsAuditedScript(reader.ReadToEnd()).Should().BeTrue(AuditRequired + ": " + resource);
        }
    }

    [Test]
    public void RedisDependencies_MatchTheAuditedCommandMappings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, typeof(Redis60CompatibilityTests).Assembly.GetName().Name + ".deps.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var libraries = document.RootElement.GetProperty("libraries").EnumerateObject().Select(p => p.Name)
            .Where(n => n.StartsWith("StackExchange.Redis/", StringComparison.Ordinal) || n.StartsWith("RedLock.net/", StringComparison.Ordinal));

        libraries.Should().BeEquivalentTo(new[] { "StackExchange.Redis/2.10.1", "RedLock.net/2.3.2" }, AuditRequired);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CacheSave_UsesRelativeTtlOnTheAuditedOverload(bool transactional)
    {
        var transaction = transactional ? A.Fake<ITransaction>() : null;
        var database = (IDatabaseAsync?)transaction ?? A.Fake<IDatabaseAsync>();
        var redis = A.Fake<IRedisInfrastructureService>();
        A.CallTo(() => redis.GetDatabase(transaction)).Returns(database);
        var repository = new CacheRepository<string>(redis);
        var key = new CacheKey { EntityType = EntityType.Session, EntityId = "local-test", Value = "local-test-cache-key" };

        var result = await repository.Save(key, "local-test-value", transaction, A.Fake<RedLockNet.IRedLock>());

        var calls = Fake.GetCalls(database).ToList();
        calls.Should().ContainSingle();
        var call = calls.Single();
        IsAuditedApi(call.Method).Should().BeTrue();
        call.Arguments.Get<RedisKey>(0).Should().Be((RedisKey)key.Value);
        call.Arguments.Get<TimeSpan?>(2).Should().Be(TimeSpan.FromDays(2));
        call.Arguments.Get<When>(3).Should().Be(When.Always);
        result.IsWaitingForTransaction.Should().Be(transactional);
        result.IsSaved.Should().Be(!transactional);
    }

    [TestCase("StringGetDeleteAsync")]
    [TestCase("StringSetAndGetAsync")]
    [TestCase("KeyCopyAsync")]
    [TestCase("KeyExpireTimeAsync")]
    [TestCase("ScriptEvaluateReadOnlyAsync")]
    [TestCase("ExecuteAsync")]
    public void Guard_RejectsUnauditedRedisApisAndRawCommandDispatch(string methodName)
    {
        var methods = CommandInterfaces.SelectMany(t => t.GetMethods()).Where(m => m.Name == methodName)
            .Where(m => methodName != "ExecuteAsync" || m.GetParameters().FirstOrDefault()?.ParameterType == typeof(string)).ToList();

        methods.Should().NotBeEmpty();
        methods.Should().OnlyContain(m => !IsAuditedApi(m));
    }

    [Test]
    public void Guard_RejectsSetOverloadsAllowingAbsoluteExpiryAndNewValueConditions()
    {
        var methods = typeof(IDatabaseAsync).GetMethods().Where(m => m.Name == "StringSetAsync" &&
            m.GetParameters().Any(p => p.ParameterType == typeof(Expiration))).ToList();

        methods.Should().NotBeEmpty();
        methods.Should().OnlyContain(m => !IsAuditedApi(m));
    }

    [TestCase("return redis.call('GETDEL', KEYS[1])")]
    [TestCase("return redis.call('GETEX', KEYS[1], 'EX', 60)")]
    [TestCase("return redis.call('SET', KEYS[1], ARGV[1], 'GET')")]
    [TestCase("return redis.call('SET', KEYS[1], ARGV[1], 'EXAT', ARGV[2])")]
    [TestCase("return redis.call(ARGV[1], KEYS[1])")]
    public void Guard_RequiresReauditForChangedLuaIncludingNewCommandsAndOptions(string script)
    {
        IsAuditedScript(script).Should().BeFalse();
    }

    [Test]
    public void Guard_DetectsAnIncompatibleCallInsideCompiledCode()
    {
        var method = typeof(Redis60CompatibilityTests).GetMethod(nameof(IncompatibleCall), BindingFlags.Static | BindingFlags.NonPublic)!;
        var calls = ReadOperands(method).Where(o => IsCommandApi(o.Method)).ToList();

        calls.Should().ContainSingle();
        calls.Single().Method!.Name.Should().Be("StringGetDeleteAsync");
        IsAuditedApi(calls.Single().Method!).Should().BeFalse();
    }

    private static Task<RedisValue> IncompatibleCall(IDatabaseAsync database) => database.StringGetDeleteAsync("unused-test-key");

    private static bool IsCommandApi(MethodBase? method) => method?.DeclaringType != null && CommandInterfaces.Contains(method.DeclaringType);
    private static bool IsAuditedApi(MethodBase method) => AuditedApis.Contains(Signature(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray()));
    private static string Signature(string name, params Type[] parameters) => name + "(" + string.Join(",", parameters.Select(t => t.FullName)) + ")";
    private static bool IsAuditedScript(string text) => AuditedScripts.Contains(Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n").Trim()))));

    private static List<Operand> ReadOperands(MethodBase method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        var result = new List<Operand>();
        if (bytes == null) return result;
        for (var offset = 0; offset < bytes.Length;)
        {
            var first = bytes[offset++];
            var code = OpCodesByValue[first == 0xfe ? unchecked((short)(0xfe00 | bytes[offset++])) : first];
            if (code.OperandType == OperandType.InlineMethod)
                result.Add(new Operand(method.Module.ResolveMethod(BitConverter.ToInt32(bytes, offset),
                    method.DeclaringType?.GetGenericArguments(), (method as MethodInfo)?.GetGenericArguments())));
            else if (code.OperandType == OperandType.InlineString)
                result.Add(new Operand(Text: method.Module.ResolveString(BitConverter.ToInt32(bytes, offset))));
            offset += code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod or
                    OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, offset),
                _ => throw new InvalidOperationException("Unsupported IL operand in " + method)
            };
        }
        return result;
    }
}
