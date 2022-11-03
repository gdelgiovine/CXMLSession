using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CXMLSessionTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CXMLSession.SessionStore X = new CXMLSession.SessionStore("1");
            CXMLSession.SessionStore Y = new CXMLSession.SessionStore("1");
            CXMLSession.SessionManager Manager = new CXMLSession.SessionManager();

            ////X.StoreMode = CXMLSession.SessionStore.StoreModes.FileSystem;
            //X.StoreMode = CXMLSession.SessionStore.StoreModes.MemoryMapped;
            ////X.FileSystemStorePath = @"C:\CXMLSessionStore";
            ////X.Namespace = "VARIABILI";
            //Y.StoreMode = CXMLSession.SessionStore.StoreModes.MemoryMapped;
            ////Y.FileSystemStorePath = @"C:\CXMLSessionStore";
            ////Y.Namespace = "VARIABILI";


            //string ciccio = "CICCIO";

            //List<string> pippi = new List<string>();
            //pippi.Add("1");
            //pippi.Add("2");


            //X.AddObject<string>("CICCIO", ciccio);
            //X.AddObject<List<string>>("PIPPI", pippi);


            //X.SessionObjects["CICCIO"].Value  = "AFFANCULO";

            //List <string> ciccio2 = X.GetSessionObject<List <string>>("PiPPI");


            //Manager.Write(X);
            //X.SessionObjects.Clear();

            //Y.Read();

            string xml = "";
            xml = Manager.Serialize<System.Windows.Forms.TextBox >(this.textBox1);



        }
    }
}
