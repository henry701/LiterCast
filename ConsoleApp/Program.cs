using System;
using System.IO;
using FmShell;
using LiterCast;
using NLog;

namespace ConsoleExecuterProj
{
    public class Program
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            LOGGER.Info("Starting Radio Application");

            using (var serv = new RadioCastServer("http://localhost:8081/radio/"))
            {
                serv.Start();

                var shell = new Shell(new ShellMethods(serv), "#FmRadio>", "FmRadio - UHU");
                shell.Start();
            }

        }

    }
}
