using System;
using System.Data;
using System.Data.Common;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.IO;
using System.Text;

public class DataToList
{
    public static void Write( Stream output, DbDataReader source)
    {
        /*
            Style:
            value1row1 value2row1
            value1row2 value2row2
        */
        TextWriter writer = new StreamWriter(output);

        while (source.Read())
        {
            for (int i = 0; i < source.FieldCount; ++i)
                writer.Write(source.GetString(i));
            
            writer.WriteLine();
        }

        writer.Flush();
    }
}

public class DataToXML
{
    public static void Write(XmlDocument target, DbDataReader source )
    {
        /*
            Style: 
            <root>
                <raw><name>name1</name><index>name2</index></raw>  
                <raw><name>name1</name><index>name2</index></raw>  
                <raw><name>name1</name><index>name2</index></raw>  
            </root>
        */

        XmlNode head = target.CreateNode(XmlNodeType.Element, "head", "");
        XmlNode body = target.CreateNode(XmlNodeType.Element, "body", "");

        for (int i = 0; i < source.FieldCount; ++i)
        {
            string vl = source.GetName(i);
            string local =  (string)HttpContext.GetGlobalResourceObject("local", vl);
            if (local != null) vl = local;
            
            Util.AddNodedText(head, "column", vl, false);
        }

        while (source.Read())
        {
            XmlNode raw = target.CreateNode(XmlNodeType.Element, "raw", "");

            for (int i = 0; i < source.FieldCount; ++i) Util.AddNodedText(raw, "value", Util.GetString( source, i ), false);

            body.AppendChild(raw);
        }

        target.FirstChild.AppendChild(head);
        target.FirstChild.AppendChild(body);
    }
}
