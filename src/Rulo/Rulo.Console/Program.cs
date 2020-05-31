using System;
using System.Net;
using System.Threading.Tasks;

using Rulo.Engine;
using Rulo.Engine.BuiltIn.DateTimeFactSources;
using Rulo.Engine.Conditions;
using Rulo.Engine.Conditions.Attributes;
using Rulo.Engine.Conditions.Composed;
using Rulo.Engine.Facts;
using Rulo.Engine.Facts.Attributes;
using Rulo.Engine.Rules;
using Rulo.Engine.Rules.Attributes;

namespace Rulo.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            TestEngineClock testEngineClock = new TestEngineClock();
            testEngineClock.Set(DateTime.Now);

            EngineClock.Default = testEngineClock;

            var factSourceContainer = new FactSourceContainer();
            factSourceContainer
                .Register<CurrentLocalDateTimeFactSource>()
                .Register<MachineNameFactSource>()
                .Register<UserNameFactSource>()
                .Register<LocalIpAddressFactSource>();

            SatisfactionEvaluator evaluator =
                new SatisfactionEvaluator(factSourceContainer);

            IRule testRule = new TestRule();

            var result = await evaluator.EvaluateRule(testRule);

            return 0;
        }
    }

    [FactProperties(
        FactId = MachineNameFactSource.Id,
        Name = "MachineName",
        Description = "The name of the machine",
        ActivationPolicy = FactSourceActivationPolicy.JustOnce
    )]
    class MachineNameFactSource : FactSource<string>
    {
        public override Task<Result<string>> GetFactResult()
        {
            return Task.FromResult(
                new Result<string>(Environment.MachineName, TimeSpan.MaxValue));
        }

        public const string Id = "742f9640-d120-40d9-8336-c5c1923d731d";
    }

    [FactProperties(
        FactId = UserNameFactSource.Id,
        Name = "UserName",
        Description = "The name of the user executing this program",
        ActivationPolicy = FactSourceActivationPolicy.JustOnce
    )]
    class UserNameFactSource : FactSource<string>
    {
        public override Task<Result<string>> GetFactResult()
        {
            return Task.FromResult(
                new Result<string>(Environment.UserName, TimeSpan.MaxValue));
        }

        public const string Id = "975b46e6-c8b7-45e0-87c8-b06a0941b563";
    }

    [FactProperties(
        FactId = LocalIpAddressFactSource.Id,
        Name = "LocalIpAddress",
        Description = "The IP address of the machine",
        ActivationPolicy = FactSourceActivationPolicy.OnEngineStartup
    )]
    class LocalIpAddressFactSource : FactSource<IPAddress[]>
    {
        public override async Task<Result<IPAddress[]>> GetFactResult()
        {
            IPAddress[] result = await Task.Run(() =>
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
                return hostEntry.AddressList;
            });

            return new Result<IPAddress[]>(result, TimeSpan.FromMinutes(5));
        }

        public const string Id = "5db9a96f-b4c4-4111-a0e2-8b6f7a2ce00c";
    }

    [ConditionProperties(
        FactId = LocalIpAddressFactSource.Id,
        Name = "HasIpAddress",
        Description = "Whether or not the local machine has a given IP address"
    )]
    class HasIpAddressCondition : Condition<IPAddress[]>
    {
        public HasIpAddressCondition(IPAddress ipAddress)
        {
            mIpAddress = ipAddress;
        }

        public override Task<SatisfactionStatus> GetSatisfactionStatus()
        {
            if (!HasFactToCheck)
                return Task.FromResult(SatisfactionStatus.Unknown);

            foreach (IPAddress ipAddress in FactToCheck.Data)
            {
                if (ipAddress.Equals(mIpAddress))
                    return Task.FromResult(SatisfactionStatus.Satisfied);
            }

            return Task.FromResult(SatisfactionStatus.Failed);
        }

        readonly IPAddress mIpAddress;
    }

    [ConditionProperties(
        FactId = MachineNameFactSource.Id,
        Name = "HasHostName",
        Description = "Whether or not the local machine has a given name"
    )]
    class HasHostNameCondition : Condition<string>
    {
        public HasHostNameCondition(string machineName)
        {
            mMachineName = machineName;
        }

        public override Task<SatisfactionStatus> GetSatisfactionStatus()
        {
            if (!HasFactToCheck)
                return Task.FromResult(SatisfactionStatus.Unknown);

            return Task.FromResult(
                FactToCheck.Data.Equals(mMachineName)
                    ? SatisfactionStatus.Satisfied
                    : SatisfactionStatus.Failed);
        }

        readonly string mMachineName;
    }

    [ConditionProperties(
        FactId = UserNameFactSource.Id,
        Name = "HasUserName",
        Description = "Whether or not the current user has a given name"
    )]
    class HasUserNameCondition : Condition<string>
    {
        public HasUserNameCondition(string userName)
        {
            mUserName = userName;
        }

        public override Task<SatisfactionStatus> GetSatisfactionStatus()
        {
            if (!HasFactToCheck)
                return Task.FromResult(SatisfactionStatus.Unknown);

            return Task.FromResult(
                FactToCheck.Data.Equals(mUserName)
                    ? SatisfactionStatus.Satisfied
                    : SatisfactionStatus.Failed);
        }

        readonly string mUserName;
    }

    class TestRule : IRule
    {
        public int Priority => 1;

        public Condition Condition => mCondition;

        public TestRule()
        {
            mCondition = new AndCondition(
                new OrCondition(
                    new HasIpAddressCondition(IPAddress.Parse("127.0.0.1")),
                    new HasIpAddressCondition(IPAddress.Parse("192.168.0.16"))),
                new HasHostNameCondition("developers-MacBook-Pro"),
                new HasUserNameCondition("sluisp"));
        
        }

        // public RuleEvaluationResult Fire(
        //     [FactParam(MachineNameFactSource.Id)]string machineName,
        //     [FactParam(UserNameFactSource.Id)]string userName)
        // {
        //     System.Console.WriteLine($"Machine name: {machineName}");
        //     System.Console.WriteLine($"User name: {userName}");

        //     return RuleEvaluationResult.EvaluateNext;
        // }

        public async Task<RuleEvaluationResult> FireAsync(
            [FactParam(MachineNameFactSource.Id)]string machineName,
            [FactParam(UserNameFactSource.Id)]string userName)
        {
            System.Console.WriteLine($"Machine name: {machineName}");
            System.Console.WriteLine($"User name: {userName}");

            await Task.Delay(TimeSpan.FromSeconds(10));

            return RuleEvaluationResult.EvaluateNext;
        }

        readonly Condition mCondition;
    }
}
