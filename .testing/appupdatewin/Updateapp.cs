﻿using Octokit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace appupdatewin
{
    static class Updateapp
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static async Task Main()
        {
            //Setup some Variables/Statics.
            long repo_id = 157181568;

            //Get the Github Repo Release Data.
            var client = new GitHubClient(new ProductHeaderValue("85th_DCS_MOD_Sync"));
            var releases = await client.Repository.Release.GetAll(repo_id);
            var latest_rel_asset = await client.Repository.Release.GetAllAssets(repo_id, releases[0].Id);

            //Make it easier to extract the data.
            var latest = releases[0];
            var asset = latest_rel_asset[0];

            //Setup Uri to download asset from.
            Uri asset_uri = new Uri(asset.BrowserDownloadUrl);

            //Setup the Download file path and Zip extraction path.
            string download_file = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Downloads\") + asset.Name;
            string extract_path = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Saved Games\");
            // Normalizes the extract path.
            extract_path = Path.GetFullPath(extract_path);

            //Get Latest Github Version Info.
            var Tag_ver = latest.TagName;
            //var Tag_ver = "v0.5.0.8-alpha"; //Only for testing
            string output = Regex.Replace(Tag_ver, "[^0-9 .]+", string.Empty); //Strip out all but numbers and "." to capture version numbers only. 

            //This section provides protection in case the versioning does not exactly match #.#.# scheme, whilst also
            //allowing for multi digit version numbers e.g. the 45 in the minor version of 1.45.6.
            var count = output.Count(x => x == '.'); //count number of full stops.
            string out_p = output; //Set out_p to initiate and have a value if the if's are passed over.
            if (count < 1) {out_p = output + ".0.0";} //If only one number e.g. V1, adds trailing .0.0 otherwise next section will break.
            else if (count < 2) {out_p = output + ".0"; } //If only two numbers e.g. V1.5, adds trailing .0 otherwise next section will break.

            //Split the version out_p string into and array based on the '.' character, to protect multi digit numbers.
            string[] ver_list = out_p.Split(new char[] {'.'}, StringSplitOptions.None);
            int.TryParse(ver_list[0], out int git_major); //Convert first array numbers to int for Major Version revision.
            int.TryParse(ver_list[1], out int git_minor); //Convert second array numbers to int for minor Version revision.
            int.TryParse(ver_list[2], out int git_build); //Convert third array numbers to int  for Build Version revision. 
            //From here, don't care for any more numbers, although more could be supported, it's not required for this App.

            //Get Application Version and split into their own int's for next if statement bool operators.
            Version App_ver = Assembly.GetExecutingAssembly().GetName().Version;
            int app_major = App_ver.Major;
            int app_minor = App_ver.Minor;
            int app_build = App_ver.Build;

            //Perform bool operators on version numbers. When a build update happens, the minor and major versions shall be the same,
            //else we would have the app update if minor version may be higher, and and older version build goes up. It's highly
            //unlikely, but this provides some protection.
            //When Minor updates happen, we want to update regradless of "Build" update version as these would be reset on the minor update.
            //When Major Updates happen, don't care for "Minor" or "Build" update version as these. would be reset on the Major update.
            if (git_build > app_build && git_minor == app_minor && git_major == app_major
                || git_minor > app_minor && git_major == app_major
                || git_major > app_major)
            {
                var update = new Updateconfirm(asset.Name, latest.TagName, latest.PublishedAt, Math.Round((double)asset.Size / 1024, 2), asset_uri, download_file, extract_path, latest.HtmlUrl);
                update.ShowDialog();
            }
        }
    }
}
