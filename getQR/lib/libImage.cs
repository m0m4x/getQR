using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace getQR
{
    static class libImage
    {

        static public bool ConvertSingleImage(string in_filename, string out_filename, int res)
        {
            libPDF conv = new libPDF();

            //Setup the converter
            conv.RedirectIO = true; //Quiet mode
            conv.FirstPageToConvert = 1;
            conv.LastPageToConvert = 1;
            conv.FitPage = true;
            conv.OutputFormat = "png16m";
            conv.ResolutionX = res;
            conv.ResolutionY = res;
            System.IO.FileInfo input = new FileInfo(in_filename);
            //If the output file exists already, be sure to add a
            //random name at the end until it is unique!
            try
            {
                File.Delete(out_filename);
            }
            catch (Exception ex)
            {

            }

            string arg = conv.ParametersUsed;

            if (conv.Convert(input.FullName, out_filename) != true)
            {
                Console.WriteLine(string.Format("Error: {0} NOT converted! Check Args! {1}", in_filename, arg));
                return false;
            }
            return true;
        }

    }
}
