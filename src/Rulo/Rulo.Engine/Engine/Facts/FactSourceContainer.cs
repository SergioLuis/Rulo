using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Rulo.Engine.Facts;
using Rulo.Engine.Facts.Attributes;

namespace Rulo.Engine.Engine.Facts
{
    public class FactSourceContainer
    {
        public FactSourceContainer()
        {
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

        public async Task<Fact> RequestNonGenericFact(string factId)
        {
            FactSource source = GetNonGenericFactSource(
                factId,
                out FactPropertiesAttribute properties);

            DateTime generatedOn = EngineClock.Default.Now();
            Result result = await source.GetFact();

            Type resultType = result.GetType();
            Type resultDataType = resultType.GetGenericArguments()[0];
            Type factType = typeof(Fact<>).MakeGenericType(resultDataType);

            Fact fact = ActivateFact(factType);
            fact.FactId = properties.FactId;
            fact.Name = properties.Name;
            fact.Description = properties.Description;
            fact.GeneratedOn = generatedOn;
            fact.ValidUntil = Add(generatedOn, result.TimeToLive);

            PropertyInfo resultDataProperty = resultType.GetProperty("Data");
            PropertyInfo factDataProperty = factType.GetProperty("Data");

            factDataProperty.SetValue(
                fact,
                resultDataProperty.GetValue(result));

            return fact;
        }

        FactSource GetNonGenericFactSource(
            string factId, out FactPropertiesAttribute properties)
        {
            if (mActivatedFactSources.TryGetValue(factId, out FactSource factSource))
            {
                properties = mFactSourceProperties[factId];
                return factSource;
            }

            if (!mFactSources.TryGetValue(factId, out Type factSourceType))
            {
                throw new Exception(
                    $"There is no registered FactSource that provides '{factId}'");
            }

            FactSource instance = ActivateNonGenericSource(factSourceType);

            properties = mFactSourceProperties[factId];
            if (properties.ActivationPolicy == FactSourceActivationPolicy.JustOnce)
                mActivatedFactSources.Add(factId, instance);

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

        FactSource ActivateNonGenericSource(Type t)
        {
            FactSource result = Activator.CreateInstance(t) as FactSource;
            if (result != null)
                return result;

            throw new Exception(
                $"Could not activate {t} as FactSource.");
        }

        Fact ActivateFact(Type t)
        {
            Fact result = Activator.CreateInstance(t) as Fact;
            if (result != null)
                return result;

            throw new Exception($"Could not activate {t} as Fact");
        }

        static DateTime Add(DateTime dateTime, TimeSpan timeSpan)
        {
            try
            {
                return dateTime + timeSpan;
            }
            catch
            {
                return DateTime.MaxValue;
            }
        }

        readonly Dictionary<string, Type> mFactSources;
        readonly Dictionary<string, FactSource> mActivatedFactSources;
        readonly Dictionary<string, FactPropertiesAttribute> mFactSourceProperties;
    }
}
