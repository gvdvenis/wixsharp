//css_dir ..\..\;
//css_ref System.Core.dll;
//css_ref Wix_bin\SDK\Microsoft.Deployment.WindowsInstaller.dll;
using System;
using static System.Collections.Specialized.BitVector32;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

#if Wix4
using WixToolset.Dtf.WindowsInstaller;
#else

using Microsoft.Deployment.WindowsInstaller;

#endif

using WixSharp;
using WixSharp.CommonTasks;

public class Script
{
    static public void Main(string[] args)
    {
        if (args.Contains("-remove"))
        {
            RemoveFiles(args[1]);
        }
        else
        {
            var project = new ManagedProject("CustomActionTest",
                    new Dir(@"%ProgramFiles%\My Company\My Product",
                        new File("setup.cs")),
                    new ManagedAction(CustomActions.MyAction, Return.check, When.Before, Step.LaunchConditions, Condition.NOT_Installed),
                    new ManagedAction(CustomActions.InvokeRemoveFiles, Return.check, When.Before, Step.LaunchConditions, Condition.NOT_Installed),
                    new Error("9000", "Hello World! (CLR: v[2]) Embedded Managed CA ([3])"));

            //project.Platform = Platform.x64;
            project.PreserveTempFiles = true;
            // project.OutDir = "bin";
            project.ManagedUI = ManagedUI.Default;
            project.AddBinary(new Binary("exta.wxl"));
            // project.LocalizationFile = "exta.wxl";

            project.UIInitialized += Project_UIInitialized;

            project.BuildMsi();
        }
    }

    static void Project_UIInitialized(SetupEventArgs e)
    {
        // example of doing localization by using Session object only
        var runtime = new MsiRuntime(e.Session);

        runtime.UIText.InitFromWxl(e.Session.ReadBinary("exta.wxl"), merge: true);

        // System message box
        MessageBox.Show("Click [WixUINext] button when you are ready.".LocalizeWith(runtime.Localize));

        // MSI session message box
        Record record = new Record(0);
        record[0] = "This is a [CompletelyCustomString] string.".LocalizeWith(runtime.Localize);

        e.Session.Message(InstallMessage.User | (InstallMessage)MessageButtons.OK | (InstallMessage)MessageIcon.Information, record);
    }

    static void RemoveFiles(string installdir)
    {
    }
}

public class CustomActions
{
    [CustomAction]
    public static ActionResult InvokeRemoveFiles(Session session)
    {
        var startInfo = new ProcessStartInfo();

        startInfo.UseShellExecute = true;
        startInfo.FileName = typeof(CustomActions).Assembly.Location;
        startInfo.Arguments = "-remove \"" + session.Property("INSTALLDIR") + "\"";
        startInfo.Verb = "runas";

        Process.Start(startInfo);

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult MyAction(Session session)
    {
        Record record = new Record(3);

        record[1] = "9000"; // error message template is defined (in `void Main()`) as a error string Id:9000
        record[2] = Environment.Version;
        record[3] = Is64BitProcess ? "x64" : "x86";

        session.Message(InstallMessage.User | (InstallMessage)MessageButtons.OK | (InstallMessage)MessageIcon.Information, record);

        // or
        // MessageBox.Show("Hello World! (CLR: v" + Environment.Version + ")", "Embedded Managed CA (" + (Is64BitProcess ? "x64" : "x86") + ")");

        session.Log("Begin MyAction Hello World");

        return ActionResult.Success;
    }

    public static bool Is64BitProcess
    {
        get { return IntPtr.Size == 8; }
    }
}