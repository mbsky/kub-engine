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
using System.Reflection;
using System.Collections.Generic;
using System.Threading;

public class CodeFilters
{
    public static object GetData(StageParams config)
    {
        string[] parts = config.map.Split( new char[]{':'}, 3 );
        if ( parts.Length > 2 ) config.map = parts[2];

        string key = parts[0] + parts[1];
        StageAction action = null;
 
        lock ( loaded )
        {
            if ( loaded.ContainsKey( key ) ) action = loaded[key];    
        }
        
        if ( action == null )
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Type filters = asm.GetType(parts[0]);
            MethodInfo filter = filters.GetMethod(parts[1]);

            action = delegate( StageParams conf )
            {
                return filter.Invoke(null, new object[] { conf }); 
            };

            lock (loaded)
            {
                loaded.Add(key, action);
            }
        }

        return action(config);
    }

    private static Dictionary<string, StageAction> loaded = new Dictionary<string,StageAction>();
}
