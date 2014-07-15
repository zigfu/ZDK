using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Runtime.InteropServices;


namespace Zigfu.Speech
{
    // Summary:
    //      Responsible for creating/removing and activating/deactivating
    //       the native C Grammar that is linked to this ZigSpeechGrammar through the nativeID
    //
    public interface IZigSpeechGrammarDelegate
    {
        bool RegisterGrammar(ZigSpeechGrammar gr, out UInt32 nativeID);
        bool UnregisterGrammar(ZigSpeechGrammar gr);
        bool ActivateGrammar(ZigSpeechGrammar gr);
        bool DeactivateGrammar(ZigSpeechGrammar gr);
    }

    
    public class ZigSpeechGrammar : MonoBehaviour
    {
        const String ClassName = "ZigSpeechGrammar";
        const String DefaultGrammarName = "Unnamed";

        public const UInt32 InvalidNativeID = UInt32.MaxValue;

        static public bool verbose = false;


        [SerializeField]
        String _grammarName = DefaultGrammarName;
        public String GrammarName { get { return _grammarName; } }

        [SerializeField]
        List<Phrase> _phrases = new List<Phrase>();
        public List<Phrase> Phrases { get { return _phrases; } }

        LanguagePack.DialectEnum _dialect = LanguagePack.DefaultDialect;
        public LanguagePack.DialectEnum Dialect
        {
            get { return _dialect; }
            set { _dialect = value; }
        }

        public UInt32 NativeID { get; private set; } 
        bool _hasRegistered = false;
        public bool HasRegistered { get { return _hasRegistered; } }

        public bool WantsActive { 
            get         { return enabled; } 
            private set { enabled = value; }
        }


        ZigKinectSpeechRecognizer SpeechRecognizer { get { return ZigKinectSpeechRecognizer.Instance; } }

        IZigSpeechGrammarDelegate _grammarDelegate;


        #region Factory Init Methods

        public static ZigSpeechGrammar CreateGrammar()
        {
            return CreateGrammar(String.Empty);
        }
        public static ZigSpeechGrammar CreateGrammar(String grammarXmlFilePath)
        {
            GameObject go = new GameObject();
            ZigSpeechGrammar newGrammar = go.AddComponent<ZigSpeechGrammar>();

            if (!String.IsNullOrEmpty(grammarXmlFilePath))
            {
                newGrammar.InitializeFromXmlFile(grammarXmlFilePath);
            }

            go.name = newGrammar.GrammarName + " Grammar";

            return newGrammar;
        }
        public static ZigSpeechGrammar CreateGrammar(string name, List<Phrase> phrases)
        {
            GameObject go = new GameObject();
            ZigSpeechGrammar newGrammar = go.AddComponent<ZigSpeechGrammar>();

            if (!String.IsNullOrEmpty(name)) { newGrammar._grammarName    = name; }
            if (phrases != null)             { newGrammar._phrases = phrases; }

            go.name = newGrammar.GrammarName + " Grammar";

            return newGrammar;
        }

        #endregion


        #region Init and Destroy

        private ZigSpeechGrammar() { }

        void Start() { }

        void OnEnable()  { Activate(); }
        void OnDisable() { Deactivate(); }

        void OnDestroy()
        {
            if (verbose) { print(ClassName + " :: OnDestroy"); }

            Unregister();
        }

        #endregion


        #region Native Registration and Activation

        public void Register(IZigSpeechGrammarDelegate del)
        {
            if (_hasRegistered) { return; }
            if (del == null) { return; }
            if (verbose) { print(ClassName + " :: Register"); }

            _grammarDelegate = del;

            UInt32 nativeID;
            if (_grammarDelegate.RegisterGrammar(this, out nativeID))
            { 
                NativeID = nativeID;
                _hasRegistered = true;
            }
        }

        public void Unregister()
        {
            if (!_hasRegistered) { return; }
            if (_grammarDelegate == null) { return; }
            if (verbose) { print(ClassName + " :: Unregister"); }

            if (_grammarDelegate.UnregisterGrammar(this))
            {
                NativeID = InvalidNativeID;
                _hasRegistered = false;
                _grammarDelegate = null;
            }
        }


        public void Activate()
        {
            if (verbose) { print(ClassName + " :: Activate"); }

            WantsActive = true;
            if (!HasRegistered) { return; }
            _grammarDelegate.ActivateGrammar(this);
        }

        public void Deactivate()
        {
            if (verbose) { print(ClassName + " :: Deactivate"); }

            WantsActive = false;
            if (!HasRegistered) { return; }
            _grammarDelegate.DeactivateGrammar(this);
        }

        #endregion


        #region GrammarXML Conversion

