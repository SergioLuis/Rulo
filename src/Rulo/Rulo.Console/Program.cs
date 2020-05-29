using System;
using System.Net;
using System.Threading.Tasks;

using Rulo.Engine.Engine;
using Rulo.Engine.Engine.BuiltIn.DateTimeFactSources;
using Rulo.Engine.Engine.Conditions;
using Rulo.Engine.Engine.Conditions.Attributes;
using Rulo.Engine.Engine.Facts;
using Rulo.Engine.Engine.Rules;
using Rulo.Engine.Facts;
using Rulo.Engine.Facts.Attributes;

namespace Rulo.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var factSourceContainer = new FactSourceContainer(EngineClock.Default);
            factSourceContainer
                .Register<CurrentLocalDateTimeFactSource>()
                .Register<MachineNameFactSource>()
                .Register<UserNameFactSource>()
                .Register<LocalIpAddressFactSource>();

            var satisfactionChain = new SatisfactionChain(factSourceContainer);
            Condition c = new AndCondition(
                new OrCondition(
                    new HasIpAddressCondition(IPAddress.Parse("127.0.0.1")),
                    new HasIpAddressCondition(IPAddress.Parse("192.168.0.16"))),
                new HasHostNameCondition("developers-MacBook-Pro"),
                new HasUserNameCondition("sluisp"));

            bool isConditionSatisfied = await satisfactionChain.IsSatisfied(c);

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
        public override Task<Result<string>> GetFact()
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
        public override Task<Result<string>> GetFact()
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
        public override async Task<Result<IPAddress[]>> GetFact()
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
        Description = "Whether or not the local machine has a given IP address",
        FactType = typeof(IPAddress[])
    )]
    class HasIpAddressCondition : Condition
    {
        public HasIpAddressCondition(IPAddress ipAddress)
        {
            mIpAddress = ipAddress;
        }

        public override Task<bool> IsSatisfied(object t)
        {
            IPAddress[] ipAddress = t as IPAddress[];
            if (ipAddress == null)
                return Task.FromResult(false);

            foreach (IPAddress address in ipAddress)
            {
                if (address.Equals(mIpAddress))
                    return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        readonly IPAddress mIpAddress;
    }

    [ConditionProperties(
        FactId = MachineNameFactSource.Id,
        Name = "HasHostName",
        Description = "Whether or not the local machine has a given name",
        FactType = typeof(string)
    )]
    class HasHostNameCondition : Condition
    {
        public HasHostNameCondition(string machineName)
        {
            mHostName = machineName;
        }

        public override Task<bool> IsSatisfied(object o)
        {
            string hostName = o as string;
            if (hostName == null)
                return Task.FromResult(false);

            return Task.FromResult(mHostName.Equals(hostName));
        }

        readonly string mHostName;
    }

    [ConditionProperties(
        FactId = UserNameFactSource.Id,
        Name = "HasUserName",
        Description = "Whether or not the current user has a given name",
        FactType = typeof(string)
    )]
    class HasUserNameCondition : Condition
    {
        public HasUserNameCondition(string userName)
        {
            mUserName = userName;
        }

        public override Task<bool> IsSatisfied(object o)
        {
            string userName = o as string;
            if (userName == null)
                return Task.FromResult(false);

            return Task.FromResult(mUserName.Equals(userName));
        }

        readonly string mUserName;
    }
}
