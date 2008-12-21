using System;
using System.Data;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Xml;
using System.IO;

public delegate object StageAction(StageParams config);

public class StageParams
{
    public StageParams(object data_, XmlNode stage_, object[] allParams_, HttpContext context_, string mapping, bool last_, Stream output )
    {
        data = data_; stage = stage_; allParams = allParams_; context = context_; last = last_;
        map = mapping;
        outputStream = output;
    }

    public object      data;        // input data to stage ( may be null )
    public XmlNode     stage;       // xml description
    public string      map;         // current mapping
    public object[]    allParams;   // parameters
    public HttpContext context;     // context ( for server access )   
    public bool        last;        // last if stage is last ( so output directly to context ) - cache issue?!
    public Stream      outputStream;

    public static object Dispatch(StageParams config, Dictionary<string, StageAction> mapping)
    {
        string[] subMapping = config.map.Split(new char[] { ':' }, 2);
        
        config.map = subMapping[1];
        
        string key = subMapping[0];
        
        if (mapping.ContainsKey(key)) return mapping[key](config);
        
        throw new Exception("Unknow mapping key: " + key);
    }
}

public class StageProcessor
{
    public static Dictionary<string, StageAction> actions = Util.MakeDir<string, StageAction>(
            "sql", (StageAction)delegate(StageParams config) { return StageParams.Dispatch(config, sqlActions); },
            "xslt", (StageAction)XSLTFilter.GetData,
            "file", (StageAction)FileSource.GetData,
            "code", (StageAction)CodeFilters.GetData );

    public static Dictionary<string, StageAction> sqlActions = Util.MakeDir<string, StageAction>(
            "mysql", (StageAction)MysqlSource.GetData,
            "mssql", (null) );
}
