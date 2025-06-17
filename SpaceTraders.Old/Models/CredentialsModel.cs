using Newtonsoft.Json;
using System.IO;

namespace SpaceTraders.Models
{
    public class CredentialsModel
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }

    public interface ICredentials
    {
        CredentialsModel Get();
        bool Save(CredentialsModel model);
    }
    public class FileCredentials : ICredentials
    {
        private readonly string _filename;
        public FileCredentials(string filename)
        {
            _filename = filename;
        }
        public CredentialsModel Get()
        {
            if (File.Exists(_filename))
            {
                var model = JsonConvert.DeserializeObject<CredentialsModel>(File.ReadAllText(_filename));
                return model;
            }
            return new CredentialsModel();
        }
        public bool Save(CredentialsModel model)
        {
            File.WriteAllText(_filename, JsonConvert.SerializeObject(model));
            return true;
        }
    }
}
