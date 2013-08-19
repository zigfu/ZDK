using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Zigfu.Speech
{
    class SpeechGrammarValidator
    {

        // The following validation methods throw an Exception if the validation fails.

        public static void ValidateSpeechGrammar(ZigSpeechGrammar spGrammar)
        {
            if (spGrammar == null)
                { throw new ArgumentNullException("spGrammar"); }

            ValidateGrammarName(spGrammar.GrammarName);


            String grammarTextPrefix = "The SpeechGrammar, \"" + spGrammar.GrammarName + "\",";

            List<Phrase> phrases = spGrammar.Phrases;
            if (phrases == null)
                { throw new NullReferenceException(grammarTextPrefix + " has a null Phrases array."); }
            if (phrases.Count <= 0)
                { throw new Exception(grammarTextPrefix + " doesn't contain any Phrases.  It must contain at least one Phrase."); }


            foreach (var phrase in phrases)
            {
                ValidatePhrase(phrase, spGrammar);
            }
        }

        public static void ValidateGrammarName(String grammarName)
        {
            String grammarTextPrefix = "The SpeechGrammar, \"" + grammarName + "\",";

            if (String.IsNullOrEmpty(grammarName))
                { throw new NullReferenceException("A SpeechGrammar exists that has not been assigned a Name."); }
            if (!StringContainsLettersOnly(grammarName))
                { throw new NullReferenceException(grammarTextPrefix + " has invalid characters in its grammar name.  SpeechGrammar Names must be one word, and contain only letters."); }
        }

        public static void ValidatePhrase(Phrase phrase, ZigSpeechGrammar spGrammar)
        {
            String grammarTextPrefix = "The SpeechGrammar, \"" + spGrammar.GrammarName + "\",";

            if (phrase == null)
                { throw new NullReferenceException(grammarTextPrefix + " contains a null Phrase."); }

            ValidateSemanticTag(phrase.SemanticTag, spGrammar);

            ValidatePhraseSynonyms(phrase.Synonyms, phrase, spGrammar);
        }

        public static void ValidateSemanticTag(String semanticTag, ZigSpeechGrammar spGrammar)
        {
            String grammarTextPrefix = "Within the SpeechGrammar, \"" + spGrammar.GrammarName + "\",";
            String tagTextPrefix = " the SemanticTag, \"" + semanticTag + "\",";

            if (semanticTag == null)
                { throw new NullReferenceException(grammarTextPrefix + " one or more Phrases contains a null SemanticTag."); }
            if (semanticTag == String.Empty)
                { throw new Exception(grammarTextPrefix + " one or more Phrases has an empty SemanticTag (equal to String.Empty). Every Phrase must be assigned a unique, non-empty SemanticTag."); }
            if (SemanticTagAppearsMoreThanOnceInSpeechGrammar(semanticTag, spGrammar))
                { throw new Exception(grammarTextPrefix + tagTextPrefix + " appears more than once.  Every Phrase must have its own unique SemanticTag."); }
        }

        public static void ValidatePhraseSynonyms(List<String> synonyms, Phrase phrase, ZigSpeechGrammar spGrammar)
        {
            String grammarTextPrefix = "Within the SpeechGrammar, \"" + spGrammar.GrammarName + "\",";
            String phraseTextPrefix = " the Phrase, \"" + phrase.SemanticTag + "\",";

            if (synonyms == null)
                { throw new NullReferenceException(grammarTextPrefix + phraseTextPrefix + " contains a null Synonyms List."); }
            if (synonyms.Count <= 0)
                { throw new Exception(grammarTextPrefix + phraseTextPrefix + " doesn't contain any Synonyms.  Every Phrase must contain at least one Synonym."); }
        
            foreach (String syn in synonyms)
            {
                ValidateSynonym(syn, phrase, spGrammar);
            }
        }

        public static void ValidateSynonym(String synonym, Phrase phrase, ZigSpeechGrammar spGrammar)
        {
            String grammarTextPrefix = "Within the SpeechGrammar, \"" + spGrammar.GrammarName + "\",";
            String phraseTextPrefix = " the Phrase, \"" + phrase.SemanticTag + "\",";
            String synonymTextPrefix = " the Synonym, \"" + synonym + "\",";
            String combinedTextPrefix = grammarTextPrefix + " and" + phraseTextPrefix + synonymTextPrefix;

            if (synonym == null)
                { throw new NullReferenceException(grammarTextPrefix + phraseTextPrefix + " contains a null Synonym."); }
            if (synonym == String.Empty)
                { throw new Exception(grammarTextPrefix + phraseTextPrefix + " contains an empty Synonym (equal to String.Empty).  Every Synonym must be assigned a unique, non-empty string value."); }
            if (!StringContainsLettersAndWhitespaceOnly(synonym))
                { throw new Exception(combinedTextPrefix + " contains one more more invalid characters.  The only characters a Synonym may contain are letters of the alphabet and whitespace."); }
            if (!StringContainsAtLeastOneLetter(synonym))
                { throw new Exception(combinedTextPrefix + " doesn't contain any letters.  Every Synonym must contain at least one letter."); }
            if (SynonymAppearsMoreThanOnceInSpeechGrammar(synonym, spGrammar))
                { throw new Exception(grammarTextPrefix + synonymTextPrefix + " appears more than once.  Every Synonym must be unique within its SpeechGrammar."); }
        }


        public static bool SemanticTagAppearsMoreThanOnceInSpeechGrammar(String phraseTag, ZigSpeechGrammar speechGrammar)
        {
            int matchCount = 0;
            foreach (Phrase phrase in speechGrammar.Phrases)
            {
                if (phrase.SemanticTag != phraseTag) { continue; }
                if (++matchCount > 1) { return true; }
            }
            return false;
        }

        public static bool SynonymAppearsMoreThanOnceInSpeechGrammar(String synonym, ZigSpeechGrammar speechGrammar)
        {
            int matchCount = 0;
            foreach (Phrase phrase in speechGrammar.Phrases)
            {
                foreach (String syn in phrase.Synonyms)
                {
                    if (syn != synonym) { continue; }
                    if (++matchCount > 1) { return true; }
                }
            }
            return false;
        }

        public static bool StringContainsLettersOnly(String input)
        {
            return !Regex.IsMatch(input, @"[^A-Za-z]");
        }

        public static bool StringContainsLettersAndWhitespaceOnly(String input)
        {
            return !Regex.IsMatch(input, @"[^A-Za-z\s]");
        }

        public static bool StringContainsAtLeastOneLetter(String input)
        {
            return Regex.IsMatch(input, @"[A-Za-z]");
        }

    }
}