using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Zigfu.Speech
{
    public class LanguagePack
    {

        #region enum Dialect

        public enum DialectEnum
        {
            English_United_States,
            English_Great_Britain,
            English_Ireland,
            English_Australia,
            English_New_Zealand,
            English_Canada,

            French_France,
            French_Canada,
            German_Germany,
            Italian_Italy,
            Japanese_Japan,
            Spanish_Spain,
            Spanish_Mexico
        }

        public const DialectEnum DefaultDialect = DialectEnum.English_United_States;

        // AllDialects
        static List<DialectEnum> _allDialects = new List<DialectEnum>
        { 
            DialectEnum.English_United_States,
            DialectEnum.English_Great_Britain, 
            DialectEnum.English_Ireland, 
            DialectEnum.English_Australia, 
            DialectEnum.English_New_Zealand, 
            DialectEnum.English_Canada, 

            DialectEnum.French_France, 
            DialectEnum.French_Canada, 
            DialectEnum.German_Germany, 
            DialectEnum.Italian_Italy, 
            DialectEnum.Japanese_Japan, 
            DialectEnum.Spanish_Spain, 
            DialectEnum.Spanish_Mexico
        };
        static public List<DialectEnum> AllDialects { get { return _allDialects; } }

        // LanguagePackForDialect
        static Dictionary<DialectEnum, LanguagePack> _languagePackForDialect = new Dictionary<DialectEnum, LanguagePack>()
        {
		    { DialectEnum.English_United_States, 
                new LanguagePack(DialectEnum.English_United_States, "en-US", "409") },
            { DialectEnum.English_Great_Britain, 
                new LanguagePack(DialectEnum.English_Great_Britain, "en-GB", "809") },
            { DialectEnum.English_Ireland, 
                new LanguagePack(DialectEnum.English_Ireland,       "en-IE", "1809") },
            { DialectEnum.English_Australia, 
                new LanguagePack(DialectEnum.English_Australia,     "en-AU", "c09") },
            { DialectEnum.English_New_Zealand, 
                new LanguagePack(DialectEnum.English_New_Zealand,   "en-NZ", "1409") },
            { DialectEnum.English_Canada, 
                new LanguagePack(DialectEnum.English_Canada,        "en-CA", "1009") },

            { DialectEnum.French_France, 
                new LanguagePack(DialectEnum.French_France,         "fr-FR", "40c") },
            { DialectEnum.French_Canada, 
                new LanguagePack(DialectEnum.French_Canada,         "fr-CA", "c0c") },
            { DialectEnum.German_Germany, 
                new LanguagePack(DialectEnum.German_Germany,        "de-DE", "407") },
            { DialectEnum.Italian_Italy, 
                new LanguagePack(DialectEnum.Italian_Italy,         "it-IT", "410") },

            { DialectEnum.Japanese_Japan, 
                new LanguagePack(DialectEnum.Japanese_Japan,        "ja-JP", "411") },
            { DialectEnum.Spanish_Spain, 
                new LanguagePack(DialectEnum.Spanish_Spain,         "es-ES", "440a") },
            { DialectEnum.Spanish_Mexico, 
                new LanguagePack(DialectEnum.Spanish_Mexico,        "es-MX", "80a") },
	    };
        static public LanguagePack LanguagePackForDialect(DialectEnum dialect)
        {
            return _languagePackForDialect[dialect];
        }

        static public bool TryGetDialectForGrxmlLangName(string grxmlLangName, out DialectEnum dialect)
        {
            var temp =
                from d in _allDialects
                where LanguagePackForDialect(d).GrxmlLangName == grxmlLangName
                select d;
            dialect = temp.SingleOrDefault();
            return temp.Count() > 0;
        }

        #endregion


        public DialectEnum Dialect     { get; private set; }       // ie DialectEnum.English_United_States
        public string GrxmlLangName    { get; private set; }       // ie "en-US"
        public string Code             { get; private set; }       // ie "409"

        public string DialectName      { get { return Dialect.ToString(); } }


        private LanguagePack(DialectEnum dialect, string grxmlLangName, string code)
        {
            Dialect = dialect;
            GrxmlLangName = grxmlLangName;
            Code = code;
        }

    }
}