        public void InitializeFromXmlFile(String path)
        {
            String grammarName = String.Empty;
            String language = String.Empty;
            List<Phrase> phrases = new List<Phrase>();
        
            XmlReader reader = XmlReader.Create(path);

            while (reader.Read())
            {
                if (reader.IsStartElement("grammar"))
                {
                    if (reader.MoveToAttribute("xml:lang"))
                    {
                        language = reader.GetAttribute("xml:lang");             /// language
                    }
                    if (reader.MoveToAttribute("root"))
                    {
                        grammarName = reader.GetAttribute("root");              /// grammarName
                    }
                }
                else if (reader.IsStartElement("item"))
                {
                    Phrase phrase = Phrase.CreatePhrase();                      /// phrase

                    while (reader.Read())
                    {
                        if (reader.IsStartElement("tag"))
                        {
                            phrase.SemanticTag = reader.ReadString();           /// semanticTag
                        }
                        else if (reader.IsStartElement("one-of"))
                        {
                            while (reader.Read())
                            {
                                if (reader.IsStartElement("item"))
                                {
                                    String synonym = reader.ReadString();       /// synonym
                                    phrase.AddSynonym(synonym);
                                }
                                else { break; }    //  </one-of>
                            }
                        }
                        else { break; }    //  </item>
                    }

                    phrases.Add(phrase);
                }
            }

            LanguagePack.DialectEnum dialect;
            if (LanguagePack.TryGetDialectForGrxmlLangName(language, out dialect))
            {
                _dialect = dialect;
            }
            _grammarName = grammarName;
            _phrases = phrases;
        }

        public XmlDocument ToXml()
        {
            PurgeAllPlaceholderSynonyms();

            if (!TryValidateSpeechGrammar(this)) { return null; }


            XmlDocument doc = new XmlDocument();

            String lang = LanguagePack.LanguagePackForDialect(_dialect).GrxmlLangName;
            String rootRuleName = _grammarName;

            XmlElement el = (XmlElement)doc.AppendChild(doc.CreateElement("grammar"));
            el.SetAttribute("version", "1.0");
            el.SetAttribute("xml:lang", lang);
            el.SetAttribute("root", rootRuleName);
            el.SetAttribute("tag-format", "semantics/1.0-literals");
            el.SetAttribute("xmlns", "http://www.w3.org/2001/06/grammar");

            el = (XmlElement)el.AppendChild(doc.CreateElement("rule"));
            el.SetAttribute("id", rootRuleName);
            el = (XmlElement)el.AppendChild(doc.CreateElement("one-of"));

            foreach (Phrase phrase in _phrases)
            {
                el = (XmlElement)el.AppendChild(doc.CreateElement("item"));
                el.AppendChild(doc.CreateElement("tag")).InnerText = phrase.SemanticTag;

                if (phrase.Synonyms.Count > 0)
                {
                    el = (XmlElement)el.AppendChild(doc.CreateElement("one-of"));
                    foreach (String synonym in phrase.Synonyms)
                    {
                        el.AppendChild(doc.CreateElement("item")).InnerText = synonym;
                    }
                    el = (XmlElement)el.ParentNode;
                }
                el = (XmlElement)el.ParentNode;
            }

            return doc;
        }

        void PurgeAllPlaceholderSynonyms()
        {
            for (int i = _phrases.Count - 1; i >= 0; i--)
            {
                Phrase phrase = _phrases[i];
                if (phrase == null)
                { 
                    _phrases.RemoveAt(i); 
                    continue; 
                }


                List<String> synonyms = phrase.Synonyms;

                for (int j = synonyms.Count - 1; j >= 0; j--)
                {
                    String syn = synonyms[j];
                    if (
                        syn == null 
                        || syn == String.Empty 
                        || syn == Phrase.NewSynonymPlaceholderText)
                    {
                        synonyms.RemoveAt(j);
                        continue;
                    }
                }

                if (synonyms.Count == 0)
                {
                    _phrases.RemoveAt(i);
                    continue;
                }
            }
        }

        static bool TryValidateSpeechGrammar(ZigSpeechGrammar spGrammar)
        {
            try
            {
                SpeechGrammarValidator.ValidateSpeechGrammar(spGrammar);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid SpeechGrammar.\n" + e.Message);
                return false;
            }
            return true;
        }

        #endregion


        #region Save/Load as XML

        public bool SaveAsXml(String filePath)
        {
            XmlDocument grammarXml = ToXml();
            return SaveGrammarXmlToFile(grammarXml, filePath);
        }

        static bool SaveGrammarXmlToFile(XmlDocument grammarXml, String filePath)
        {
            try
            {
                grammarXml.Save(filePath);
            }
            catch
            {
                UnityEngine.Debug.LogError("Failed to save the grammarXml to filePath: " + filePath);
                return false;
            }
            return true;
        }

        #endregion


        #region Utility

        override public string ToString()
        {
            string outStr = "";
            outStr += "Name: " + _grammarName + "\n";
            outStr += "Dialect: " + _dialect + "\n";
            outStr += "Phrases: \n";

            foreach (var phrase in _phrases)
            {
                outStr += " " + phrase + "\n";
            }

            return outStr;
        }

        #endregion

    }
}

// GrammarXML Example for reference:
/*
    <grammar version="1.0" xml:lang="en-US" root="Directions" tag-format="semantics/1.0-literals" xmlns="http://www.w3.org/2001/06/grammar">
      <rule id="Directions">
        <one-of>
          <item>
            <tag>FORWARD</tag>
            <one-of>
              <item>forwards</item>
              <item>forward</item>
              <item>straight</item>
            </one-of>
          </item>
        </one-of>
      </rule>
    </grammar>
*/