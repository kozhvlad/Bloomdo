namespace Bloomdo.Client.Core.Interfaces;

public interface IPreferencesService
{
    bool Get(string key, bool defaultValue);
    void Set(string key, bool value);
}
