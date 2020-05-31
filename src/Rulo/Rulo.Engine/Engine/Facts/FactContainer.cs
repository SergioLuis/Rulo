using System;
using System.Collections.Generic;

namespace Rulo.Engine.Facts
{
    public class FactContainer
    {
        public FactContainer()
        {
            mFacts = new Dictionary<string, Fact>();
        }

        public void AddFactRange(IEnumerable<Fact> facts)
        {
            foreach (Fact fact in facts)
                AddFact(fact);
        }

        public void AddFact(Fact fact)
        {
            if (!mFacts.TryGetValue(fact.FactId, out Fact oldFact))
            {
                mFacts.Add(fact.FactId, fact);
                return;
            }

            if (oldFact != null)
                throw new Exception($"Duplicated fact '{fact.FactId}' in engine!");

            mFacts[fact.FactId] = fact;
        }

        public bool IsCached(string factId, DateTime now)
        {
            if (!mFacts.TryGetValue(factId, out Fact fact))
                return false;

            if (fact.ValidUntil >= now)
                return true;

            mFacts[factId] = null;
            return false;
        }

        public Fact<T> PullFact<T>(string factId)
        {
            if (!mFacts.TryGetValue(factId, out Fact fact))
                return null;

            Fact<T> result = fact as Fact<T>;
            if (result == null)
            {
                throw new Exception(
                    $"Fact type missmatch. Expected {typeof(T)} but got {fact.GetType()}.");
            }

            return result;
        }

        public Fact PullFact(string factId)
        {
            if (mFacts.ContainsKey(factId))
                return mFacts[factId];

            return null;
        }

        readonly Dictionary<string, Fact> mFacts;
    }
}
