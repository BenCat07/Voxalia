//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace VoxaliaBrowser
{
    static class Program
    {
        public static Stream STDOut;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            STDOut = Console.OpenStandardOutput();
            string page;
            if (args.Length == 1)
            {
                page = args[0];
            }
            else if (args.Length == 0)
            {
                page = "https://voxalia.xyz/";
            }
            else
            {
                page = "";
                for (int i = 0; i < args.Length; i++)
                {
                    page += args[i];
                    if (i + 1 < args.Length)
                    {
                        page += "%20";
                    }
                }
            }
            page = page.Trim();
            if (page.StartsWith("\"") && page.EndsWith("\""))
            {
                page = page.Substring(1, page.Length - 2);
            }
            bool term = true;
            if (page.StartsWith("{T}"))
            {
                term = false;
                page = page.Substring("{T}".Length);
            }
            if (!page.StartsWith("http://") && !page.StartsWith("https://"))
            {
                page = "http://" + page;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(page, term));

        }
    }
}
