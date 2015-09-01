Kube is a lightweight asp.net engine that provides support for a fast creation webservice-like server handlers, simplifing data-access and xslt-processing in declarative style.

**Example:**

This example gets list from database and render it as html radio list with given name.

1. Engine access

```
public class temp : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) 
    {
        string rest = context.Request.Params[""];
        if (rest != null)
        {
            string rawurl = context.Request.RawUrl.Split(new char[] { '=' })[1];
            Engine.Handle(rawurl, context);
        }
    }
 
    public bool IsReusable {
        get {
            return true;
        }
    }
}
```

2. Engine call on server side

```
<div class="rowvalue">
<%Engine.Handle("output/general/get_time_types/report_period_ratings", Context);%>
</div>
```

3. Service description:
```
<services>
  <output>
    <general>
      <get_time_types>
        <stage map="sql:mysql:code:table:select * from reports_periods"/>
        <stage map="xslt:~/xslt/htmlTimeRadioList.xslt">
          <inputname type="string"/>
        </stage>
      </get_time_types>
    </general>
 </output>  
</services>
```

4. XSLT
```
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:param name="inputname"/>

  <xsl:template match="/Root">
    <xsl:for-each select="body/raw">
      <input type="radio">
        <xsl:attribute name="name">
          <xsl:value-of select="$inputname"/>
        </xsl:attribute>
        <xsl:attribute name="time">
          <xsl:value-of select="node()[2]"/>
        </xsl:attribute>
        <xsl:attribute name="value">
          <xsl:value-of select="node()[1]"/>
        </xsl:attribute>
        <xsl:attribute name="hours_step">
          <xsl:value-of select="node()[4]"/>
        </xsl:attribute>
        <xsl:attribute name="steps_count">
          <xsl:value-of select="node()[5]"/>
        </xsl:attribute>
        <xsl:if test="position() = 3">
          <xsl:attribute name="checked">
            <xsl:value-of select="true"/>
          </xsl:attribute>
        </xsl:if>
      </input>
      <span>
        <xsl:value-of select="node()[3]"/>
      </span>
    </xsl:for-each>
</xsl:template>

  
</xsl:stylesheet> 
```