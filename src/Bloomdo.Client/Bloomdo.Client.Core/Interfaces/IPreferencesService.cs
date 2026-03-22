namespace Bloomdo.Client.Core.Interfaces;

public interface IPreferencesService
{
    bool Get(string key, bool defaultValue);
    void Set(string key, bool value);
    string Get(string key, string defaultValue);
    void Set(string key, string value);
    void Remove(string key);
}
