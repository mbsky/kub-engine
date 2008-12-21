using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;

public class FileSource
{
    public static object GetData( StageParams config )
    {
        FileStream stream = new FileStream( config.context.Server.MapPath( config.map ), FileMode.Open, FileAccess.Read);
        return stream;
    }
}
