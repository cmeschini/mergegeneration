using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

/// <summary>
/// Summary description for Telefono
/// </summary>
public class Telefono
{
	public Telefono()
	{
        this._name = "Telefono";
	}

    private string _name;
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    //Agrega
    private string _lastName;
    public string LastName
    {
        get { return _lastName; }
        set { _lastName = value; }
    }

    //Modifica
    private int _age;
    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }


    public void ForNext( int from, int to)
    {
        for (int i = from; i <= to; i++)
        {
            //TODO:
        }
    
    }

}
