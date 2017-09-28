using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;

namespace getQR
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                bool parm_error = false;
                if (args.Length == 0) exitWithHelp(); // Exit! No param

                //set mandatory parm
                string in_path = "";

                //optional parm
                bool cropcenter = false;
                bool emulate = false;
                bool debug = false;

                //optional blob parm
                bool blob = false;
                string bfilterw = "";
                double bfilterw_min = 1;
                double bfilterw_max = 1;
                string bfilterh = "";
                double bfilterh_min = 1;
                double bfilterh_max = 1;
                bool blob_noff = false;
                bool blob_noshape = false;
                bool blob_notrotate = false;
                string blob_zone = "";
                double blob_zonex = 0.5;
                double blob_zoney = 0.5;

                //parsing parm
                for (int p = 0; p < args.Length; p++)
                {
                    //for each parm get type and value
                    //ex. -parm value -parm2 value2
                    //get parm
                    switch (args[p])
                    {
                        case "-debug":
                            debug = true;
                            break;
                        case "-f":
                            in_path = args[p + 1];
                            break;
                        case "-cropcenter":
                            cropcenter = true;
                            break;
                        case "-emulate":
                            emulate = true;
                            break;
                        case "-blob":
                            blob = true;
                            break;
                        case "-bfilterw":
                            bfilterw = args[p + 1];
                            break;
                        case "-bfilterh":
                            bfilterh = args[p + 1];
                            break;
                        case "-bnoff":
                            blob_noff = true;
                            break;
                        case "-bzone":
                            blob_zone = args[p + 1];
                            break;
                        case "-bnoshape":
                            blob_noshape = true;
                            break;
                        case "-bnotrotate":
                            blob_notrotate = true;
                            break;
                        default:
                            if (args[p].StartsWith("-"))
                                exitNotValid(args[p]);    // Exit! Invalid param
                            break;
                    }
                }

                //check mandatory param
                if (in_path.Equals("")) exitWithHelp();

                //check others param
                if (!bfilterw.Equals("")) {
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline;
                    Regex pattern = new Regex(@"((?:[0]\.)?\d+)\-((?:[0]\.)?\d+)", options);
                    Match match = pattern.Match(bfilterw);
                    if (match.Success && match.Groups.Count.Equals(3)) {
                        bfilterw_min = Convert.ToDouble(match.Groups[1].Value.Replace('.', ','));
                        bfilterw_max = Convert.ToDouble(match.Groups[2].Value.Replace('.', ','));
                    } else { exitWithError("Opzione '-bfilterw' non valida.","Specificare i valori minimi e massimi nel seguente formato:", "   -bfilterw valoremin-valoremax", "   es. -bfilterw 0.30-0.40"); }
                }
                if (!bfilterh.Equals(""))
                {
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline;
                    Regex pattern = new Regex(@"((?:[0]\.)?\d+)\-((?:[0]\.)?\d+)", options);
                    Match match = pattern.Match(bfilterh);
                    if (match.Success && match.Groups.Count.Equals(3)) {
                        bfilterh_min = Convert.ToDouble(match.Groups[1].Value.Replace('.', ','));
                        bfilterh_max = Convert.ToDouble(match.Groups[2].Value.Replace('.', ','));
                    } else { exitWithError("Opzione '-bfilterh' non valida.", "Specificare i valori minimi e massimi nel seguente formato:", "   -bfilterh valoremin-valoremax", "   es. -bfilterh 0.30-0.40");}
                }
                if (!blob_zone.Equals(""))
                {
                    RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline;
                    Regex pattern = new Regex(@"((?:[0]\.)?\d+)\,((?:[0]\.)?\d+)", options);
                    Match match = pattern.Match(blob_zone);
                    if (match.Success && match.Groups.Count.Equals(3))
                    {
                        blob_zonex = Convert.ToDouble(match.Groups[1].Value.Replace('.', ','));
                        blob_zoney = Convert.ToDouble(match.Groups[2].Value.Replace('.', ','));
                    }
                    else { exitWithError("Opzione '-bzone' non valida.", "Specificare le coordinate del punto dove cercare il barcode.", "   -bzone x,y", "   es. -bzone 0.5,0.5"); }
                }

                //check validity
                if (File.Exists(in_path))
                {
                    in_path = Path.GetFullPath(in_path);
                }
                else
                    exitFileNotFound(in_path);

                //START
                Stopwatch stopWatch = new Stopwatch();
                if (emulate)
                {
                    stopWatch.Start();
                }

                //Convert to image if PDF
                string tmp_path = "";
                bool tmp_file = false;
                if (Path.GetExtension(in_path).Equals(".pdf")) {
                    if (debug) { Console.WriteLine("Converting pdf..."); }
                    tmp_path = in_path + ".png";
                    tmp_file = true;
                    libImage.ConvertSingleImage(in_path, tmp_path, 300);
                } else {
                    tmp_path = in_path;
                }

                //Load image in memory and del file
                System.Drawing.Bitmap tmp_img;
                using (System.Drawing.Bitmap img_source = (Bitmap)Bitmap.FromFile(tmp_path))
                {
                    tmp_img = new Bitmap(img_source);
                }
                if(tmp_file) File.Delete(tmp_path);

                //Get Info on page
                int page_w = tmp_img.Width;
                int page_h = tmp_img.Height;
                if (debug) { Console.WriteLine("File dimension: w="+ page_w + " h=" + page_h); }

                //Crop Center
                if (cropcenter)
                {
                    if (debug) { Console.WriteLine("Cropping central image..."); }
                    int crop_x = Convert.ToInt32(((double)tmp_img.Width * 0.3)),
                        crop_y = Convert.ToInt32(((double)tmp_img.Height * 0.3)),
                        crop_width = Convert.ToInt32(((double)tmp_img.Width * 0.7) - crop_x),
                        crop_height = Convert.ToInt32(((double)tmp_img.Height * 0.7) - crop_y);
                    //source = source.crop(crop_x, crop_y, crop_width, crop_height);
                    tmp_img = tmp_img.Clone(new Rectangle(crop_x, crop_y, crop_width, crop_height), PixelFormat.Format32bppArgb);
                    page_w = tmp_img.Width;
                    page_h = tmp_img.Height;
                    if (debug) { Console.WriteLine("New file dimension: w=" + page_w + " h=" + page_h); }
                } else
                    tmp_img = AForge.Imaging.Image.Clone(tmp_img, PixelFormat.Format32bppArgb);

                //Blob Analysis
                if (blob)
                {
                    if (debug) { Console.WriteLine("Starting Blob Analysis..."); }

                    // filter GreyScale
                    Grayscale filterg = new Grayscale(0.2125, 0.7154, 0.0721);
                    tmp_img = filterg.Apply(tmp_img);
                    Bitmap tmp_img_wrk = (Bitmap) tmp_img.Clone();

                    // filter Erosion3x3
                    BinaryErosion3x3 filter = new BinaryErosion3x3();
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);
                    tmp_img_wrk = filter.Apply(tmp_img_wrk);

                    //Binarization
                    SISThreshold filterSIS = new SISThreshold();
                    tmp_img_wrk = filterSIS.Apply(tmp_img_wrk);

                    //Inversion
                    Invert filterI = new Invert();
                    tmp_img_wrk = filterI.Apply(tmp_img_wrk);

                    //Blob Analisys 
                    BlobCounterBase bc = new BlobCounter();
                    bc.FilterBlobs = true;
                    if (!bfilterw.Equals("")) { 
                        bc.MinWidth = Convert.ToInt32(page_w * bfilterw_min);   // 0.15 in proporzione è il 20%
                        bc.MaxWidth = Convert.ToInt32(page_w * bfilterw_max);   // 0.30
                    }
                    if (!bfilterh.Equals("")) {
                        bc.MinHeight = Convert.ToInt32(page_h * bfilterh_min);  // 0.10 in proporzione è il 15%
                        bc.MaxHeight = Convert.ToInt32(page_h * bfilterh_max);  // 0.20
                    }
                    if (debug) { Console.WriteLine("Searching blob (Dimension filter: w=" + bc.MinWidth + "-" + (bc.MaxWidth.Equals(int.MaxValue) ? "max" : bc.MaxWidth.ToString()) + " h=" + bc.MinHeight + "-" + (bc.MaxHeight.Equals(int.MaxValue) ? "max": bc.MaxHeight.ToString()) +")"); }
                    bc.ObjectsOrder = ObjectsOrder.Size;
                    bc.ProcessImage(tmp_img_wrk);
                    Blob[] blobs = bc.GetObjectsInformation();
                    if (debug) { Console.WriteLine("Blobs found: " + blobs.Count()); }

                    //Esamina Blobs
                    int i = 1;
                    foreach (Blob b in blobs)
                    {
                    
                        //Escludi blob contenitore (l'immagine stessa)
                        if (b.Rectangle.Width == page_w)
                        { if (debug) { Console.WriteLine("Blob "+i+": skip! (is container)"); }  i++; continue; }

                        //check form factor
                        if (!blob_noff) { 
                            double formf = (Convert.ToDouble(b.Rectangle.Width) / Convert.ToDouble(b.Rectangle.Height))*100;
                            if (formf<95)
                            {
                                //skip Form Factor Not a square
                                if (debug) { Console.WriteLine("Blob " + i + ": Check 1 - Form factor > 95 Failed! (form factor is not square " + formf + "<95) Blob Skipped!"); 
                                Console.WriteLine("You can disable this check with -bnoff parameter.");
                                }
                            i++;  continue;
                            }
                            if (debug) { Console.WriteLine("Blob " + i + ": Check 1 - Form factor > 95  " + formf + " Ok!"); }
                        } else if (debug) { Console.WriteLine("Blob " + i + ": Check 1 - Form factor > 95 skipped by option -bnoff "); }

                        //check zone
                        if (!blob_zone.Equals(""))
                        {
                            Rectangle bZone = b.Rectangle;
                            bZone.Inflate(Convert.ToInt32(b.Rectangle.Width * 0.2), Convert.ToInt32(b.Rectangle.Height * 0.2));
                            if (!bZone.Contains(Convert.ToInt32(page_w * blob_zonex), Convert.ToInt32(page_h * blob_zoney)))
                            {
                                //skip Zone Not in center
                                if (debug) { Console.WriteLine("Blob " + i + ": Check 2 - Zone of blob Failed! (Not in the zone requested! blob zone:" + b.Rectangle.ToString() + " and requested point is at x=" + Convert.ToInt32(page_w * blob_zonex) + ",y=" + Convert.ToInt32(page_h * blob_zonex) + " ) Blob Skipped!"); }
                                i++; continue;
                            }
                            if (debug) { Console.WriteLine("Blob " + i + ": Check 2 - Zone of blob contains " + Convert.ToInt32(page_w * blob_zonex) + ","+ Convert.ToInt32(page_h * blob_zonex) + "...   Ok!"); }
                        }

                        //check shape
                        List<IntPoint> edgePoints = bc.GetBlobsEdgePoints(b);
                        List<IntPoint> corners;
                        SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
                        if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                        {
                            if (!blob_noshape) { 
                                PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);
                                if (!subType.Equals(PolygonSubType.Square))
                                {
                                    //skip Not a square
                                    if (debug) {    Console.WriteLine("Blob " + i + ": Check 3 - Shape is Square Failed! (Shape is not Square! " + subType.ToString() + " detected!) Blob Skipped!");
                                                    Console.WriteLine("You can disable this check with -bnoshape parameter."); }
                                    i++; continue;
                                } else if (debug) { Console.WriteLine("Blob " + i + ": Check 3 - Shape is Square   Ok!"); }
                            } else if (debug) { Console.WriteLine("Blob " + i + ":  Check 3 - Shape is Square skipped by option -bnoshape "); }
                        } else
                        {
                            shapeChecker.ToString();
                            //skip Not a quadrilateral
                            if (debug) { Console.WriteLine("Blob " + i + ": Check 3 - Shape is Square...   Failed! (not a Quadrilateral! ConvexPolygon:" + shapeChecker.IsConvexPolygon(edgePoints, out corners) + " Triangle:"+shapeChecker.IsTriangle(edgePoints, out corners)+ ") Blob Skipped!"); }
                            i++; continue;
                        }

                        //if (debug){ Console.WriteLine("Blob " + i + ": Trying to decode..."); }

                        //Calculate rotation angle 0 bottom left , 1 top left , 2 top right, 3 bottom right
                        double dx = corners[2].X - corners[1].X;
                        double dy = corners[1].Y - corners[2].Y;
                        double ang = Math.Atan2(dx, dy) * (180 / Math.PI);
                        if (ang > 90) ang = ang - 90;
                        else ang = 90 - ang;

                        //Extract Blob
                        Rectangle cropRect = b.Rectangle;
                        cropRect.Inflate(Convert.ToInt32(b.Rectangle.Width * 0.1), Convert.ToInt32(b.Rectangle.Height * 0.1));
                        Crop filter_blob = new Crop(cropRect);
                        Bitmap tmp_img_blob = filter_blob.Apply(tmp_img);

                        //Rotate
                        if (!blob_notrotate) { 
                            RotateBilinear filterRotate = new RotateBilinear(ang, true);
                            tmp_img_blob = filterRotate.Apply(tmp_img_blob);
                            //Togli margine esterno (bande nere derivanti dalla rotazione)
                            Rectangle cropRectInterno = new Rectangle(0,0, tmp_img_blob.Width, tmp_img_blob.Height);
                            cropRectInterno.Inflate(-Convert.ToInt32(b.Rectangle.Width * 0.05), -Convert.ToInt32(b.Rectangle.Height * 0.05));
                            Crop filterCropInterno = new Crop(cropRectInterno);
                            tmp_img_blob = filterCropInterno.Apply(tmp_img_blob);
                            if (debug) { Console.WriteLine("Blob " + i + ": Rotated and aligned! (angle:" + ang + ")"); }
                        } else
                        { if (debug) { Console.WriteLine("Blob " + i + ": Rotation skipped by option -bnotrotate (angle:" + ang + ")"); } }

                        //Applica filtri
                        var filter1 = new Median();
                        filter1.ApplyInPlace(tmp_img_blob);
                        var filter2 = new OtsuThreshold();
                        filter2.ApplyInPlace(tmp_img_blob);

                        //Decodifica
                        if (debug) { Console.WriteLine("Blob " + i + ": Extracted! Trying to decode..."); }
                        BarcodeReader reader = new BarcodeReader { AutoRotate = true };
                        Result result = reader.Decode(tmp_img_blob);

                        //Output Results
                        if (result != null)
                        {
                            if (emulate)
                            { stopWatch.Stop(); Console.WriteLine("Success in " + stopWatch.Elapsed); }
                            else { Console.WriteLine(result.Text); }
                            Environment.Exit(0);
                        } else if (debug) { Console.WriteLine("Blob " + i + ": Decode failed! (Result null)"); }
                    }
                } else {
                    BarcodeReader reader = new BarcodeReader { AutoRotate = true };
                    Result result = reader.Decode(tmp_img);

                    //Output Results
                    if (result != null)
                    {
                        if (emulate)
                        { stopWatch.Stop(); Console.WriteLine(stopWatch.Elapsed); }
                        else { Console.WriteLine(result.Text); Environment.Exit(0); }
                    } else if (debug) { Console.WriteLine("Decode failed! (Result null)"); }
                }

                //Exit
                if (emulate && stopWatch.IsRunning)
                { stopWatch.Stop(); Console.WriteLine("Failure in "+stopWatch.Elapsed); }
                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                Console.WriteLine( "Fatal Error: "+ex.Message+"\n"+ex.InnerException );
            }
        }

        private static void exitWithError(string r1, string r2="", string r3 = "", string r4 = "")
        {
            Console.WriteLine(r1);
            if (!r2.Equals("")) Console.WriteLine(r2);
            if (!r3.Equals("")) Console.WriteLine(r3);
            if (!r4.Equals("")) Console.WriteLine(r4);
            Environment.Exit(1);
        }

        private static void exitNotValid(string optn)
        {
            Console.WriteLine("Opzione '"+optn+ "' non valida.");
            exitWithHelp();
        }

        private static void exitFileNotFound(string path)
        {
            Console.WriteLine("File "+path+" non trovato.");
            Environment.Exit(1);
        }

        private static void exitWithHelp()
        {
            Console.WriteLine(" ");
            Console.WriteLine("Sintassi:");
            Console.WriteLine(" extsp.exe [-cropcenter] [-emulate] -f file_path ");
            Console.WriteLine("");
            Console.WriteLine("Opzioni:");
            Console.WriteLine("   -f file_path    Percorso dell'immagine su cui tentare la decodifica. Se  ");
            Console.WriteLine("                   viene indicato un file pdf questo viene convertito in    ");
            Console.WriteLine("                   immagine (con libreria ghostscript)");
            Console.WriteLine("   -emulate        Solo simulazione per analisi delle prestazioni. Viene");
            Console.WriteLine("                   restituito il tempo impiegato per la decodifica.");
            Console.WriteLine("   -debug          Espone messaggi di debug circa l'iter di decodifica.");
            Console.WriteLine("   -cropcenter     Ritaglia e analizza solo la parte centrale del documento.");
            Console.WriteLine("                   Viene analizzato solo la parte di immagine compresa in un");
            Console.WriteLine("                   rettangolo i cui estremi sono calcolati come segue:");
            Console.WriteLine("                     x1 = larghezza documento * 0,3");
            Console.WriteLine("                     y1 = altezza documento * 0,3");
            Console.WriteLine("                     x2 = larghezza documento * 0,7");
            Console.WriteLine("                     y2 = altezza documento * 0,7");
            Console.WriteLine("   -blob           Evita di scansionare tutto il documento ed effettua una  "); 
            Console.WriteLine("                   ricerca di possibili QRCode secondo parametri impostati. ");
            Console.WriteLine("                   Vengono cercate e scansionate solo le regioni dell'immagi"); 
            Console.WriteLine("                   ne che corrispondono ai filtri impostati e che potrebbero");
            Console.WriteLine("                   per caratteristiche geometriche contenere un QRCode.");
            Console.WriteLine("                                                                            ");
            Console.WriteLine("   Parametri per la ricerca blob:                                           ");
            Console.WriteLine("     -bfilterw 0.15-0.30  Filtra solo blob con larghezza compresa tra il 10%");
            Console.WriteLine("                          ed il 30% della grandezza totale dell'immagine.   ");
            Console.WriteLine("     -bfilterh 0.10-0.20  Filtra solo blob con altezza compresa tra il 10%  ");
            Console.WriteLine("                          ed il 20% della grandezza totale dell'immagine.   ");
            Console.WriteLine("     -bnoff               Non controllare il fattore di forma del blob. Per ");
            Console.WriteLine("                          default si escludono fattori di forma non quadrati");
            Console.WriteLine("                          ovvero con (larghezza/altezza)% < 95");
            Console.WriteLine("     -bzone 0.5,0.6       La regione del blob (+-20%) per essere valida deve");
            Console.WriteLine("                          contenere un punto definito alle coordinate x,y   ");
            Console.WriteLine("                          calcolate in percentuale rispetto alle dimensioni ");
            Console.WriteLine("                          documento. Nell'esempio x = 50% larghezza doc. e  ");
            Console.WriteLine("                          y = 60% altezza documento. ");
            Console.WriteLine("     -bnoshape            Non controllare la forma del blob. Per default ven");
            Console.WriteLine("                          gono analizzati i bordi del blob per ricavarne  la"); 
            Console.WriteLine("                          forma dell'oggetto,c he deve risultare di tipo    ");
            Console.WriteLine("                          'Square'. Altre forme vengono ignorate, ma alcuni ");
            Console.WriteLine("                          QRCode potrebbero comunque essere riconosciuti.   ");
            Console.WriteLine("     -bnotrotate          Non riallineare il QRCode prima di tentare la lett");
            Console.WriteLine("                          ura. Per default, il blob viene riallineato prima ");
            Console.WriteLine("                          di tentare la decodifica. L'angolo di rotazione è ");
            Console.WriteLine("                          calcolato come Atan2(dx, dy) * (180 / Math.PI)    ");
            Console.WriteLine("                                                                            ");
            Console.WriteLine("     esempi:                                                                ");
            Console.WriteLine("         getQR.exe -f document.pdf -cropcenter                              ");
            Console.WriteLine(" ");
            Console.WriteLine("         getQR.exe -f image.png -blob -cropcenter                           ");
            Console.WriteLine(" ");
            Console.WriteLine("         getQR.exe -f image.png -blob                                       ");
            Console.WriteLine("                   -bfilterw 0.15-0.30 -bfilterh 0.10-0.20 -bzone 0.5,0.5   ");
            Console.WriteLine("                   ");
            Environment.Exit(1);
        }

    }
}
