using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rulo.Engine.Facts;
using Rulo.Engine.Facts.Attributes;

namespace Rulo.Engine.Engine.Facts
{
    public class FactSourceContainer
    {
        public FactSourceContainer(IEngineClock clock)
        {
            mEngineClock = clock;
            mFactSources = new Dictionary<string, Type>();
            mActivatedFactSources = new Dictionary<string, FactSource>();
            mFactSourceProperties = new Dictionary<string, FactPropertiesAttribute>();
        }

        public FactSourceContainer Register<T>() where T : FactSource
        {
            Type type = typeof(T);
            FactPropertiesAttribute attr = type.GetCustomAttributes(
                typeof(FactPropertiesAttribute),
                true).FirstOrDefault() as FactPropertiesAttribute;

            if (attr == null)
            {
                throw new Exception(
                    $"Could not find a FactPropertiesAttribute for type {type}");
            }

            if (mFactSources.ContainsKey(attr.FactId))
            {
                throw new Exception($"Duplicate FactId ${attr.FactId}");
            }

            mFactSources.Add(attr.FactId, type);
            mFactSourceProperties.Add(attr.FactId, attr);

            if (attr.ActivationPolicy == FactSourceActivationPolicy.OnEngineStartup)
            {
                FactSource factSource = ActivateSource<FactSource>(type);
                mActivatedFactSources.Add(attr.FactId, factSource);
            }

            return this;
        }

        public async Task<Fact<T>> RequestFact<T>(string factId)
        {
            FactSource<T> source = GetFactSource<T>(
                factId,
                out FactPropertiesAttribute properties);

            DateTime generatedOn = mEngineClock.Now();
            Result<T> fact = await source.GetFact();

            var result = new Fact<T>
            {
                Data = fact.Data,
                FactId = properties.FactId,
                Name = properties.Name,
                Description = properties.Description,
                GeneratedOn = generatedOn,
                ValidUntil = Add(generatedOn, fact.TimeToLive)
            };

            return result;
        }

        FactSource<T> GetFactSource<T>(
            string factId, out FactPropertiesAttribute properties)
        {
            if (mActivatedFactSources.TryGetValue(factId, out FactSource factSource))
            {
                properties = mFactSourceProperties[factId];
                return factSource as FactSource<T>;
            }

            if (!mFactSources.TryGetValue(factId, out Type factSourceType))
            {
                throw new Exception(
                    $"There is no registered FactSource that provides '{factId}'");
            }

            FactSource<T> instance = ActivateSource<FactSource<T>>(factSourceType); 

            properties = mFactSourceProperties[factId];
            if (properties.ActivationPolicy == FactSourceActivationPolicy.JustOnce)
            {
                mActivatedFactSources.Add(factId, instance);
            }

            return instance;
        }

        T ActivateSource<T>(Type t) where T : class
        {
            T result = Activator.CreateInstance(t) as T;
            if (result != null)
                return result;

            throw new Exception(
                $"Could not activate {t} as {typeof(T)}");
        }

        static DateTime Add(DateTime dateTime, TimeSpan timeSpan)
        {
            try
            {
                return dateTime +timeSpan;
            }
            catch
            {
                return DateTime.MaxValue;
            }
        }

        readonly IEngineClock mEngineClock;
        readonly Dictionary<string, Type> mFactSources;
        readonly Dictionary<string, FactSource> mActivatedFactSources;
        readonly Dictionary<string, FactPropertiesAttribute> mFactSourceProperties;
    }
}
