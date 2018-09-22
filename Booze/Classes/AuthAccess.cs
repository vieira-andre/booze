using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Booze.Classes
{
    public static class AuthAccess
    {
        public static Dictionary<string, string> Keys;

        static AuthAccess()
        {
            XDocument xml = XDocument.Load("authinfo.config");

            if (xml == null || xml.Root == null)
                throw new ApplicationException("Couldn't find/load auth config");

            Keys = new Dictionary<string, string>();

            foreach (XElement child in xml.Root.Elements("auth"))
            {
                XAttribute attrName = child.Attribute("name");
                XAttribute attrKey = child.Attribute("key");

                if (attrName == null || attrKey == null)
                    throw new ApplicationException("Invalid auth data");

                Keys.Add(attrName.Value, attrKey.Value);
            }
        }
    }
}
