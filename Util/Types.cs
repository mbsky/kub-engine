using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections.Generic;

public class Types
{
    public static object[] GetParamsForStage(XmlNode stage, string[] allParams, ref int usedParams, HttpContext context)
    {
        List<object> result = new List<object>();

        foreach (XmlNode param in stage.ChildNodes)
        {
            if (Util.GetAttr(param, "output", false)) continue;

            string value = null;

            if (param.Attributes["value"] != null) value = param.Attributes["value"].Value;
            else if (param.Attributes["input_index"] != null) value = allParams[int.Parse(param.Attributes["input_index"].Value)];
            else if (param.Attributes["postparams"] != null) value = GetPostedValue( param, context );
            else value = allParams[usedParams++];

            result.Add(Types.Convert(value, param));
        }

        return result.ToArray();
    }

    private  delegate object Convertion(string value, XmlNode desc );

    public static object Convert(string value, XmlNode typeDesc)
    {
        string typeName = typeDesc.Attributes["type"].Value;
        Convertion convertion = null;

        if (converts.TryGetValue(typeName, out convertion)) return convertion(value, typeDesc);
        else throw new Exception("Unknowed type: " + typeName);
    }

    public static void WriteOutput(object data, Stream output, bool closeStreams )
    {
        if (data == output) return;

        XmlDocument xml = data as XmlDocument;
        if (xml != null)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(output, System.Text.Encoding.UTF8);
            xml.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }

        Stream stream = data as Stream;
        if (stream != null)
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[0x5000];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, read);

            if ( closeStreams ) stream.Close();
        }

        byte[] block = data as byte[];
        if ( block != null && block.Length > 0 )
            output.Write( block, 0, block.Length );
    }

    // 

    private static void ReadAllBytesFromStream(Stream stream, byte[] buffer)
    {
        int offset = 0;
        int read = 0;
        while ((read = stream.Read(buffer, offset, buffer.Length - offset)) > 0) offset += read;
    }

    private static Dictionary<string, Convertion> converts = Util.MakeDir<string, Convertion>(
        "datetime", (Convertion)delegate(string value, XmlNode typeDesc)
            {
                return DateTime.Parse(HttpContext.Current.Server.UrlDecode(value),
                                   new System.Globalization.CultureInfo("fr-FR", false));
            },
        "bool", (Convertion)delegate(string value, XmlNode typeDesc)
            {
                return value == "true";
            },
        "int", (Convertion)delegate(string value, XmlNode typeDesc)
            {
                int vl = int.Parse(value);

                if (typeDesc.Attributes["min"] != null)
                    if (vl < int.Parse(typeDesc.Attributes["min"].Value)) throw new Exception("Int range min exception");

                if (typeDesc.Attributes["max"] != null)
                    if (vl > int.Parse(typeDesc.Attributes["max"].Value)) throw new Exception("Int range max exception");

                return int.Parse(value);
            },
        "float", (Convertion)delegate(string value, XmlNode typeDesc)
            {
                return float.Parse(value);
            },
        "string", (Convertion)delegate(string value, XmlNode typeDesc)
            {
                value = HttpContext.Current.Server.UrlDecode(value);

                if ((typeDesc.Attributes["allowed"] == null &&
                       typeDesc.Attributes["allowed_regex"] == null &&
                       typeDesc.Attributes["value"] == null) && Util.GetAttr(typeDesc, "inplace", "false") == "true")
                    throw new Exception("Security break: string param can have sql injection!");

                if (typeDesc.Attributes["allowed"] != null)
                {
                    string al = typeDesc.Attributes["allowed"].Value;
                    string[] values = al.Split(',');

                    foreach (string vl in values)
                        if (vl == value) return value;

                    throw new Exception(String.Format("Allowed break exception, you can use only: {0}", al));
                }

                if (typeDesc.Attributes["allowed_regex"] != null)
                {
                    string al = typeDesc.Attributes["allowed_regex"].Value;

                    Match match = Regex.Match(value, al);

                    if (match.Success && match.Index == 0 && match.Length == value.Length) return value;

                    Log.Error("sql injection try detected", value, HttpContext.Current);

                    throw new Exception("Allowed_regex break exception");
                }

                return value;
            });

    private static string GetPostedValue(XmlNode param, HttpContext context)
    {
        string value = null;
        string nm = param.Attributes["postparams"].Value;
        if (nm.Length == 0) nm = param.Name;

        if (param.Attributes["base64"] != null)
        {
            string b64 = param.Attributes["base64"].Value;
            byte[] data = System.Convert.FromBase64String(context.Request.Params[nm].Replace(' ', '+'));

            if (b64 == "gzip")
            {
                MemoryStream input = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(input);

                int comp = reader.ReadInt32();
                int origin = reader.ReadInt32();

                byte[] rs = new byte[origin];

                if (comp < origin)
                {
                    GZipStream zipStream = new GZipStream(input, CompressionMode.Decompress);
                    ReadAllBytesFromStream(zipStream, rs);
                    zipStream.Close();
                }
                else ReadAllBytesFromStream(input, rs);

                input.Close();
                data = rs;
            }
            value = System.Text.Encoding.UTF8.GetString(data);
        }
        else value = context.Request.Params[nm];
        
        return value;
    }
}
