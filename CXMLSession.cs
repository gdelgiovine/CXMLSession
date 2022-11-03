using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CXMLSession
{

    public class ExecutionResult
    {

        public ResultCodes ResultCode = 0;
        public int ErrorCode = 0;
        public string ResultMessage = "";
        public Exception Exception = null;
        public string Context = "";
        private bool mFailed;
        public bool Failed
        {
            get
            {
                if (ResultCode == ResultCodes.Failed)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                mFailed = value;
            }
        }

        public void Reset()
        {
            Failed = false;
            ResultCode = ResultCodes.Success;
            ErrorCode = 0;
            ResultMessage = "";
            Exception = null;
        }

        public ExecutionResult(string Context = "")
        {
            this.Reset();
            this.Context = Context;
        }

        public enum ResultCodes
        {
            Success = 0,
            Warning = 1,
            Failed = 2
        }
    }

    [Serializable]
    public class SessionObject
    {
        public string ID { get; set; }
        public string  Type { get; set; }
        public object Value { get; set; }
        public bool Lock { get; set; }
        public string Namespace { get; set; }
        public string Owner { get; set; }

    }


    public class SessionManager
    {
        public string Serialize<T>(T toSerialize)
        {
            try
            {
                System.Xml.XmlDocument XMLDoc = CXMLSession.CustomXmlSerializer.Serialize(toSerialize, 1, "SessionStore");
                return XMLDoc.OuterXml;
            }
            catch (Exception)
            {

                return "";
            }

            
        }
        public string SimpleSerialize<T>(T toSerialize)
        {

            try
            {
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(toSerialize.GetType());
                using (System.IO.StringWriter textWriter = new System.IO.StringWriter())
                {
                    try
                    {
                        xmlSerializer.Serialize(textWriter, toSerialize);
                        return textWriter.ToString();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex ;
            }

          
        }

        public T Deserialize<T>(string XMLString)
        {

            T returnObject = default(T);
            try
            {
                return (T)CustomXmlDeserializer.Deserialize(XMLString , 1);
            }
            catch (Exception)
            {

                return returnObject;
            }
          
        }


        public T SimpleDeserialize<T>(string XMLString)
        {
            T returnObject = default(T);
            if (string.IsNullOrEmpty(XMLString)) return default(T);

            try
            {
                System.IO.StringReader xmlStream = new System.IO.StringReader(XMLString);
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                returnObject = (T)serializer.Deserialize(xmlStream);
            }
            catch (Exception)
            {

            }
            return returnObject;
        }


        public ExecutionResult Write(SessionStore SessionStore)
        {
            ExecutionResult _E = new ExecutionResult();
            _E.Context = "Write";
            
            if (SessionStore.StoreMode ==  SessionStore.StoreModes.FileSystem )
            {
                _E = WriteToFileSystem(SessionStore);
                return _E;
            }

            if (SessionStore.StoreMode == SessionStore.StoreModes.MemoryMappedFile )
            {
                _E = WriteToMemoryMappedFile(SessionStore,true);
                return _E;
            }

            if (SessionStore.StoreMode == SessionStore.StoreModes.MemoryMapped)
            {
                _E = WriteToMemoryMappedFile(SessionStore);
                return _E;
            }



            return _E;

        }

        public ExecutionResult Read(ref SessionStore SessionStore, string ID = "", string Namespace = "")
        {
            ExecutionResult _E = new ExecutionResult();
            _E.Context = "Read";

            
            if (SessionStore.StoreMode == SessionStore.StoreModes.FileSystem)
            {
                _E = ReadFromFileSystem(ref SessionStore,ID,Namespace );
                
            }


            if (SessionStore.StoreMode == SessionStore.StoreModes.MemoryMappedFile )
            {
                _E =ReadFromMemoryMappedFile (ref SessionStore, true, ID, Namespace);

            }

            if (SessionStore.StoreMode == SessionStore.StoreModes.MemoryMapped)
            {
                _E = ReadFromMemoryMappedFile(ref SessionStore, false, ID, Namespace);

            }
            return _E;

        }

        private ExecutionResult WriteToFileSystem(SessionStore SessionStore)
        {
            ExecutionResult _E = new ExecutionResult();
            _E.Context = "WriteToFileSystem";


            System.Xml.XmlDocument XMLDoc = CXMLSession.CustomXmlSerializer.Serialize(SessionStore , 1, "SessionStore");

            
            if (XMLDoc.ToString()  == "")
            {
                _E.Failed = true;
                _E.ErrorCode = -10;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.ResultMessage = String.Format ("Unable to serialize SessionStore.ID({0}).",SessionStore .ID) ;
                return _E;
            }


            string _Path = SessionStore.FileSystemStorePath;

            if (_Path.EndsWith(@"\") == false) _Path = _Path + @"\";
            if (SessionStore.Namespace != "")
            {
                _Path = _Path + SessionStore.Namespace;
            }


            if (System.IO.Directory.Exists(SessionStore.FileSystemStorePath) == false)
            {
                _E.Failed = true;
                _E.ErrorCode = -11;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.ResultMessage = String.Format("SessionStore.ID({0}) - Directory ({1}) not exist!", SessionStore.ID,SessionStore.FileSystemStorePath);
                return _E;
            }

            try
            {
                System.IO.Directory.CreateDirectory(_Path);
            }
            catch (Exception _e)
            {
                _E.Failed = true;
                _E.ErrorCode = -12;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.Exception = _e;
                _E.ResultMessage = String.Format("SessionStore.ID({0}) - Unable to create Directory {1}. Error {2}", SessionStore.ID,_Path, _E.Exception.Message);

                return _E;
            }

            string _FileName = _Path + @"\" + SessionStore.ID + ".xml";

            try
            {
                XMLDoc.Save(_FileName);
               // System.IO.File.WriteAllText(_FileName, SessionObjectXMLValue);
            }
            catch (Exception _e)
            {

                _E.Failed = true;
                _E.ErrorCode = -14;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.Exception = _e;
                _E.ResultMessage = String.Format("SessionStore.ID({0}) - Unable to write file {1}. Error {2}",SessionStore.ID, _FileName, _E.Exception.Message);

                return _E;
            }


            return _E;

        }
        private ExecutionResult WriteToMemoryMappedFile(SessionStore SessionStore,bool fromfile=false)
        {
            string _FileName = "";
            ExecutionResult _E = new ExecutionResult();
            _E.Context = "WriteToMemoryMappedFile";
            
            System.Xml.XmlDocument XMLDoc = CXMLSession.CustomXmlSerializer.Serialize(SessionStore, 1, "SessionStore");


            if (XMLDoc.ToString() == "")
            {
                _E.Failed = true;
                _E.ErrorCode = -10;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.ResultMessage = String.Format("Unable to serialize SessionStore.ID({0}).", SessionStore.ID);
                return _E;
            }



            string _Path = SessionStore.FileSystemStorePath;
            if (fromfile)
            {
                if (_Path.EndsWith(@"\") == false) _Path = _Path + @"\";
                if (SessionStore.Namespace != "")
                {
                    _Path = _Path + SessionStore.Namespace;
                }



                if (System.IO.Directory.Exists(SessionStore.FileSystemStorePath) == false)
                {
                    _E.Failed = true;
                    _E.ErrorCode = -11;
                    _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                    _E.ResultMessage = String.Format("SessionStore.ID({0}) - Directory ({1}) not exist!", SessionStore.ID, SessionStore.FileSystemStorePath);
                    return _E;
                }

                try
                {
                    System.IO.Directory.CreateDirectory(_Path);
                }
                catch (Exception _e)
                {
                    _E.Failed = true;
                    _E.ErrorCode = -12;
                    _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                    _E.Exception = _e;
                    _E.ResultMessage = String.Format("SessionStore.ID({0}) - Unable to create Directory {1}. Error {2}", SessionStore.ID, _Path, _E.Exception.Message);

                    return _E;
                }
            }

            try
            {

                if (fromfile)
                {
                    _FileName = _Path + @"\" + SessionStore.ID + ".xml";
                    WriteObjectToMMF_FromFile(_FileName, XMLDoc.OuterXml);
                }
                else
                {
                    _FileName = SessionStore.Namespace + "_" + SessionStore.ID;
                    WriteObjectToMMF_Memory(_FileName, XMLDoc.OuterXml);
                }
                
            }
            catch (Exception _e)
            {

                _E.Failed = true;
                _E.ErrorCode = -14;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.Exception = _e;
                _E.ResultMessage = String.Format("SessionStore.ID({0}) - Unable to write file {1}. Error {2}", SessionStore.ID, _FileName, _E.Exception.Message);

                return _E;
            }


            return _E;

        }

     
        public void WriteObjectToMMF_Memory(string mmfFile, object objectData)
        {

            System.IO.MemoryMappedFiles.MemoryMappedFileSecurity mSec = new System.IO.MemoryMappedFiles.MemoryMappedFileSecurity();

            mSec.AddAccessRule(new System.Security.AccessControl.AccessRule<System.IO.MemoryMappedFiles.MemoryMappedFileRights>(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null),
                System.IO.MemoryMappedFiles.MemoryMappedFileRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
            // Convert .NET object to byte array
            byte[] buffer = ObjectToByteArray(objectData);
            // Create a new memory mapped file
            System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(mmfFile, buffer.Length, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.ReadWriteExecute, System.IO.MemoryMappedFiles.MemoryMappedFileOptions.None, mSec, System.IO.HandleInheritability.Inheritable);
            // Create a view accessor into the file to accommmodate binary data size
            System.IO.MemoryMappedFiles.MemoryMappedViewAccessor mmfWriter = mmf.CreateViewAccessor(0, buffer.Length);
            // Write the data
            mmfWriter.WriteArray<byte>(0, buffer, 0, buffer.Length);
            
        }



        public void WriteObjectToMMF_FromFile(string mmfFile, object objectData)
        {
            // Convert .NET object to byte array
            byte[] buffer = ObjectToByteArray(objectData);

            // Create a new memory mapped file
            using (System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(mmfFile, System.IO.FileMode.Create, null, buffer.Length))
            {
                // Create a view accessor into the file to accommmodate binary data size
                using (System.IO.MemoryMappedFiles.MemoryMappedViewAccessor mmfWriter = mmf.CreateViewAccessor(0, buffer.Length))
                {
                    // Write the data
                    mmfWriter.WriteArray<byte>(0, buffer, 0, buffer.Length);
                }
            }
        }

        public byte[] ObjectToByteArray(object inputObject)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();    // Create new BinaryFormatter
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();             // Create target memory stream
            binaryFormatter.Serialize(memoryStream, inputObject);       // Serialize object to stream
            return memoryStream.ToArray();                              // Return stream as byte array
        }


  

        public object ReadObjectFromMMF_Memory(string mmfFile,bool deleteafterread=true)
        {
            System.IO.MemoryMappedFiles.MemoryMappedFileSecurity mSec = new System.IO.MemoryMappedFiles.MemoryMappedFileSecurity();
            mSec.AddAccessRule(new System.Security.AccessControl.AccessRule<System.IO.MemoryMappedFiles.MemoryMappedFileRights>(new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null),
                System.IO.MemoryMappedFiles.MemoryMappedFileRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));

            // Get a handle to an existing memory mapped file
            System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(mmfFile, System.IO.MemoryMappedFiles.MemoryMappedFileRights.FullControl, System.IO.HandleInheritability.Inheritable);
            // Create a view accessor from which to read the data
            System.IO.MemoryMappedFiles.MemoryMappedViewAccessor mmfReader = mmf.CreateViewAccessor();
            // Create a data buffer and read entire MMF view into buffer
            byte[] buffer = new byte[mmfReader.Capacity];
            mmfReader.ReadArray<byte>(0, buffer, 0, buffer.Length);
            // Convert the buffer to a .NET object
            if (deleteafterread )
            {
                mmfReader.SafeMemoryMappedViewHandle.ReleasePointer();
            }
            return ByteArrayToObject(buffer);
                
        }

        public object ReadObjectFromMMF_FromFile(string mmfFile)
        {
            // Get a handle to an existing memory mapped file
            using (System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(mmfFile, System.IO.FileMode.Open))
            
            {
                // Create a view accessor from which to read the data
                using (System.IO.MemoryMappedFiles.MemoryMappedViewAccessor mmfReader = mmf.CreateViewAccessor())
                {
                    // Create a data buffer and read entire MMF view into buffer
                    byte[] buffer = new byte[mmfReader.Capacity];
                    mmfReader.ReadArray<byte>(0, buffer, 0, buffer.Length);

                    // Convert the buffer to a .NET object
                    return ByteArrayToObject(buffer);
                }
            }
        }

        public object ByteArrayToObject(byte[] buffer)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(); // Create new BinaryFormatter
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(buffer);    // Convert buffer to memorystream
            return binaryFormatter.Deserialize(memoryStream);        // Deserialize stream to an object
        }

        private ExecutionResult ReadFromFileSystem(ref SessionStore SessionStore,string ID="",string Namespace="")
        {
            ExecutionResult _E = new ExecutionResult();
            _E.Context = "ReadFromFileSystem";

            string _Path = SessionStore.FileSystemStorePath;

            if (_Path.EndsWith(@"\") == false) _Path = _Path + @"\";
            if (Namespace == "")
            {
                Namespace=SessionStore.Namespace;
            }
            
             _Path = _Path + SessionStore.Namespace; 

            if (System.IO.Directory.Exists(SessionStore.FileSystemStorePath) == false)
            {
                _E.Failed = true;
                _E.ErrorCode = -11;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.ResultMessage = String.Format("Directory {0} not exist!", SessionStore.FileSystemStorePath);
                return _E;
            }

            if (ID=="" )
            {
                ID = SessionStore.ID;
            }
            string _FileName = _Path + @"\" + ID + ".xml";
            string SessionStoreXMLValue = "";

            try
            {
                SessionStoreXMLValue = System.IO.File.ReadAllText(_FileName);
               if (SessionStore .DeleteAfterRead ==true)
                {
                    System.IO.File.Delete(_FileName);
                }
                SessionStore = Deserialize<SessionStore>(SessionStoreXMLValue);
            }
            catch (Exception _e)
            {

                _E.Failed = true;
                _E.ErrorCode = -14;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.Exception = _e;
                _E.ResultMessage = String.Format("Unable to write file {0}. Error {1}", _FileName, _E.Exception.Message);

                return _E;
            }

            return _E;

        }

        private ExecutionResult ReadFromMemoryMappedFile(ref SessionStore SessionStore, bool fromfile=false, string ID = "", string Namespace = "")
        {
            ExecutionResult _E = new ExecutionResult();
            string _filename = "";

            _E.Context = "ReadFromMemoryMappedFile";


            string _Path = SessionStore.FileSystemStorePath;


            if (_Path.EndsWith(@"\") == false) _Path = _Path + @"\";
            if (Namespace == "")
            {
                Namespace = SessionStore.Namespace;
            }

            _Path = _Path + SessionStore.Namespace;

            if (fromfile)
            {
                if (System.IO.Directory.Exists(SessionStore.FileSystemStorePath) == false)
                {
                    _E.Failed = true;
                    _E.ErrorCode = -11;
                    _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                    _E.ResultMessage = String.Format("Directory {0} not exist!", SessionStore.FileSystemStorePath);
                    return _E;
                }
            }

            if (ID == "")
            {
                ID = SessionStore.ID;
            }

        
           
            string SessionStoreXMLValue = "";

            try
            {


                if (fromfile)
                {
                    _filename = _Path + @"\" + ID + ".xml";
                    SessionStoreXMLValue = (string)ReadObjectFromMMF_FromFile(_filename);
                    if (SessionStore.DeleteAfterRead == true)
                    {
                        System.IO.File.Delete(_filename);
                    }
                }
                else
                {
                    _filename = SessionStore.Namespace + "_" + ID ;
                    SessionStoreXMLValue = (string)ReadObjectFromMMF_Memory(_filename, SessionStore .DeleteAfterRead );
                    
                }
                
                
                
                SessionStore = Deserialize<SessionStore>(SessionStoreXMLValue);
            }
            catch (Exception _e)
            {

                _E.Failed = true;
                _E.ErrorCode = -14;
                _E.ResultCode = ExecutionResult.ResultCodes.Failed;
                _E.Exception = _e;
                _E.ResultMessage = String.Format("Unable to read file {0}. Error {1}", _filename, _E.Exception.Message);

                return _E;
            }

            return _E;

        }

    }

   




[Serializable]
    public class SessionStore
    {

        public enum StoreModes
        {
            MemoryMappedFile = 0,
            FileSystem = 1,
            DataBase = 2,
            Rest = 3,
            MemoryMapped = 4

        }

        public StoreModes StoreMode { get; set; } = StoreModes.FileSystem;
        public Dictionary<string,SessionObject>  SessionObjects{ get; set; }
        public string ID { get; set; } 
        public string Owner { get; set; }
        public string Namespace { get; set; }
        public string DataBaseStoreConnectionString { get; set; }
        public string FileSystemStorePath { get; set; } = System.IO.Path.GetTempPath();
        public string RestStoreURL { get; set; }
        public string StoreUserName { get; set; }
        public string StorePassword { get; set; }
        public bool DeleteAfterRead { get; set; } = true;

        public SessionStore()
        {
             SessionObjects = new Dictionary<string, SessionObject >(StringComparer.InvariantCultureIgnoreCase );
            
        }

        public SessionStore(string ID)
        {
            this.ID = ID;
            SessionObjects = new Dictionary<string, SessionObject >(StringComparer.OrdinalIgnoreCase);
             
        }

        public ExecutionResult Write()
        {
            CXMLSession.SessionManager M = new CXMLSession.SessionManager();

            return M.Write(this);
        }

        public ExecutionResult Read()
        {
            ExecutionResult _E = new CXMLSession.ExecutionResult();
            CXMLSession.SessionManager M = new CXMLSession.SessionManager();
            SessionStore Me = (SessionStore)this.MemberwiseClone();
            _E=M.Read(ref Me);
            if (_E.Failed == false)
            {
                this.ID = Me.ID;
                this.Namespace = Me.Namespace;
                this.Owner = Me.Owner;
                this.SessionObjects = Me.SessionObjects;
                this.RestStoreURL = Me.RestStoreURL;
                this.StoreMode = Me.StoreMode;
                this.StorePassword = Me.StorePassword;
                this.StoreUserName = Me.StoreUserName;
                this.FileSystemStorePath = Me.FileSystemStorePath;
            }
            return _E;

        }

        public void AddObject <T> (string Key, object value, string Namespace="",string Owner="", bool Lock=false)
        {
            SessionObject o = new SessionObject();
            SessionObject _o = new SessionObject();
            o.ID = Key;
            o.Value = value;
            o.Namespace = Namespace;
            o.Owner = Owner;
            o.Lock = Lock;
            o.Type = typeof(T).ToString ();



            if (this.SessionObjects.ContainsKey(Key)==false)
            {
                this.SessionObjects.Add(Key,o);
            
            }
        
            else
            {
            this.SessionObjects[Key].Value  = value ;
                
            }

        }
        public void RemoveObject(string Key)
        {

            if (this.SessionObjects.ContainsKey(Key) == true)
            {
                this.SessionObjects.Remove(Key);
            }

        }

        public void UpdateObject(string Key,SessionObject  value)
        {

            if (this.SessionObjects.ContainsKey(Key) == true)
            {
                this.SessionObjects[Key] = value;
            }

        }

        public void ClearObjects()
        {
            this.SessionObjects.Clear();
        }
           
        public T GetSessionObject<T>(string Key)
        {
            T returnObject = default(T);

            try
            {
         
                returnObject = (T)this.SessionObjects[Key].Value ;

            }
            catch (Exception )
            {

                return returnObject;
            }

            return returnObject;
        }

    }
}
