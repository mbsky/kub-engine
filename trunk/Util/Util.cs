using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data;
using System.Data.Common;
using System.Xml;

public class Util
{
    public static bool GetAttr( XmlNode node, string name, bool def )
    {
        if (node.Attributes[name] == null) return def;
        else return node.Attributes[name].Value.ToLower() == "true";
    }

    public static string GetAttr( XmlNode node, string name, string def )
    {
        if (node.Attributes[name] == null) return def;
        else return node.Attributes[name].Value;
    }

    public static string GetString(DbDataReader source, int field)
    {
        if (source.IsDBNull(field)) return "";
        else return source.GetString(field);
    }

    public static void TransiteAttribute(XmlNode source, XmlNode dest, string name)
    {
        if ( dest.Attributes[name] == null && source.Attributes[name] != null )
            dest.Attributes.Append( dest.OwnerDocument.CreateAttribute(name ) ).Value = source.Attributes[name].Value;

    }

    public static XmlNode ChangeName(XmlNode node, string name)
    {
        XmlNode child = node.OwnerDocument.CreateNode(XmlNodeType.Element, name, "");
        XmlNode value = node.OwnerDocument.CreateNode(XmlNodeType.Text, "", "");
        value.Value = node.FirstChild.Value;
        child.AppendChild(value);

        return child;
    }

    public static void AddNodedText(XmlNode parent, string childName, string text, bool prepend)
    {
        XmlNode child = parent.OwnerDocument.CreateNode(XmlNodeType.Element, childName, "");
        XmlNode value = parent.OwnerDocument.CreateNode(XmlNodeType.Text, "", "");
        value.Value = text;
        child.AppendChild(value);

        if (prepend) parent.PrependChild(child);
        else parent.AppendChild(child);
    }

    public static XmlNode GetByName(XmlNode node, string child)
    {
        foreach (XmlNode c in node.ChildNodes)
            if (c.Name == child) return c;
        return null;
    }

    public static Dictionary<K, V> MakeDir<K,V>(params object[] kv)
    {
        Dictionary<K,V> result = new Dictionary<K,V>();
        for (int i = 0; i < kv.Length; i += 2) result.Add((K)kv[i], (V)kv[i + 1]);
        return result;
    }

    public static T Validate<T>(object data, string owner) where T: class
    {
        T ret = data as T;
        if (ret == null)
            throw new Exception( 
                String.Format("Bad input on {0}, {1} expected, got {2}", owner, typeof(T).ToString(), data.GetType().ToString()));
        return ret;
    }
}
