using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

using MySql.Data;
using MySql.Data.MySqlClient;

public class MysqlSource
{
    public static readonly string[] validCodeTypes = new string[]{ "code", "storeprocedure" };
    public static readonly string[] validResultTypes = new string[]{ "table", "list", "scalar", "nonquery" };

    private static XmlNode settings;

	public static object GetData( StageParams config )
	{
        if (settings == null) settings = Util.GetByName(Engine.GetConfig(), "mysql");

        using (MySql.Data.MySqlClient.MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection())
        {
            conn.ConnectionString = settings.Attributes["connectionString"].Value;
            conn.Open();

            using (MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand())
            {
                cmd.Connection = conn;

                object result = null;

                /*
                    mysql map details
                    [code/storeprocedure]:[table/scalar/nonquery]:[text/procedure name]
                */

                string[] resultMap = config.map.Split(new char[] { ':' }, 3);

                string ct = resultMap[0]; // call type
                string rt = resultMap[1]; // return type
                string mp = resultMap[2]; // mapping ( sql text or store procedure name )

                if (!Validate(ct, validCodeTypes)) throw new Exception("Mysql bad call type: " + ct);
                if (!Validate(rt, validResultTypes)) throw new Exception("Mysql bad result type: " + rt);

                // command text
                cmd.CommandText = mp;

                // command type     
                if (ct == "code")
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Prepare();
                }
                else cmd.CommandType = CommandType.StoredProcedure;

                List<string> output_params = new List<string>();

                // named parameters
                for (int i = 0; i < config.stage.ChildNodes.Count; ++i)
                {
                    XmlNode paramNode = config.stage.ChildNodes[i];
                    if (!Util.GetAttr(paramNode, "inplace", false))
                        if (Util.GetAttr(paramNode, "output", false))
                        {
                            cmd.Parameters.Add(paramNode.Name, Convert(paramNode.Attributes["type"].Value));
                            cmd.Parameters[paramNode.Name].Direction = ParameterDirection.Output;
                            output_params.Add(paramNode.Name);
                        }
                        else Add(cmd, "@" + paramNode.Name, config.allParams[i]);
                }

                if (rt == "table")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.AppendChild(doc.CreateNode(XmlNodeType.Element, "Root", ""));

                    MySqlDataReader reader = cmd.ExecuteReader();
                    DataToXML.Write(doc, reader);
                    reader.Close();

                    if (cmd.CommandText.StartsWith("select SQL_CALC_FOUND_ROWS"))
                    {
                        cmd.CommandText = "SELECT FOUND_ROWS()";
                        cmd.Parameters.Clear();
                        reader = cmd.ExecuteReader();
                        reader.Read();
                        doc.FirstChild.Attributes.Append(doc.CreateAttribute("found_rows")).Value = reader.GetInt32(0).ToString();
                        reader.Close();
                    }

                    result = doc;
                }
                else if (rt == "list")
                {
                    MySqlDataReader reader = cmd.ExecuteReader();
                    MemoryStream output = new MemoryStream();
                    DataToList.Write(output, reader);
                    reader.Close();

                    result = output;
                }
                else if (rt == "scalar")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.AppendChild(doc.CreateNode(XmlNodeType.Element, "Root", ""));

                    object callresult = cmd.ExecuteScalar();
                    doc.FirstChild.AppendChild(doc.CreateNode(XmlNodeType.Text, "", ""));
                    doc.FirstChild.FirstChild.Value = callresult.ToString();

                    result = doc;
                }
                else if (rt == "nonquery")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.AppendChild(doc.CreateNode(XmlNodeType.Element, "Root", ""));

                    int affected = cmd.ExecuteNonQuery();
                    doc.FirstChild.Attributes.Append(doc.CreateAttribute("affected")).Value = affected.ToString();

                    result = doc;
                }

                foreach (string param in output_params)
                {
                    XmlDocument doc = Util.Validate<XmlDocument>(result, "Output params for this request not supported");
                    doc.FirstChild.Attributes.Append(
                        doc.CreateAttribute(param)).Value = cmd.Parameters[param].Value.ToString();
                }

                return result;
            }
        }
	}

    private static bool Validate(string value, string[] types)
    {
        foreach (string st in types) if (st == value) return true;
        return false;
    }

    public static void Add(MySqlCommand cmd, string name, object value)
    {
        if (value is string)
        {
            cmd.Parameters.AddWithValue(name, Encoding.UTF8.GetBytes((string)value));
            cmd.Parameters[name].Direction = ParameterDirection.Input;
        }
        else
        {
            if (value != null)
            {
                cmd.Parameters.AddWithValue(name, value);
                cmd.Parameters[name].Direction = ParameterDirection.Input;
            }
            else
            {
                cmd.Parameters.AddWithValue(name, "");
                cmd.Parameters[name].Direction = ParameterDirection.Input;
            }
        }
    }

    private static MySqlDbType Convert(string typename)
    {
        MySqlDbType result = MySqlDbType.Int32;
        if (converts.TryGetValue(typename, out result)) return result;
        else new Exception(String.Format("Output parameter type {0} not supported", typename));
        return result;
    }

    private static Dictionary<string, MySqlDbType> converts = Util.MakeDir<string, MySqlDbType>(
           "datetime", MySqlDbType.DateTime,
           "int", MySqlDbType.Int32,
           "float", MySqlDbType.Double,
           "string", MySqlDbType.Text);
}
