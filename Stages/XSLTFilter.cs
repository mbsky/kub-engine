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
using System.Xml.Xsl;
using System.IO;

public class XSLTFilter
{
    public static object GetData(StageParams config)
    {
        XmlDocument input = Util.Validate<XmlDocument>( config.data, "XSLTStage" );

        string xslPath = config.context.Server.MapPath( config.map );
        if (!File.Exists(xslPath))
            throw new FileLoadException(String.Format("Style sheet {0} is not found", xslPath));

        XslCompiledTransform transform = new XslCompiledTransform();
        transform.Load(xslPath, new XsltSettings(false, true), new XmlUrlResolver());

        XsltArgumentList al = new XsltArgumentList();

        int index = 0;
        foreach (XmlNode node in config.stage)
            if ( node.Attributes["type"] != null )
                al.AddParam( node.Name, "", config.allParams[index++]);

        if (!config.last)
        {
            MemoryStream output = new MemoryStream();
            transform.Transform(input, al, output);
            return output;
        }
        else
        {
            transform.Transform(input, al, config.outputStream);
            return null;
        }
    }
}
