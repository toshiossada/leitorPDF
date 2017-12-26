using LeitorEcac.Entities;
using System;

namespace LeitorEcac
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnEnviar_Click(object sender, EventArgs e)
        {
            ECAC_DOCUMENTO ecac = new ECAC_DOCUMENTO();

            ecac.LerConteudoPDFECAC(@"C:\Temp\teste4.pdf");
        }



    }
}