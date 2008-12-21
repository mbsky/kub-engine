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

public class myfilters
{
    /*
        This filter an example of using code filters.
        It's add column to XmlDocument table with number of row value.
        Now this done actually via xslt position() function now - but I left this code for reference.
        
        <stage map="code:myfilters:add_line_numbers">
          <start_number type="int" input_index="1"/>
        </stage>
    */
    public static object add_line_numbers(StageParams config)
    {
        XmlDocument input = Util.Validate<XmlDocument>( config.data, "myfilter:add_line_numbers" );

        Util.AddNodedText(input.FirstChild.ChildNodes[0], "column", "", true);

        XmlNode body = input.FirstChild.ChildNodes[1];
        for (int i = 0; i < body.ChildNodes.Count; ++i) Util.AddNodedText(body.ChildNodes[i], "value", 
            ( (int)config.allParams[0] + i + 1).ToString(), true);

        return input;
    }

    /*
        This filter change xml table structure from
        <root>
        <head><column>cn</column></head>
        <body><row><value>123</value></row></body>
        </root>
     
        to
     
        <root>
        <head><column>cn</column></head>
        <body><row><cn>123</cn></row></body>
        </root>
    */
    public static object set_values_column_name(StageParams config)
    {
        XmlDocument input = Util.Validate<XmlDocument>(config.data, "myfilter:set_values_column_name");

        XmlNode head = input.FirstChild.ChildNodes[0];
        XmlNode body = input.FirstChild.ChildNodes[1];

        foreach (XmlNode row in body.ChildNodes)
            for (int i = 0; i < row.ChildNodes.Count; ++i)
                row.ReplaceChild(Util.ChangeName(row.ChildNodes[i], head.ChildNodes[i].FirstChild.Value), row.ChildNodes[i] ); 
        
        return input;
    }

    /*
        Convert current stream output to XMLDocument output 
    */
    public static object stream_to_xml_document(StageParams config)
    {
        Stream input = Util.Validate<Stream>(config.data, "myfilter:stream_to_xml_document");
        XmlDocument doc = new XmlDocument();
        doc.Load(input);
        input.Close();
        return doc;
    }

    /*
        direct time output
    */
    public static object output_time(StageParams config)
    {
        XmlDocument input = Util.Validate<XmlDocument>(config.data, "output_time");
        config.context.Response.Write(input.FirstChild.ChildNodes[1].FirstChild.FirstChild.FirstChild.Value.ToString());
        return null;
    }

    /*
        log current output 
    */
    public static object log_output(StageParams config)
    {
        //MemoryStream stream = new MemoryStream();
        //Types.WriteOutput( config.data, stream );
        //byte[] temp = new byte[ stream.Length ];
        //stream.Seek( 0, SeekOrigin.Begin );
        //stream.Read( temp, 0, (int)stream.Length );

        //Log.Error("log_output", System.Text.Encoding.UTF8.GetString(temp), config.context);
        return config.data;
    }
}
