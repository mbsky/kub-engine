using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Web.Hosting;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Threading;

public class Engine
{
    public static int maxThreads = 0;
    public static int activeHandles = 0;
    public static int errorDetected = 0;

    public static void Handle(string url, HttpContext context)
    {
        try
        {
            Interlocked.Increment(ref activeHandles);
            if (activeHandles > maxThreads) maxThreads = activeHandles;

            if (instance == null)
            {
                Log.Info("KUCE_initialisation", context);
                instance = new Engine(context);
            }

            instance.Process(url, context);
        }
        catch (Exception ex)
        {
            HandleError(ex, context);
        }
        finally
        {
            Interlocked.Decrement(ref activeHandles);
        }
    }

    ~Engine()
    {
        try
        {
            Log.Info("KUCE_destruction", null);
        }
        catch (Exception)
        {

        }
    }

    public static void HandleError(Exception ex, HttpContext context)
    {
        Interlocked.Increment(ref errorDetected);

        if (ex.InnerException != null) ex = ex.InnerException;
        Error(ex, context);
        context.Response.StatusCode = 500;
        context.Response.Output.Write("Запрос не может быть выполнен. Присылайте замечания info@akura.ru");
    }

    public static void Error( Exception ex, HttpContext context )
    {
        Log.Error( ex, context );
    }

    private void Process(string url, HttpContext context)
    {
        string[] urlParts = url.Split('/');
        
        
        string fullActionName = String.Join("-", new string[] { urlParts[2], urlParts[1], urlParts[0] });
        
        // get service description
        if (!serverActions.ContainsKey(fullActionName)) 
            throw new Exception( String.Format("Action {0} ( sysname: {1} ) is not defined", url, fullActionName));
        
        XmlNode service = serverActions[fullActionName];
        Stream  outputStream = null;
        
        //  ---------- cashe case ----------------------------------------------------------------------------

        bool noinfo = Util.GetAttr(service, "noinfo", false);
        bool nocache = Util.GetAttr(service, "nocache", false ) || System.Configuration.ConfigurationManager.AppSettings["nocache"] == "true";

        if (!nocache)
        {
            // have cached values?
            object saved = context.Cache.Get(url);
            if (saved != null)
            {
                Log.Info(String.Format("KUCE_use_cache: {0}", url), context);
                Types.WriteOutput(saved, context.Response.OutputStream, false);
                return;
            }

            outputStream = new MemoryStream(0x5000);
        }
        else outputStream = context.Response.OutputStream;

        //   ----------------------------------------------------------------------------
        
        // log action start 
        int start = Environment.TickCount;

        if ( !noinfo )
            Log.Info(String.Format("KUCE_process {0}, current active: {1}", url, activeHandles.ToString()), context);
        
        // prepare all service params       
        string[] allValues = new string[urlParts.Length - 3];
        Array.Copy(urlParts, 3, allValues, 0, allValues.Length);
        
        // the process
        List<object> output = new List<object>();
        object data = null;
        int    currentParamIndex = 0; // specify how params was used from input

        // action must be locked?

        if (service.Attributes["locked"] != null) Monitor.Enter(service);
        try
        {
            foreach (XmlNode stage in service.ChildNodes)
            {
                if (stage.Name == "stage")
                {
                    bool lastStage = false;

                    if (stage.NextSibling == null || Util.GetAttr(stage.NextSibling, "flush", false) )
                        lastStage = true;

                    if ( Util.GetAttr(stage, "flush", false) && data != null ) output.Add(data);

                    object[] stageParams = Types.GetParamsForStage(stage, allValues, ref currentParamIndex, context);

                    /*
                        inplace параметры применяются к mapping ещё до того как стейдж начинается парситься,
                        что позволяет очень гибко настраивать стейд через параметры
                    */
                    List<object> inplace = new List<object>();
                    for (int i = 0; i < stage.ChildNodes.Count; ++i)
                    {
                        XmlNode paramNode = stage.ChildNodes[i];
                        if (Util.GetAttr(paramNode, "inplace", false ) ) inplace.Add(stageParams[i]);
                    }

                    string stageMapping = String.Format(stage.Attributes["map"].Value, inplace.ToArray());

                    StageParams config = new StageParams(data, stage, stageParams, context, stageMapping, lastStage, outputStream);

                    data = StageParams.Dispatch(config, StageProcessor.actions);
                }
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, context);
        }
        finally
        {
            if (service.Attributes["locked"] != null) Monitor.Exit(service);
        }

        if (data != null) output.Add(data);

        foreach (object result in output) Types.WriteOutput(result, outputStream, true);

        Types.WriteOutput(outputStream, context.Response.OutputStream, false); 
           
        // -------- caching issues ----------------------------------------------------------------
                
        if (!nocache)
        {
            // cache is filled and ready
            context.Cache.Insert(url, outputStream, Dependency.Get( String.Join("/", new string[] { urlParts[0], urlParts[1] } ) ),
                    System.Web.Caching.Cache.NoAbsoluteExpiration, 
                    System.Web.Caching.Cache.NoSlidingExpiration );
        }

        if (Util.GetAttr(service, "noclientcache", false ))
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
        }
        else
        {
            context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(60));
            context.Response.Cache.SetProxyMaxAge(TimeSpan.FromMinutes(60));
        }

        if ( service.Attributes["flushcache"] != null) Dependency.Update(service.Attributes["flushcache"].Value);

        // ----------------------------------------------------------------------------------------
        
        if ( !noinfo )
            Log.Info(String.Format("KUCE_end {0}, time: {1}", url, Environment.TickCount - start ), context);
    }

    private Engine( HttpContext context )
    {
        serviceDesc = new XmlDocument();
        serviceDesc.Load( context.Server.MapPath( "~/server/service.config" ) );

        foreach (XmlNode root in serviceDesc.ChildNodes )
        foreach (XmlNode service in root)
            foreach (XmlNode section in service)
                foreach (XmlNode action in section)
                {
                    Util.TransiteAttribute(section, action, "locked");
                    Util.TransiteAttribute(section, action, "nocache");
                    Util.TransiteAttribute(section, action, "flushcache");
                    Util.TransiteAttribute(section, action, "noclientcache");
                    Util.TransiteAttribute(section, action, "noinfo");

                    serverActions.Add(String.Join("-", new string[] { action.Name, section.Name, service.Name }), action);
                }
    }

    public static XmlNode GetConfig()
    {
        if (config == null)
        {
            XmlDocument conf = new XmlDocument();
            conf.Load(HttpContext.Current.Server.MapPath("~/server/kuce.config"));
            config = conf.FirstChild.NextSibling.FirstChild;
        }
        return config;
    }

    private static XmlNode config;
    private Dictionary<string, XmlNode> serverActions = new Dictionary<string,XmlNode>();
    private XmlDocument   serviceDesc;
    private static Engine instance; 
}
