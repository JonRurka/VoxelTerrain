using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGameServer;

public class Settings : AppSettings
{
    public static string SomeString;
    public static int SomeInt;
    public static float someFloat;

    public Settings(string file) : base(file)
    {
    }

    public override void LoadSettings()
    {
        base.LoadSettings();
        //<Setting> = TryGetValue("section", "setting", <Default Setting>);

        SomeString = TryGetValue("ExampleSection", "SomeString", "default string value");
        SomeInt = TryGetValue("ExampleSection", "SomeInt", 1);
        someFloat = TryGetValue("ExampleSection", "someFloat", 3.1415f);

    }

}
