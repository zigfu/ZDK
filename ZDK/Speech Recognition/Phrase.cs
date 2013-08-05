using UnityEngine;
using System;
using System.Collections.Generic;


namespace Zigfu.Speech
{
    public class Phrase : ScriptableObject
    {
        [SerializeField]
        String _semanticTag = String.Empty;
        public String SemanticTag { get { return _semanticTag; } set { _semanticTag = value; } }

        [SerializeField]
        List<String> _synonyms = new List<String>();
        public List<String> Synonyms { get { return _synonyms; } }


        #region Factory Init Methods

        public static Phrase CreatePhrase()
        {
            return CreatePhrase(String.Empty);
        }
        public static Phrase CreatePhrase(Phrase phraseToCopy)
        {
            if (!phraseToCopy) { return CreatePhrase(); }

            String[] synonyms = phraseToCopy.Synonyms.ToArray();
            return CreatePhrase(phraseToCopy.SemanticTag, synonyms);
        }
        public static Phrase CreatePhrase(String semanticTag, params String[] synonyms)
        {
            Phrase newPhrase = ScriptableObject.CreateInstance<Phrase>();
            newPhrase.SemanticTag = (semanticTag == null) ? String.Empty : semanticTag;
            newPhrase.Synonyms.AddRange(synonyms);

            return newPhrase;
        }

        #endregion


        public void AddSynonym(String newSynonym)
        {
            if (!_synonyms.Contains(newSynonym))
            {
                _synonyms.Add(newSynonym);
            }
        }

        public void RemoveSynonym(String synonymToRemove)
        {
            _synonyms.Remove(synonymToRemove);
        }


        #region Utility

        override public string ToString()
        {
            string outStr = "(" + _semanticTag + ": ";

            int numSynonyms = _synonyms.Count;
            for (int i = 0; i < numSynonyms; i++)
            {
                outStr += _synonyms[i];
                if (i < numSynonyms - 1) 
                {
                    outStr += ", ";
                }
            }

            outStr += ")";
            return outStr;
        }

        #endregion

    }
}