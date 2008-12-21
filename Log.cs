
using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySql.Data;

public class Log
{
    public static void Error( Exception ex, HttpContext context )
    {
        Error( ex.Message, ex.StackTrace, context );
    }

    public static void Error(string message, string callstack, HttpContext context )
    {
        MySqlConnection c = new MySqlConnection();
        c.ConnectionString = Util.GetByName( Engine.GetConfig(), "mysql" ).Attributes["connectionString"].Value;
        c.Open();
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = c;

        cmd.CommandText = "Logweb";
        cmd.CommandType = CommandType.StoredProcedure;

        if (context != null)
        {
            MysqlSource.Add(cmd, "source", "Akuba");
            MysqlSource.Add(cmd, "message", message);
            MysqlSource.Add(cmd, "callstack", callstack);
            MysqlSource.Add(cmd, "client_ip", context.Request.UserHostAddress.ToString());
            MysqlSource.Add(cmd, "rawurl", context.Request.RawUrl);
            MysqlSource.Add(cmd, "refurl", context.Request.UrlReferrer == null ? null : context.Request.UrlReferrer.ToString());
            MysqlSource.Add(cmd, "cookie", getcookie(context.Request));
            MysqlSource.Add(cmd, "browse", context.Request.Browser == null ? null : context.Request.Browser.Browser);
            MysqlSource.Add(cmd, "method", context.Request.HttpMethod);
        }
        else
        {
            MysqlSource.Add(cmd, "source", "Akuba");
            MysqlSource.Add(cmd, "message", message);
            MysqlSource.Add(cmd, "callstack", callstack);
            MysqlSource.Add(cmd, "client_ip", "empty");
            MysqlSource.Add(cmd, "rawurl", "empty");
            MysqlSource.Add(cmd, "refurl", "empty");
            MysqlSource.Add(cmd, "cookie", "empty");
            MysqlSource.Add(cmd, "browse", "empty");
            MysqlSource.Add(cmd, "method", "empty");
        }

        cmd.ExecuteNonQuery();
        cmd.Dispose();
        c.Close();
    }

    private static bool logInit = false;
    private static bool showInfo = false;

    public static void Info(string message, HttpContext context)
    {
        if (!logInit)
        {
            showInfo = System.Configuration.ConfigurationManager.AppSettings["LogInfo"] == "true";
            logInit = true;
        }

        if (showInfo)
        {
            Error("AkubaInfo", message, context);
        }
    }

    private static string str(string vl)
    {
        return vl == null ? "null" : vl;
    }

    private static string getcookie( HttpRequest req )
    {
        StringBuilder res = new StringBuilder();
        foreach ( string key in req.Cookies.AllKeys ) res.Append( str(key) ).Append( "=" ).Append( str(req.Cookies[key].Value) ).Append( "\n" );
        return res.ToString();
    }
}
