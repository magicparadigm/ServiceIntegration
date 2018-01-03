using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Confirm : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        button.InnerText = "Home";
        button.ServerClick += new EventHandler(button_Click);
    }

    protected void button_Click(object sender, EventArgs e)
    {
        Response.Redirect("ServiceIntegration.aspx");
    }
}