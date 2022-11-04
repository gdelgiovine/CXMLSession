# CXMLSession
A .NET Objects Persistence Library<p><b>
Almost all .NET objecta can be serialized in an XML files and viceversa.


            CXMLSession.SessionStore MyStoreA = new CXMLSession.SessionStore("MyStore");
            CXMLSession.SessionStore MyStoreB = new CXMLSession.SessionStore("MyStore");
            CXMLSession.SessionManager Manager = new CXMLSession.SessionManager();


            MyStoreA.StoreMode = CXMLSession.SessionStore.StoreModes.FileSystem;
            //MyStoreA.StoreMode = CXMLSession.SessionStore.StoreModes.MemoryMapped;
            MyStoreA.FileSystemStorePath = @"C:\CXMLSessionStore";
            MyStoreA.Namespace = "MYNAMESPACE";

            MyStoreB.StoreMode = CXMLSession.SessionStore.StoreModes.FileSystem ;
            MyStoreB.FileSystemStorePath = @"C:\CXMLSessionStore";
            MyStoreB.Namespace = "MYNAMESPACE";


            string stringvarA = "I'M A STRING";
            List<string> listOfString = new List<string>();
            listOfString.Add("1");
            listOfString.Add("2");


            MyStoreA.AddObject<string>("stringvar", stringvarA);
            MyStoreA.AddObject<List<string>>("listOfString", listOfString);


            MyStoreA.Write();
            MyStoreA.ClearObjects();

            MyStoreA = null;

            MyStoreB.Read();


            string stringvarB = (string)MyStoreB.SessionObjects["stringvar"].Value;

            List<string> listOfStringB = (List<string>) MyStoreB.SessionObjects["listOfString"].Value;
            
            // There is an helper method for getting the Object 
            List<string> listOfStringB2 = MyStoreB.GetSessionObject<List <string>>("listOfString");
