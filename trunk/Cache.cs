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
using System.Web.Caching;
using System.Collections;
using System.Collections.Generic;

public class Dependency:CacheDependency
{
    public static CacheDependency Get( string key )
    {
        Dependency dep = new Dependency(key);
        lock (deps)
        {
            deps.Add(dep);
        }
        return dep;
    }

    public static void Update( string keys )
    {
        lock (deps)
        {
            List<Dependency> temp = new List<Dependency>( deps.Count );
            foreach (Dependency dep in deps)
                if (!dep.DoUpdate(keys)) temp.Add(dep);
            
            deps.Clear();
            deps.AddRange(temp);
        }
    }

    public Dependency(string keyvalue)
    {
        value = keyvalue;
    }

    private bool DoUpdate( string keys )
    {
        if (value == keys)
        {
            base.NotifyDependencyChanged(this, EventArgs.Empty);
            return true;
        }
        else return false;
    }

    private string value;
    private static List<Dependency> deps = new List<Dependency>();
}