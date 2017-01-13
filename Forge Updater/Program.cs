﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.Threading;
using System.Net;
using System.IO.Compression;
using System.IO;

namespace Forge_Updater
{
    class Program
    {
        static Release[] releases;
        static string executableDir;
        static string forgeDir;

        static int Main(string[] args)
        {
            executableDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            forgeDir = Path.GetDirectoryName(executableDir);
            bool restartForge = false;
            if(args.Length == 0)
            {
                Console.WriteLine("Usage:\n\nForgeUpdater.exe [options]\n\nOptions:\n* Download latest release     -d\n* Download latest nightly     -dn\n* Install downloaded release  -i\n* Latest release info       -info\n* Restart Forge              -r");
                return 0;
            }
            var client = new GitHubClient(new ProductHeaderValue("forge-updater"));
            GetReleases(client).Wait();
            
            foreach(string arg in args)
            {
                if (arg.Equals("-info"))
                {
                    foreach(Release latest in releases)
                    {
                        Console.WriteLine($"Name: {latest.Name}\nDescription:\n{latest.Body}");
                        Console.WriteLine($"URL: {latest.Assets[0].BrowserDownloadUrl}");
                        Console.WriteLine($"Upload Date: {latest.Assets[0].UpdatedAt}");
                        break;
                    }
                }
                if (arg.Equals("-d"))
                {
                    foreach(Release latest in releases)
                    {
                        if (!latest.Prerelease)
                        {
                            Console.WriteLine($"Name: {latest.Name}\nDescription:\n{latest.Body}");
                            Console.WriteLine($"URL: {latest.Assets[0].BrowserDownloadUrl}");
                            Console.WriteLine($"Upload Date: {latest.Assets[0].UpdatedAt}");
                            int code = DownloadRelease(latest.Assets[0].BrowserDownloadUrl, "currentRelease", latest.Assets[0].UpdatedAt.ToString());
                            if (code != 0)
                                return code;
                            break;
                        }
                    }
                }
                else if(arg.Equals("-dn"))
                {
                    foreach (Release latest in releases)
                    {
                        Console.WriteLine($"Name: {latest.Name}\nDescription:\n{latest.Body}");
                        Console.WriteLine($"URL: {latest.Assets[0].BrowserDownloadUrl}");
                        Console.WriteLine($"Upload Date: {latest.Assets[0].UpdatedAt}");
                        int code = DownloadRelease(latest.Assets[0].BrowserDownloadUrl, "currentRelease", latest.Assets[0].UpdatedAt.ToString());
                        if (code != 0)
                            return code;
                        break;
                    }
                }
                else if (arg.Equals("-i"))
                {
                    foreach(string dir in Directory.GetDirectories("currentRelease/"))
                    {
                        if (!dir.Equals(Path.Combine(executableDir, "currentRelease/updater/")))
                        {
                            string dirName = new DirectoryInfo(dir).Name;
                            Directory.Move(dir, Path.Combine(forgeDir, dirName + @"\"));
                        }
                        else
                        {
                            Directory.Move(dir, Path.Combine(forgeDir, @"new_updater\"));
                        }
                    }
                    foreach (string file in Directory.GetFiles("currentRelease/"))
                    {
                        Directory.Move(file, Path.Combine(forgeDir,Path.GetFileName(file)));
                        File.Delete(file);
                    }
                }
                else if (arg.Equals("-r"))
                {
                    restartForge = true;
                }
            }
            if (restartForge)
                System.Diagnostics.Process.Start(Path.Combine(forgeDir, "Smash Forge.exe"));

            return 0;
        }

        static async Task GetReleases(GitHubClient client)
        {
            List<Release> Releases = new List<Release>();
            foreach (Release r in await client.Repository.Release.GetAll("jam1garner", "Smash-Forge"))
                Releases.Add(r);
            releases = Releases.ToArray();
        }

        static int DownloadRelease(string downloadUrl, string downloadName, string versionTime)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, downloadName + ".zip");
                }
                if (Directory.Exists(downloadName + "/"))
                    Directory.Delete(downloadName + "/", true);
                ZipFile.ExtractToDirectory(downloadName + ".zip", downloadName + "/");
                string versionTxt = Path.Combine(Path.GetFullPath(downloadName + "/"), "version.txt");
                File.WriteAllText(versionTxt, versionTime);
                return 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}