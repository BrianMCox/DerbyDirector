using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public class FileService
    {
        public object Load(string fileName)
        {
            Event result;

            try
            {
                var json = System.IO.File.ReadAllText(fileName);
                result = JsonConvert.DeserializeObject<Event>(json);
            }
            catch(System.IO.FileNotFoundException ex)
            {
                result = null;
            }
            
            return result;
        }
        
        public void Save(string fileName, object entity)
        {
            string json = JsonConvert.SerializeObject(entity);
            System.IO.File.WriteAllText(fileName, json);
        }
    }
}